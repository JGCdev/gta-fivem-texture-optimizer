#!/usr/bin/env python3
"""
YTD Batch Optimizer - Procesa múltiples archivos YTD de GTA V / FiveM
Reduce el tamaño de texturas manteniendo calidad visual aceptable.

Uso:
    python batch_optimize.py [input_folder] [output_folder] [--size 512]
"""

import os
import sys
import struct
import zlib
import shutil
import subprocess
import argparse
from pathlib import Path
from concurrent.futures import ProcessPoolExecutor, as_completed
import time


class YTDBatchOptimizer:
    """Optimizador batch para archivos YTD."""
    
    def __init__(self, texconv_path="tools/texconv.exe", target_size=512, parallel=4):
        self.texconv_path = Path(texconv_path).absolute()
        self.target_size = target_size
        self.parallel = parallel
        self.stats = {
            'processed': 0,
            'skipped': 0,
            'errors': 0,
            'original_bytes': 0,
            'optimized_bytes': 0
        }
    
    def decompress_rsc7(self, data):
        """Descomprime datos RSC7."""
        if data[:4] != b'RSC7':
            return None
        compressed = data[16:]
        try:
            return zlib.decompress(compressed, -15)
        except:
            return None
    
    def get_texture_info(self, decompressed):
        """Obtiene información de la textura del YTD descomprimido."""
        dxt_pos = decompressed.find(b'DXT')
        if dxt_pos < 0:
            return None
        
        info_offset = dxt_pos - 8
        if info_offset < 0:
            return None
        
        width = struct.unpack('<H', decompressed[info_offset:info_offset+2])[0]
        height = struct.unpack('<H', decompressed[info_offset+2:info_offset+4])[0]
        format_str = decompressed[dxt_pos:dxt_pos+4].decode('ascii', errors='ignore')
        
        # Mip count después del formato
        mip_offset = dxt_pos + 5
        mip_count = decompressed[mip_offset] if mip_offset < len(decompressed) else 1
        if mip_count == 0:
            mip_count = 12
        
        return {
            'width': width,
            'height': height,
            'format': format_str,
            'mip_count': mip_count,
            'info_offset': info_offset
        }
    
    def calculate_texture_size(self, width, height, format_str, mip_count):
        """Calcula tamaño de datos de textura."""
        bpb = 8 if format_str == 'DXT1' else 16
        total = 0
        for _ in range(mip_count):
            total += max(1, width // 4) * max(1, height // 4) * bpb
            width = max(1, width // 2)
            height = max(1, height // 2)
        return total
    
    def create_dds_header(self, width, height, mip_count, format_str):
        """Crea header DDS."""
        header = bytearray(128)
        struct.pack_into('<I', header, 0, 0x20534444)  # "DDS "
        struct.pack_into('<I', header, 4, 124)
        flags = 0x1 | 0x2 | 0x4 | 0x1000 | 0x80000
        if mip_count > 1:
            flags |= 0x20000
        struct.pack_into('<I', header, 8, flags)
        struct.pack_into('<I', header, 12, height)
        struct.pack_into('<I', header, 16, width)
        bpb = 8 if format_str == 'DXT1' else 16
        linear = max(1, width // 4) * max(1, height // 4) * bpb
        struct.pack_into('<I', header, 20, linear)
        struct.pack_into('<I', header, 28, mip_count)
        struct.pack_into('<I', header, 76, 32)
        struct.pack_into('<I', header, 80, 0x4)
        fourcc = {'DXT1': 0x31545844, 'DXT3': 0x33545844, 'DXT5': 0x35545844}
        struct.pack_into('<I', header, 84, fourcc.get(format_str, 0x35545844))
        caps = 0x1000
        if mip_count > 1:
            caps |= 0x8 | 0x400000
        struct.pack_into('<I', header, 108, caps)
        return bytes(header)
    
    def encode_rsc7_flags(self, virtual_size, physical_size):
        """Codifica flags RSC7 para los tamaños dados."""
        def encode(size):
            if size == 0:
                return 0
            pages = (size + 0xFFF) // 0x1000
            shift = 0
            while (1 << (shift + 1)) <= pages:
                shift += 1
            base = 1 << shift
            extra = pages - base
            mult = min(extra, 127)
            return (shift << 4) | (mult << 17)
        
        f0 = encode(virtual_size)
        f1 = encode(physical_size)
        if physical_size > 0:
            f1 |= 0x80
        return f0, f1
    
    def optimize_single(self, ytd_path, output_path, temp_dir):
        """Optimiza un solo archivo YTD."""
        try:
            with open(ytd_path, 'rb') as f:
                original = f.read()
            
            if original[:4] != b'RSC7':
                # No es RSC7, copiar sin cambios
                shutil.copy(ytd_path, output_path)
                return {'status': 'skipped', 'reason': 'not RSC7', 
                        'original': len(original), 'optimized': len(original)}
            
            version = struct.unpack('<I', original[4:8])[0]
            decompressed = self.decompress_rsc7(original)
            if decompressed is None:
                shutil.copy(ytd_path, output_path)
                return {'status': 'skipped', 'reason': 'decompress failed',
                        'original': len(original), 'optimized': len(original)}
            
            tex_info = self.get_texture_info(decompressed)
            if tex_info is None:
                shutil.copy(ytd_path, output_path)
                return {'status': 'skipped', 'reason': 'no texture found',
                        'original': len(original), 'optimized': len(original)}
            
            # Si ya es pequeño, no optimizar
            if tex_info['width'] <= self.target_size and tex_info['height'] <= self.target_size:
                shutil.copy(ytd_path, output_path)
                return {'status': 'skipped', 'reason': 'already optimized',
                        'original': len(original), 'optimized': len(original)}
            
            # Calcular nuevo tamaño
            ratio = min(self.target_size / tex_info['width'], 
                       self.target_size / tex_info['height'])
            new_w = max(4, int(tex_info['width'] * ratio))
            new_h = max(4, int(tex_info['height'] * ratio))
            # Redondear a potencia de 2
            new_w = 1 << (new_w - 1).bit_length()
            new_h = 1 << (new_h - 1).bit_length()
            
            # Extraer textura a DDS temporal
            virtual_size = 0x2000
            tex_size = self.calculate_texture_size(
                tex_info['width'], tex_info['height'],
                tex_info['format'], tex_info['mip_count']
            )
            tex_data = decompressed[virtual_size:virtual_size + tex_size]
            
            dds_header = self.create_dds_header(
                tex_info['width'], tex_info['height'],
                tex_info['mip_count'], tex_info['format']
            )
            
            base_name = Path(ytd_path).stem
            temp_dds = temp_dir / f"{base_name}_orig.dds"
            resized_dds = temp_dir / f"{base_name}_resized.dds"
            
            with open(temp_dds, 'wb') as f:
                f.write(dds_header)
                f.write(tex_data)
            
            # Redimensionar con texconv
            new_mips = min(12, min(new_w, new_h).bit_length())
            cmd = [
                str(self.texconv_path),
                "-w", str(new_w), "-h", str(new_h),
                "-m", str(new_mips),
                "-f", "BC3_UNORM",
                "-o", str(temp_dir),
                "-y",
                str(temp_dds)
            ]
            
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=60)
            if result.returncode != 0:
                shutil.copy(ytd_path, output_path)
                if temp_dds.exists():
                    temp_dds.unlink()
                return {'status': 'error', 'reason': 'texconv failed',
                        'original': len(original), 'optimized': len(original)}
            
            # texconv sobrescribe el archivo original
            resized_dds = temp_dds  # texconv mantiene el mismo nombre
            
            # Leer textura redimensionada
            with open(resized_dds, 'rb') as f:
                f.seek(0)
                dds_data = f.read()
            
            new_tex_data = dds_data[128:]  # Saltar header DDS
            
            # Leer nuevas dimensiones del header DDS
            new_height = struct.unpack('<I', dds_data[12:16])[0]
            new_width = struct.unpack('<I', dds_data[16:20])[0]
            new_mip_count = struct.unpack('<I', dds_data[28:32])[0]
            
            # Reconstruir YTD
            new_virtual = bytearray(decompressed[:virtual_size])
            
            # Actualizar dimensiones
            info_off = tex_info['info_offset']
            struct.pack_into('<H', new_virtual, info_off, new_width)
            struct.pack_into('<H', new_virtual, info_off + 2, new_height)
            
            # Actualizar mip count
            dxt_pos = info_off + 8
            mip_off = dxt_pos + 5
            if mip_off < len(new_virtual):
                new_virtual[mip_off] = new_mip_count if new_mip_count > 0 else 9
            
            # Alinear datos físicos
            phys_size = ((len(new_tex_data) + 0xFFF) // 0x1000) * 0x1000
            padding = bytes(phys_size - len(new_tex_data))
            
            new_decompressed = bytes(new_virtual) + new_tex_data + padding
            
            # Calcular flags RSC7
            f0, f1 = self.encode_rsc7_flags(virtual_size, phys_size)
            
            # Comprimir
            compressor = zlib.compressobj(9, zlib.DEFLATED, -15)
            compressed = compressor.compress(new_decompressed) + compressor.flush()
            
            # Escribir nuevo YTD
            new_ytd = struct.pack('<4sIII', b'RSC7', version, f0, f1) + compressed
            
            output_path.parent.mkdir(parents=True, exist_ok=True)
            with open(output_path, 'wb') as f:
                f.write(new_ytd)
            
            # Limpiar temp
            if temp_dds.exists():
                temp_dds.unlink()
            
            return {
                'status': 'optimized',
                'original': len(original),
                'optimized': len(new_ytd),
                'old_size': f"{tex_info['width']}x{tex_info['height']}",
                'new_size': f"{new_width}x{new_height}"
            }
            
        except Exception as e:
            try:
                shutil.copy(ytd_path, output_path)
            except:
                pass
            return {'status': 'error', 'reason': str(e)[:50],
                    'original': 0, 'optimized': 0}
    
    def process_batch(self, input_folder, output_folder):
        """Procesa todos los YTD en una carpeta."""
        input_folder = Path(input_folder)
        output_folder = Path(output_folder)
        temp_dir = output_folder / "temp"
        temp_dir.mkdir(parents=True, exist_ok=True)
        
        ytd_files = list(input_folder.glob("*.ytd"))
        total = len(ytd_files)
        
        print(f"\n{'='*60}")
        print(f"YTD Batch Optimizer")
        print(f"{'='*60}")
        print(f"Input: {input_folder}")
        print(f"Output: {output_folder}")
        print(f"Target size: {self.target_size}x{self.target_size}")
        print(f"Files to process: {total}")
        print(f"{'='*60}\n")
        
        start_time = time.time()
        
        for i, ytd_path in enumerate(ytd_files, 1):
            output_path = output_folder / ytd_path.name
            
            result = self.optimize_single(ytd_path, output_path, temp_dir)
            
            status = result['status']
            orig = result['original']
            opt = result['optimized']
            
            self.stats['original_bytes'] += orig
            self.stats['optimized_bytes'] += opt
            
            if status == 'optimized':
                self.stats['processed'] += 1
                reduction = ((orig - opt) / orig * 100) if orig > 0 else 0
                print(f"[{i}/{total}] {ytd_path.name}")
                print(f"    {result['old_size']} -> {result['new_size']}, {reduction:.1f}% smaller")
            elif status == 'skipped':
                self.stats['skipped'] += 1
                print(f"[{i}/{total}] {ytd_path.name} - skipped ({result['reason']})")
            else:
                self.stats['errors'] += 1
                print(f"[{i}/{total}] {ytd_path.name} - ERROR: {result.get('reason', 'unknown')}")
        
        # Limpiar temp
        try:
            shutil.rmtree(temp_dir)
        except:
            pass
        
        elapsed = time.time() - start_time
        
        # Resumen
        print(f"\n{'='*60}")
        print(f"Summary")
        print(f"{'='*60}")
        print(f"Processed: {self.stats['processed']}")
        print(f"Skipped: {self.stats['skipped']}")
        print(f"Errors: {self.stats['errors']}")
        
        orig_mb = self.stats['original_bytes'] / (1024 * 1024)
        opt_mb = self.stats['optimized_bytes'] / (1024 * 1024)
        reduction = ((self.stats['original_bytes'] - self.stats['optimized_bytes']) 
                    / self.stats['original_bytes'] * 100) if self.stats['original_bytes'] > 0 else 0
        
        print(f"\nTotal size: {orig_mb:.2f} MB -> {opt_mb:.2f} MB ({reduction:.1f}% reduction)")
        print(f"Time: {elapsed:.1f} seconds")


def main():
    parser = argparse.ArgumentParser(description='YTD Batch Optimizer for GTA V / FiveM')
    parser.add_argument('input', help='Input folder with .ytd files')
    parser.add_argument('output', help='Output folder for optimized files')
    parser.add_argument('-s', '--size', type=int, default=512, 
                       help='Target max texture size (default: 512)')
    parser.add_argument('-t', '--texconv', default='tools/texconv.exe',
                       help='Path to texconv.exe')
    
    args = parser.parse_args()
    
    optimizer = YTDBatchOptimizer(
        texconv_path=args.texconv,
        target_size=args.size
    )
    
    optimizer.process_batch(args.input, args.output)


if __name__ == "__main__":
    main()
