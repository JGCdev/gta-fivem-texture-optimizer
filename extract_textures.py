#!/usr/bin/env python3
"""
Extractor de texturas YTD a DDS y optimizador.
Soporta formato RSC7 de GTA V / FiveM.
"""

import struct
import os
import sys
import subprocess
import zlib
from pathlib import Path

# Constantes DDS
DDS_MAGIC = 0x20534444  # "DDS "
DDSD_CAPS = 0x1
DDSD_HEIGHT = 0x2
DDSD_WIDTH = 0x4
DDSD_PITCH = 0x8
DDSD_PIXELFORMAT = 0x1000
DDSD_MIPMAPCOUNT = 0x20000
DDSD_LINEARSIZE = 0x80000
DDSD_DEPTH = 0x800000

DDPF_FOURCC = 0x4

DDSCAPS_COMPLEX = 0x8
DDSCAPS_MIPMAP = 0x400000
DDSCAPS_TEXTURE = 0x1000

# FourCC codes
FOURCC_DXT1 = 0x31545844  # "DXT1"
FOURCC_DXT3 = 0x33545844  # "DXT3"  
FOURCC_DXT5 = 0x35545844  # "DXT5"

class YTDExtractor:
    """Extrae texturas de archivos YTD y las convierte a DDS."""
    
    def __init__(self, texconv_path="tools/texconv.exe"):
        self.texconv_path = Path(texconv_path)
        
    def decompress_rsc7(self, data):
        """Descomprime datos RSC7 usando deflate raw."""
        if data[:4] != b'RSC7':
            raise ValueError("Not a RSC7 file")
        
        compressed = data[16:]
        try:
            decompressed = zlib.decompress(compressed, -15)
            return decompressed
        except Exception as e:
            raise ValueError(f"Decompression failed: {e}")
    
    def parse_ytd(self, data, is_decompressed=True):
        """
        Parsea un YTD descomprimido y extrae información de texturas.
        
        Estructura YTD (TextureDictionary):
        - Offset 0x0000: VFT pointer
        - Offset 0x0008: BlockMap pointer
        - Offset 0x0010: ParentDictionary
        - Offset 0x0018: UsageCount
        - Offset 0x001C: TextureNameHashes (PsoPointer to array)
        - Offset 0x0020: Textures (PsoPointer to texture objects)
        """
        result = {
            'textures': []
        }
        
        # Buscar la cadena "DXT" para localizar texturas
        pos = 0
        while True:
            dxt_pos = data.find(b'DXT', pos)
            if dxt_pos < 0:
                break
            
            # Verificar si es DXT1, DXT3 o DXT5
            format_str = data[dxt_pos:dxt_pos+4]
            if format_str in [b'DXT1', b'DXT3', b'DXT5']:
                # La estructura de textura está antes del formato
                # Típicamente: width(2), height(2), depth(2), stride/flags(2), format(4)
                tex_info_start = dxt_pos - 8
                
                if tex_info_start >= 0:
                    width = struct.unpack('<H', data[tex_info_start:tex_info_start+2])[0]
                    height = struct.unpack('<H', data[tex_info_start+2:tex_info_start+4])[0]
                    depth = struct.unpack('<H', data[tex_info_start+4:tex_info_start+6])[0]
                    flags = struct.unpack('<H', data[tex_info_start+6:tex_info_start+8])[0]
                    
                    # Mip count está después del formato
                    mip_offset = dxt_pos + 5  # Después del byte 0x00 tras DXT5
                    mip_count = data[mip_offset] if mip_offset < len(data) else 1
                    
                    # Buscar puntero a datos (0x6XXXXXXX)
                    data_ptr = None
                    data_offset = None
                    
                    # Buscar en los 32 bytes siguientes
                    for i in range(dxt_pos, min(dxt_pos + 64, len(data) - 4), 4):
                        ptr = struct.unpack('<I', data[i:i+4])[0]
                        if (ptr & 0xF0000000) == 0x60000000:
                            data_ptr = ptr
                            data_offset = ptr & 0x0FFFFFFF
                            break
                    
                    # Calcular tamaño de segmento virtual (buscar patrones)
                    # Por defecto asumimos 0x2000 para YTD simples
                    virtual_size = 0x2000
                    
                    tex = {
                        'format': format_str.decode('ascii'),
                        'width': width,
                        'height': height,
                        'depth': depth,
                        'mip_count': mip_count if mip_count > 0 else 12,
                        'data_ptr': data_ptr,
                        'data_offset_in_phys': data_offset,
                        'info_offset': tex_info_start,
                        'virtual_size': virtual_size
                    }
                    
                    # Calcular tamaño de datos
                    tex['data_size'] = self.calculate_texture_size(
                        width, height, format_str.decode('ascii'), tex['mip_count']
                    )
                    
                    result['textures'].append(tex)
            
            pos = dxt_pos + 1
        
        return result
    
    def calculate_texture_size(self, width, height, format_str, mip_count):
        """Calcula el tamaño total de una textura con mipmaps."""
        if format_str == 'DXT1':
            bytes_per_block = 8
        else:  # DXT3, DXT5
            bytes_per_block = 16
        
        total = 0
        w, h = width, height
        for i in range(mip_count):
            blocks_x = max(1, w // 4)
            blocks_y = max(1, h // 4)
            mip_size = blocks_x * blocks_y * bytes_per_block
            total += mip_size
            w = max(1, w // 2)
            h = max(1, h // 2)
        
        return total
    
    def create_dds_header(self, width, height, mip_count, format_str):
        """Crea un header DDS válido."""
        header = bytearray(128)
        
        # Magic
        struct.pack_into('<I', header, 0, DDS_MAGIC)
        
        # DDS_HEADER (size = 124)
        struct.pack_into('<I', header, 4, 124)  # dwSize
        
        # dwFlags
        flags = DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT | DDSD_LINEARSIZE
        if mip_count > 1:
            flags |= DDSD_MIPMAPCOUNT
        struct.pack_into('<I', header, 8, flags)
        
        # dwHeight, dwWidth
        struct.pack_into('<I', header, 12, height)
        struct.pack_into('<I', header, 16, width)
        
        # dwPitchOrLinearSize (para DXT, es el tamaño del mip 0)
        if format_str == 'DXT1':
            bytes_per_block = 8
        else:
            bytes_per_block = 16
        blocks_x = max(1, width // 4)
        blocks_y = max(1, height // 4)
        linear_size = blocks_x * blocks_y * bytes_per_block
        struct.pack_into('<I', header, 20, linear_size)
        
        # dwDepth
        struct.pack_into('<I', header, 24, 0)
        
        # dwMipMapCount
        struct.pack_into('<I', header, 28, mip_count)
        
        # dwReserved1[11] - skip 44 bytes (offsets 32-75)
        
        # DDS_PIXELFORMAT (offset 76, size 32)
        struct.pack_into('<I', header, 76, 32)  # dwSize
        struct.pack_into('<I', header, 80, DDPF_FOURCC)  # dwFlags
        
        # dwFourCC
        if format_str == 'DXT1':
            fourcc = FOURCC_DXT1
        elif format_str == 'DXT3':
            fourcc = FOURCC_DXT3
        else:  # DXT5
            fourcc = FOURCC_DXT5
        struct.pack_into('<I', header, 84, fourcc)
        
        # Rest of PIXELFORMAT (RGBBitCount, masks) - zeros for FOURCC
        
        # dwCaps (offset 108)
        caps = DDSCAPS_TEXTURE
        if mip_count > 1:
            caps |= DDSCAPS_COMPLEX | DDSCAPS_MIPMAP
        struct.pack_into('<I', header, 108, caps)
        
        # dwCaps2, dwCaps3, dwCaps4, dwReserved2 - zeros
        
        return bytes(header)
    
    def extract_texture_to_dds(self, data, texture_info, output_path):
        """Extrae una textura a un archivo DDS."""
        virtual_size = texture_info['virtual_size']
        data_offset = virtual_size + texture_info['data_offset_in_phys']
        data_size = texture_info['data_size']
        
        print(f"  Extracting from offset 0x{data_offset:X}, size {data_size} bytes")
        
        if data_offset + data_size > len(data):
            print(f"  Warning: Data extends beyond file (need {data_offset + data_size}, have {len(data)})")
            # Ajustar tamaño disponible
            data_size = len(data) - data_offset
        
        texture_data = data[data_offset:data_offset + data_size]
        
        # Crear header DDS
        header = self.create_dds_header(
            texture_info['width'],
            texture_info['height'],
            texture_info['mip_count'],
            texture_info['format']
        )
        
        # Escribir DDS
        with open(output_path, 'wb') as f:
            f.write(header)
            f.write(texture_data)
        
        print(f"  Saved: {output_path} ({os.path.getsize(output_path)} bytes)")
        return True
    
    def resize_texture(self, input_dds, output_dds, target_width, target_height):
        """
        Usa texconv.exe para redimensionar una textura.
        1. Convierte DDS a formato temporal no comprimido
        2. Redimensiona
        3. Recomprime a DXT5
        """
        if not self.texconv_path.exists():
            raise FileNotFoundError(f"texconv.exe not found at {self.texconv_path}")
        
        temp_dir = Path(input_dds).parent / "temp"
        temp_dir.mkdir(exist_ok=True)
        
        input_path = Path(input_dds)
        output_path = Path(output_dds)
        
        # Calcular mip levels basado en el tamaño menor
        min_dim = min(target_width, target_height)
        max_mips = min(12, min_dim.bit_length())
        
        # Paso 1: Copiar input a temp con nombre diferente para evitar sobrescritura
        temp_input = temp_dir / f"temp_{input_path.name}"
        import shutil
        shutil.copy(input_dds, temp_input)
        
        # Paso 2: Redimensionar y recomprimir a DXT5
        cmd = [
            str(self.texconv_path),
            "-w", str(target_width),
            "-h", str(target_height),
            "-m", str(max_mips),  # Mip levels apropiados
            "-f", "BC3_UNORM",  # DXT5
            "-o", str(output_path.parent),
            "-y",  # Overwrite
            "-px", output_path.stem.replace(temp_input.stem, "") + "_",  # Prefijo único
            str(temp_input)
        ]
        
        print(f"  Running: texconv resize to {target_width}x{target_height}")
        result = subprocess.run(cmd, capture_output=True, text=True)
        
        if result.returncode != 0:
            print(f"  Error: {result.stderr}")
            return False
        
        # Buscar archivo generado y renombrarlo
        for f in output_path.parent.glob("*temp_*.dds"):
            if f.exists():
                f.rename(output_path)
                break
        
        # Limpiar temp
        if temp_input.exists():
            temp_input.unlink()
        
        if output_path.exists():
            print(f"  Resized: {output_path.name} ({output_path.stat().st_size} bytes)")
            return True
        return False
    
    def process_ytd(self, ytd_path, output_dir=None, target_size=512):
        """
        Procesa un archivo YTD completo:
        1. Descomprime si es necesario
        2. Extrae texturas a DDS
        3. Redimensiona
        4. Prepara para reempaquetado
        """
        ytd_path = Path(ytd_path)
        
        if output_dir is None:
            output_dir = ytd_path.parent / "extracted"
        output_dir = Path(output_dir)
        output_dir.mkdir(exist_ok=True)
        
        print(f"\n{'='*60}")
        print(f"Processing: {ytd_path.name}")
        print(f"{'='*60}")
        
        # Leer archivo
        with open(ytd_path, 'rb') as f:
            raw_data = f.read()
        
        # Descomprimir si es RSC7
        if raw_data[:4] == b'RSC7':
            print("Decompressing RSC7...")
            try:
                data = self.decompress_rsc7(raw_data)
                print(f"  Decompressed: {len(raw_data)} -> {len(data)} bytes")
            except Exception as e:
                print(f"  Decompression failed: {e}")
                return None
        else:
            data = raw_data
        
        # Parsear YTD
        print("Parsing YTD structure...")
        parsed = self.parse_ytd(data)
        
        if not parsed['textures']:
            print("  No textures found!")
            return None
        
        print(f"  Found {len(parsed['textures'])} texture(s)")
        
        results = []
        
        for i, tex in enumerate(parsed['textures']):
            print(f"\nTexture {i}:")
            print(f"  Format: {tex['format']}")
            print(f"  Size: {tex['width']}x{tex['height']}")
            print(f"  Mip levels: {tex['mip_count']}")
            print(f"  Data size: {tex['data_size']} bytes")
            
            # Nombre de salida
            base_name = ytd_path.stem
            dds_name = f"{base_name}_tex{i}.dds"
            dds_path = output_dir / dds_name
            
            # Extraer a DDS
            if self.extract_texture_to_dds(data, tex, dds_path):
                result = {
                    'original': tex,
                    'dds_path': str(dds_path)
                }
                
                # Redimensionar si es más grande que target
                if tex['width'] > target_size or tex['height'] > target_size:
                    # Calcular nuevo tamaño manteniendo aspect ratio
                    ratio = min(target_size / tex['width'], target_size / tex['height'])
                    new_width = int(tex['width'] * ratio)
                    new_height = int(tex['height'] * ratio)
                    
                    # Asegurar que sean potencias de 2
                    new_width = 1 << (new_width - 1).bit_length()
                    new_height = 1 << (new_height - 1).bit_length()
                    
                    resized_name = f"{base_name}_tex{i}_resized.dds"
                    resized_path = output_dir / resized_name
                    
                    print(f"\n  Resizing to {new_width}x{new_height}...")
                    if self.resize_texture(dds_path, resized_path, new_width, new_height):
                        result['resized_path'] = str(resized_path)
                        result['new_width'] = new_width
                        result['new_height'] = new_height
                
                results.append(result)
        
        return results


def main():
    import argparse
    
    parser = argparse.ArgumentParser(description='YTD Texture Extractor and Optimizer')
    parser.add_argument('input', help='Input YTD file or directory')
    parser.add_argument('-o', '--output', help='Output directory', default='extracted')
    parser.add_argument('-s', '--size', type=int, default=512, help='Target max texture size (default: 512)')
    parser.add_argument('--texconv', default='tools/texconv.exe', help='Path to texconv.exe')
    
    args = parser.parse_args()
    
    extractor = YTDExtractor(texconv_path=args.texconv)
    
    input_path = Path(args.input)
    
    if input_path.is_file():
        extractor.process_ytd(input_path, args.output, args.size)
    elif input_path.is_dir():
        for ytd in input_path.glob('*.ytd'):
            extractor.process_ytd(ytd, args.output, args.size)
    else:
        print(f"Error: {input_path} not found")
        sys.exit(1)


if __name__ == "__main__":
    main()
