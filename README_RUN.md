# README_RUN.md

## Ejecutar ActivitiesTracker (V1)

### 1) Requisitos para ejecutar
- Windows 10 x64.
- Carpeta publicada portable (self-contained) generada con `dotnet publish`.
- **No** se requiere Visual Studio para ejecutar.

### 2) OAuth en Google Cloud (Desktop App)
1. Entra a Google Cloud Console.
2. Crea (o selecciona) un proyecto.
3. Habilita la **Google Sheets API**.
4. Ve a **APIs & Services > Credentials**.
5. Crea credenciales **OAuth Client ID**.
6. Tipo de aplicación: **Desktop app**.
7. Descarga el archivo JSON de credenciales.
8. Renómbralo a `credentials.json`.

### 3) Scopes requeridos
- `https://www.googleapis.com/auth/spreadsheets`
- `openid`
- `email`
- `profile`

### 4) Dónde colocar `credentials.json`
Coloca el archivo en:

`%LOCALAPPDATA%\ActivitiesTracker\credentials.json`

La aplicación también muestra esta ruta en la pestaña **Settings**.

### 5) Flujo de primera autenticación
1. Abre la app.
2. Si no hay token válido, la app abrirá el flujo interactivo OAuth (pendiente de integración completa en siguiente iteración).
3. Tras autorizar, el token local quedará almacenado en almacenamiento seguro del usuario (objetivo V1).

### 6) Logs locales
Los logs se guardan en:

`%LOCALAPPDATA%\ActivitiesTracker\logs\`

Incluyen eventos de sync, errores de OAuth, errores de validación y conflictos de sincronización.
