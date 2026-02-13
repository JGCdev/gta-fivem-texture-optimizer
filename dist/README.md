# FiveM Texture Optimizer - GUI

Interfaz gráfica para optimizar texturas de GTA V / FiveM (YTD, YDD, YDR, YFT).

## Uso

1. **Ejecutar** `FiveM Texture Optimizer.exe`

2. **Seleccionar Carpeta**: Selecciona la carpeta que contiene los archivos YTD/YDD/YDR/YFT

3. **Escanear Archivos**: Escanea y muestra todos los archivos con sus texturas internas

4. **Configurar**:
   - Tamaño máximo (128, 256, 512, 1024, 2048 px)
   - Seleccionar/deseleccionar archivos individuales

5. **Optimizar Seleccionados**: Procesa solo los archivos marcados

## Características

- **Tema oscuro** moderno
- **Vista detallada** de cada textura (nombre, dimensiones, formato)
- **Selección individual** de archivos a optimizar
- **Barra de progreso** y log de operaciones
- **Resumen** de reducción de tamaño

## Requisitos

- Windows 10/11 (64-bit)
- `texconv.exe` debe estar en la misma carpeta que el ejecutable

## Archivos en dist/

```
FiveM Texture Optimizer.exe  - Aplicación principal (self-contained)
texconv.exe                  - Herramienta de conversión de texturas
CodeWalker.Core.pdb          - Símbolos de debug (opcional)
FiveM Texture Optimizer.pdb  - Símbolos de debug (opcional)
ShadersGen9Conversion.xml    - Archivo de configuración
strings.txt                  - Archivo de strings
```

## Notas

- Los archivos `.pdb` son opcionales y solo sirven para debugging
- La carpeta de salida por defecto es `[carpeta_entrada]/optimized`
- Los archivos originales NO se modifican
