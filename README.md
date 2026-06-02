# CentroP API

API REST para el sistema de gestión de farmacias CentroP. Fase 1 — solo lectura.

## Stack

- **ASP.NET Core 9.0** — framework web
- **Entity Framework Core 9** — ORM para consultas simples
- **Dapper 2** — micro-ORM para consultas SQL personalizadas
- **MediatR 12** — patrón CQRS
- **FluentValidation 11** — validación de requests
- **Serilog 8** — logging estructurado
- **HybridCache** — caché en memoria
- **Scalar** — UI interactiva para la documentación de la API

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- SQL Server (Express o superior)

## Configuración

Editá `src/CentroP.Api/appsettings.Development.json` con tu cadena de conexión:

```json
{
  "ConnectionStrings": {
    "CentroP": "Server=TU_SERVIDOR;Database=CentroP;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;"
  }
}
```

## Correr el proyecto

```bash
cd src/CentroP.Api
dotnet run
```

La API queda disponible en `http://localhost:5000`.

> **Windows 11:** Si Smart App Control está activado, bloqueará los DLLs compilados.
> Desactivarlo en **Configuración → Seguridad de Windows → Control de aplicaciones y explorador → Configuración de Smart App Control → Desactivado**.

## Documentación interactiva

Una vez levantado, abrí `http://localhost:5000/scalar/v1` para explorar y probar los endpoints desde el navegador.

## Endpoints

| Método | URL | Descripción |
|--------|-----|-------------|
| `GET` | `/api/v1/sucursales` | Lista paginada de sucursales |
| `GET` | `/api/v1/sucursales/{id}` | Sucursal por ID |
| `GET` | `/api/v1/provincias` | Lista de provincias |
| `GET` | `/api/v1/laboratorios` | Lista de laboratorios |
| `GET` | `/api/v1/clientes` | Lista paginada de clientes (con búsqueda opcional) |
| `GET` | `/api/v1/clientes/{id}` | Cliente por ID |
| `GET` | `/health` | Estado de la aplicación y la base de datos |
| `GET` | `/health/live` | Liveness probe (siempre 200) |

### Parámetros de paginación

Los endpoints paginados aceptan:

| Parámetro | Descripción | Default | Máximo |
|-----------|-------------|---------|--------|
| `page` | Número de página | 1 | — |
| `pageSize` | Resultados por página | 20 | 100 |

Los clientes además aceptan `search` para filtrar por nombre o apellido.

### Formato de respuesta paginada

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

## Estructura del proyecto

```
src/CentroP.Api/
├── Common/
│   ├── Behaviors/        # Validación automática en el pipeline de MediatR
│   ├── Exceptions/       # Excepciones personalizadas y sus manejadores
│   ├── Interfaces/       # Abstracciones (IDbConnectionFactory)
│   └── Pagination/       # Wrapper genérico de respuestas paginadas
├── Infrastructure/
│   ├── Cache/            # Constantes de claves de caché
│   └── Data/
│       ├── Configurations/  # Mapeo EF Core entidad → tabla SQL
│       ├── Entities/        # Clases que representan las tablas de la BD
│       └── CentroPDbContext.cs
└── Features/
    ├── Sucursales/       # Query + Handler + DTO + Endpoints
    ├── Provincias/
    ├── Laboratorios/
    └── Clientes/
```

## Rate limiting

100 requests por cada 60 segundos por IP. Superar el límite devuelve HTTP 429.
Configurable en `appsettings.json` bajo la clave `RateLimiting`.

## Logs

Se generan en la carpeta `logs/` con rotación diaria y retención de 30 días.
Cada request queda registrada con método, path, status code y tiempo de respuesta.
