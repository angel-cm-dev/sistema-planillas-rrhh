# ADR-0002 · `IAppDbContext` como puerto y SQLite para pruebas

- **Estado:** Aceptada
- **Fecha:** 2026-11

## Contexto

Necesitamos que la capa de aplicación consulte datos con filtros, proyecciones y paginación eficientes, y que las pruebas de integración corran en CI **sin Docker ni SQL Server**.

## Opciones

1. Repositorios genéricos (`IRepository<T>`) sobre EF Core.
2. Exponer una interfaz `IAppDbContext` con los `DbSet<T>` y `SaveChangesAsync`.

## Decisión

Opción 2. EF Core ya implementa *Unit of Work* y *Repository*; envolverlo en repositorios genéricos ocultaría `IQueryable` (y con él las proyecciones y la paginación en SQL) sin aportar aislamiento real.

El proveedor se elige por configuración (`Database:Provider`):

- **SqlServer** en producción, con **migraciones** versionadas.
- **Sqlite** en desarrollo local y pruebas (en memoria, `EnsureCreated`).

## Consecuencias

- ✔ Consultas expresivas y eficientes (`Select` a DTO, `Skip/Take` en BD).
- ✔ 23 pruebas de integración que levantan la API real en segundos.
- ✘ `Application` referencia el paquete `Microsoft.EntityFrameworkCore` (acoplamiento aceptado).
- ✘ SQLite difiere de SQL Server: no soporta `ORDER BY`/`SUM` sobre `decimal`. Se resolvió con una conversión `decimal → double` **solo** cuando el proveedor es SQLite (`AppDbContext.ConfigureConventions`). Las migraciones siguen siendo exclusivas de SQL Server.
