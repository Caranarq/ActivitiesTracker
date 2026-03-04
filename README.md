# ActivitiesTracker

Aplicación desktop portable para Windows 10, offline-first, para registrar:
- Eventos (duration / instant / open-ended)
- Activity segments para Daily Cycles (incluyendo Sleep como categoría normal)
- Cola de cambios local para sincronización con Google Sheets

## Estructura
- `src/Domain`: entidades y contratos de dominio
- `src/Infrastructure`: SQLite, rutas locales, logging
- `src/Sync`: servicio de cola/sync
- `src/UI`: app WPF

Consulta `README_BUILD.md` y `README_RUN.md` para compilar/ejecutar.
