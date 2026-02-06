#!/usr/bin/env python3
"""
Reconstructor de archivos YTD con texturas optimizadas.
Toma un YTD original y un DDS redimensionado, y crea un nuevo YTD optimizado.
"""

import struct
import os
import sys
import zlib
from pathlib import Path


def read_dds_info(dds_path):
    """Lee información de un archivo DDS."""
    with open(dds_path, 'rb') as f:
        magic = f.read(4)
        if magic != b'DDS ':
            raise ValueError("Not a valid DDS file")
        
        header = f.read(124)
        
        height = struct.unpack('<I', header[8:12])[0]
        width = struct.unpack('<I', header[12:16])[0]
        linear_size = struct.unpack('<I', header[16:20])[0]
        mip_count = struct.unpack('<I', header[24:28])[0]
        
        # FourCC
        fourcc = header[80:84]
        
        # Leer datos de textura
        f.seek(128)  # Después del header
        texture_data = f.read()
        
        return {
            'width': width,
            'height': height,
            'mip_count': mip_count if mip_count > 0 else 1,
            'format': fourcc.decode('ascii') if fourcc[0:3] == b'DXT' else 'BC3',
            'linear_size': linear_size,
            'data': texture_data,
            'data_size': len(texture_data)
        }


def calculate_rsc7_flags(virtual_size, physical_size):
    """
    Calcula los flags RSC7 para los tamaños dados.
    
    Para tamaños típicos en YTD:
    - Virtual (estructuras): 0x2000 (8KB)
    - Physical (texturas): variable según tamaño de textura
    """
    
    def encode_size_to_flags(size):
        """Codifica un tamaño a flags RSC7."""
        if size == 0:
            return 0
        
        # Encontrar el shift base (potencia de 2 más cercana después de 4KB)
        # El tamaño base es 0x1000 << base_shift
        page_size = 0x1000  # 4KB
        
        # Calcular cuántas páginas necesitamos
        pages_needed = (size + page_size - 1) // page_size
        
        # Encontrar base_shift tal que (1 << base_shift) <= pages_needed
        base_shift = 0
        while (1 << (base_shift + 1)) <= pages_needed:
            base_shift += 1
        
        base_pages = 1 << base_shift
        extra_pages = pages_needed - base_pages
        
        # Codificar extra_pages como (mult + 1) << shift - 1 = extra_pages
        # Para simplicidad, usar shift=0, mult=extra_pages
        mult = extra_pages
        shift = 0
        
        # Si mult > 127, necesitamos usar shift
        while mult > 127:
            shift += 1
            mult = (extra_pages >> shift)
        
        # Construir flags
        # bits 4-7: base_shift
        # bits 17-23: mult
        # bits 24-27: shift
        flags = (base_shift << 4) | (mult << 17) | (shift << 24)
        
        return flags
    
    flags0 = encode_size_to_flags(virtual_size)
    flags1 = encode_size_to_flags(physical_size)
    
    # Añadir marcadores típicos
    # bit 7 = 1 indica que hay datos físicos
    if physical_size > 0:
        flags1 |= 0x80
    
    return flags0, flags1


def align_to(size, alignment):
    """Alinea un tamaño al múltiplo de alignment más cercano."""
    return (size + alignment - 1) & ~(alignment - 1)


