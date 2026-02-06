# YTD Optimizer para GTA V / FiveM

Optimiza archivos .ytd reduciendo el tamaño de texturas (hasta 90% de reducción).

## Requisitos

- .NET SDK 8.0+
- Python 3.8+ (opcional, para el wrapper)

## Instalación

```powershell
# Instalar .NET SDK si no está instalado
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "dotnet-install.ps1"
./dotnet-install.ps1
```

## Uso

```powershell
python optimize.py <carpeta_entrada> <carpeta_salida> [tamaño_max]
```

### Ejemplos

```powershell
python optimize.py optimize output 512    # Calidad media (recomendado)
python optimize.py optimize output 256    # Más pequeño, menos calidad
python optimize.py optimize output 1024   # Alta calidad
```

### Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| carpeta_entrada | Carpeta con archivos .ytd originales |
| carpeta_salida | Carpeta donde guardar los .ytd optimizados |
| tamaño_max | Tamaño máximo de textura (default: 512) |

## Resultados

| Archivo | Original | Optimizado | Reducción |
|---------|----------|------------|-----------|
| catamaran.ytd | 3.9 MB | 0.4 MB | 90% |
| skidoo800R.ytd | 0.5 MB | 0.3 MB | 52% |

## Estructura

```
optimize_proyect/
├── optimize.py          # Script principal
├── optimize/            # YTDs de entrada (ejemplo)
├── output/              # YTDs optimizados
├── tools/
│   ├── texconv.exe      # Herramienta de conversión
│   ├── YtdOptimizer/    # Proyecto C# con CodeWalker.Core
│   └── CodeWalker/      # Librería para manejo de YTD
└── README.md
```
