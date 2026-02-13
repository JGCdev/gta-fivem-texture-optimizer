# FiveM Texture Optimizer

🎨 Herramienta con interfaz gráfica para optimizar texturas de GTA V / FiveM. Reduce el tamaño de archivos hasta un 90%.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4) ![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6) ![License](https://img.shields.io/badge/License-MIT-green)

## Características

- ✅ Interfaz gráfica moderna con tema oscuro
- ✅ Selección individual de archivos a optimizar
- ✅ Vista previa de texturas con dimensiones y formato
- ✅ Configuración de tamaño máximo (128-2048px)
- ✅ Log en tiempo real del proceso
- ✅ Estadísticas de reducción de tamaño

## Formatos Soportados

| Formato | Descripción | Soporte |
|---------|-------------|---------|
| **YTD** | Texture Dictionary | ✅ Completo |
| **YDD** | Drawable Dictionary | ✅ Texturas embebidas |
| **YDR** | Drawable | ✅ Texturas embebidas |
| **YFT** | Fragment | ✅ Texturas embebidas |

> **Nota:** Los archivos YFT suelen almacenar texturas en un archivo `.ytd` separado con el mismo nombre. Si un YFT muestra "Sin texturas embebidas", optimiza el YTD asociado.

## Requisitos

- Windows 10/11 (64-bit)
- El ejecutable incluye todo lo necesario (.NET 8 runtime incluido)

## Instalación

### Opción 1: Usar ejecutable (Recomendado)

1. Descarga la carpeta `dist/`
2. Ejecuta `FiveM Texture Optimizer.exe`
3. ¡Listo!

### Opción 2: Compilar desde código

Ver sección [Desarrollo](#desarrollo).

## Uso

### Interfaz Gráfica

1. **Seleccionar Carpeta Entrada** - Carpeta con archivos YTD/YDD/YDR/YFT
2. **Seleccionar Carpeta Salida** - Donde se guardarán los optimizados
3. **Escanear** - Analiza y lista todos los archivos
4. **Seleccionar** - Marca/desmarca archivos individuales
5. **Configurar tamaño** - Elige tamaño máximo (128-2048px)
6. **Optimizar** - Procesa los archivos seleccionados

### Línea de Comandos (Opcional)

```powershell
python optimize.py <carpeta_entrada> <carpeta_salida> [tamaño_max]
```

```powershell
# Ejemplo: texturas máximo 512px
python optimize.py input output 512
```

## Tamaños Recomendados

| Tamaño | Uso recomendado |
|--------|-----------------|
| 128px | Mínimo, baja calidad |
| 256px | Servidores con muchos jugadores |
| **512px** | **Balance calidad/rendimiento** |
| 1024px | Alta calidad |
| 2048px | Máxima calidad |

## Resultados Típicos

| Archivo | Original | Optimizado | Reducción |
|---------|----------|------------|-----------|
| catamaran.ytd | 3.9 MB | 0.4 MB | 90% |
| interior_club.ytd | 5.2 MB | 0.8 MB | 85% |
| vehicle_hd.ytd | 2.1 MB | 0.5 MB | 76% |

## Limitaciones

- ❌ **Archivos FXA** (assets compilados de FiveM con magic `FXAP`) no soportados - usa los archivos originales sin compilar
- ❌ **Archivos corruptos** - se copian sin modificar al destino

## Desarrollo

### Requisitos para compilar

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11

### Compilar la aplicación

```powershell
# Navegar al proyecto GUI
cd tools/YtdOptimizerGUI

# Compilar y publicar en dist/
dotnet publish -c Release -o ../../dist
```

### Estructura del Proyecto

```
├── dist/                    # Ejecutables distribuibles
│   ├── FiveM Texture Optimizer.exe  # Aplicación GUI
│   └── texconv.exe          # Conversor de texturas
├── tools/
│   ├── texconv.exe          # Microsoft DirectXTex
│   ├── YtdOptimizerGUI/     # Código fuente GUI (C# WinForms)
│   ├── YtdOptimizer/        # Herramienta CLI (C#)
│   └── codewalker/          # Librería CodeWalker.Core
├── optimize.py              # Script wrapper Python (opcional)
└── README.md
```

### Archivos del proyecto GUI

| Archivo | Descripción |
|---------|-------------|
| `YtdOptimizerGUI.csproj` | Configuración del proyecto |
| `Program.cs` | Punto de entrada |
| `MainForm.cs` | Formulario principal y lógica |
| `Models.cs` | Clases de datos |

## Créditos

- [CodeWalker](https://github.com/dexyfex/CodeWalker) - Librería para formatos GTA V
- [DirectXTex](https://github.com/microsoft/DirectXTex) - texconv.exe para procesamiento DDS

## Licencia

MIT