def rebuild_ytd(original_ytd_path, new_dds_path, output_ytd_path):
    """
    Reconstruye un YTD con una nueva textura.
    
    Estrategia:
    1. Descomprimir YTD original
    2. Leer nueva textura DDS
    3. Actualizar campos de dimensiones en la estructura
    4. Reemplazar datos de textura
    5. Recalcular flags RSC7
    6. Recomprimir
    """
    print(f"\n{'='*60}")
    print(f"Rebuilding YTD")
    print(f"{'='*60}")
    print(f"Original: {original_ytd_path}")
    print(f"New DDS: {new_dds_path}")
    print(f"Output: {output_ytd_path}")
    
    # Leer YTD original
    with open(original_ytd_path, 'rb') as f:
        original_data = f.read()
    
    if original_data[:4] != b'RSC7':
        raise ValueError("Original is not RSC7 format")
    
    # Descomprimir
    original_version = struct.unpack('<I', original_data[4:8])[0]
    original_flags0 = struct.unpack('<I', original_data[8:12])[0]
    original_flags1 = struct.unpack('<I', original_data[12:16])[0]
    
    compressed = original_data[16:]
    decompressed = zlib.decompress(compressed, -15)
    
    print(f"\nOriginal decompressed: {len(decompressed)} bytes")
    
    # Calcular tamaño del segmento virtual original
    # Para esto usamos el método de búsqueda del offset de DXT
    dxt_offset = decompressed.find(b'DXT')
    if dxt_offset < 0:
        raise ValueError("Cannot find texture format in original")
    
    # La estructura de textura está 8 bytes antes de DXT
    tex_info_offset = dxt_offset - 8
    
    # Leer dimensiones originales
    orig_width = struct.unpack('<H', decompressed[tex_info_offset:tex_info_offset+2])[0]
    orig_height = struct.unpack('<H', decompressed[tex_info_offset+2:tex_info_offset+4])[0]
    
    print(f"Original texture: {orig_width}x{orig_height}")
    
    # Encontrar puntero a datos físicos
    data_ptr_offset = None
    for i in range(dxt_offset, min(dxt_offset + 64, len(decompressed) - 4), 4):
        ptr = struct.unpack('<I', decompressed[i:i+4])[0]
        if (ptr & 0xF0000000) == 0x60000000:
            data_ptr_offset = i
            break
    
    if data_ptr_offset is None:
        raise ValueError("Cannot find texture data pointer")
    
    # El segmento virtual termina donde empiezan los datos físicos
    # Típicamente el puntero 0x60000000 indica offset 0 en el segmento físico
    # Buscar el menor offset con datos significativos después del header de textura
    virtual_size = 0x2000  # Valor típico para YTD simples
    
    # Leer nueva textura DDS
    new_tex = read_dds_info(new_dds_path)
    print(f"New texture: {new_tex['width']}x{new_tex['height']}, {new_tex['mip_count']} mips")
    print(f"New texture data: {new_tex['data_size']} bytes")
    
    # Crear nuevo buffer de datos descomprimidos
    # Estructura:
    # [0, virtual_size): Estructuras (modificadas con nuevas dimensiones)
    # [virtual_size, ...): Datos de textura nuevos
    
    # Copiar segmento virtual original
    new_virtual = bytearray(decompressed[:virtual_size])
    
    # Actualizar dimensiones en la estructura
    struct.pack_into('<H', new_virtual, tex_info_offset, new_tex['width'])
    struct.pack_into('<H', new_virtual, tex_info_offset + 2, new_tex['height'])
    
    # Actualizar mip count (está en offset dxt + 5, que es tex_info_offset + 13)
    mip_offset = tex_info_offset + 13  # Después de DXT5 + null
    if mip_offset < len(new_virtual):
        new_virtual[mip_offset] = new_tex['mip_count']
    
    # El puntero de datos se mantiene en 0x60000000 (inicio del segmento físico)
    
    # Alinear datos de textura a 16 bytes
    new_tex_data = new_tex['data']
    aligned_tex_size = align_to(len(new_tex_data), 0x1000)  # Alinear a página
    
    # Padding
    padding = bytes(aligned_tex_size - len(new_tex_data))
    
    # Construir nuevo archivo descomprimido
    new_decompressed = bytes(new_virtual) + new_tex_data + padding
    
    print(f"\nNew decompressed size: {len(new_decompressed)} bytes")
    print(f"  Virtual: {len(new_virtual)} bytes")
    print(f"  Physical: {len(new_tex_data) + len(padding)} bytes")
    
    # Calcular nuevos flags RSC7
    new_physical_size = aligned_tex_size
    new_flags0, new_flags1 = calculate_rsc7_flags(virtual_size, new_physical_size)
    
    print(f"\nNew RSC7 flags:")
    print(f"  Flags0: 0x{new_flags0:08X}")
    print(f"  Flags1: 0x{new_flags1:08X}")
    
    # Comprimir con deflate raw
    compressor = zlib.compressobj(9, zlib.DEFLATED, -15)  # Nivel 9, deflate raw
    compressed = compressor.compress(new_decompressed)
    compressed += compressor.flush()
    
    print(f"Compressed: {len(compressed)} bytes")
    
    # Construir nuevo archivo RSC7
    new_header = struct.pack('<4sIII', b'RSC7', original_version, new_flags0, new_flags1)
    new_ytd_data = new_header + compressed
    
    # Crear directorio de salida si no existe
    output_path = Path(output_ytd_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    
    # Guardar
    with open(output_ytd_path, 'wb') as f:
        f.write(new_ytd_data)
    
    print(f"\nSaved: {output_ytd_path}")
    print(f"Size: {len(new_ytd_data)} bytes")
    
    # Comparar tamaños
    orig_size = os.path.getsize(original_ytd_path)
    new_size = len(new_ytd_data)
    reduction = ((orig_size - new_size) / orig_size) * 100
    print(f"\nSize reduction: {orig_size} -> {new_size} bytes ({reduction:.1f}%)")
    
    return True


def verify_ytd(ytd_path):
    """Verifica que un YTD sea válido descomprimiéndolo."""
    print(f"\nVerifying: {ytd_path}")
    
    with open(ytd_path, 'rb') as f:
        data = f.read()
    
    if data[:4] != b'RSC7':
        print("  ERROR: Not RSC7 format")
        return False
    
    try:
        decompressed = zlib.decompress(data[16:], -15)
        print(f"  OK: Decompressed to {len(decompressed)} bytes")
        
        # Buscar DXT
        dxt_offset = decompressed.find(b'DXT')
        if dxt_offset >= 0:
            tex_info = dxt_offset - 8
            w = struct.unpack('<H', decompressed[tex_info:tex_info+2])[0]
            h = struct.unpack('<H', decompressed[tex_info+2:tex_info+4])[0]
            fmt = decompressed[dxt_offset:dxt_offset+4].decode('ascii')
            print(f"  Texture: {w}x{h} {fmt}")
        
        return True
    except Exception as e:
        print(f"  ERROR: {e}")
        return False


def main():
    import argparse
    
    parser = argparse.ArgumentParser(description='YTD Rebuilder')
    parser.add_argument('original', help='Original YTD file')
    parser.add_argument('new_dds', help='New DDS file (resized texture)')
    parser.add_argument('-o', '--output', help='Output YTD file', required=True)
    parser.add_argument('-v', '--verify', action='store_true', help='Verify output')
    
    args = parser.parse_args()
    
    rebuild_ytd(args.original, args.new_dds, args.output)
    
    if args.verify:
        verify_ytd(args.output)


if __name__ == "__main__":
    main()
