# YTD Optimizer para GTA V / FiveM

Optimiza archivos .ytd reduciendo el tamano de texturas (hasta 74% de reduccion).

## Requisitos

- Python 3.8+

## Instalacion

```powershell
# texconv.exe se descarga automaticamente si no existe
# Si falta, descargalo de: https://github.com/microsoft/DirectXTex/releases
```

## Uso

```powershell
# Colocar archivos .ytd en carpeta "input"
# Ejecutar:
python batch_optimize.py input output -s 512
```

### Parametros

| Parametro | Descripcion | Default |
|-----------|-------------|---------|
| input | Carpeta con .ytd originales | - |
| output | Carpeta de salida | - |
| -s, --size | Tamano max textura | 512 |

### Ejemplos

```powershell
python batch_optimize.py mis_ytd optimizados -s 512   # Calidad media
python batch_optimize.py mis_ytd optimizados -s 256   # Mas pequeno
python batch_optimize.py mis_ytd optimizados -s 1024  # Alta calidad
```

## Resultados

| Textura Original | Reduccion |
|------------------|-----------|
| 2048x4096 | ~97% |
| 2048x2048 | ~91% |
| 1024x1024 | ~65% |

## Estructura

```
ytd_optimizer/
 batch_optimize.py   # Script principal
 extract_textures.py # Uso individual
 rebuild_ytd.py      # Uso individual
 tools/
    texconv.exe
 README.md
```

## Archivos

- **batch_optimize.py** - Procesa multiples YTD
- **extract_textures.py** - Extrae texturas a DDS  
- **rebuild_ytd.py** - Reconstruye YTD con nueva textura
