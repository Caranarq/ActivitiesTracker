# README_BUILD.md

## Build local (desarrollo)

> Este repositorio define la estructura base de implementación en C#/.NET + WPF + SQLite.

### 1) Requisitos
- .NET SDK 8.0+
- Windows con workload de Desktop/WPF

### 2) Compilar solución
```bash
dotnet build src/ActivitiesTracker.sln
```

### 3) Publicar build portable self-contained (Windows 10)
```bash
dotnet publish src/UI/ActivitiesTracker.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish/
```

### 4) Ejecutable final
El ejecutable se encuentra en:

`publish/ActivitiesTracker.UI.exe`

Ese directorio puede copiarse y ejecutarse sin instalación con privilegios de administrador.
