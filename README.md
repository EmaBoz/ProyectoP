# CentroP API

API REST para el sistema de gestión de farmacias **CentroP**. Fase 1 — solo lectura.

---

## Propósito

CentroP API es el núcleo backend de un sistema integral de gestión farmacéutica. Expone datos de clientes, sucursales, laboratorios, productos y stock a través de endpoints REST versionados, seguros y paginados.

---

## Arquitectura

El proyecto adopta un enfoque de **Monolito Modular** organizado mediante **Vertical Slice Architecture (VSA)**:

- Cada funcionalidad de negocio vive en su propia *slice* bajo `Features/`, conteniendo sus queries, handlers, DTOs y validadores.
- No existe acoplamiento horizontal entre módulos: cada slice es autosuficiente.
- El patrón **CQRS** se implementa con **MediatR** como mediador, separando la intención (query/command) de su ejecución (handler).
- Un `ValidationBehavior` genérico en el pipeline de MediatR intercepta todos los requests y ejecuta los validadores de **FluentValidation** registrados, sin lógica de validación en los endpoints.

```
Request HTTP → Minimal API Endpoint → MediatR → ValidationBehavior → Handler → DB
```

---

## Stack tecnológico

| Componente | Tecnología | Rol |
|---|---|---|
| Framework web | ASP.NET Core 9 — Minimal APIs | Exposición de endpoints REST |
| Mediador / CQRS | MediatR 12 | Desacopla endpoints de handlers |
| Validación | FluentValidation 11 | Reglas de negocio en el pipeline |
| ORM (lecturas simples) | Entity Framework Core 9 | Consultas con HybridCache |
| Micro-ORM (consultas complejas) | Dapper 2 | Queries SQL transaccionales |
| Caché | HybridCache (.NET 9) | Datos de referencia de baja variabilidad |
| Logging | Serilog 8 | Logging estructurado con rotación diaria |
| Documentación interactiva | Scalar 2 | UI para explorar y testear la API |
| Health Checks | AspNetCore.HealthChecks | Estado de la app y la base de datos |

> **Regla de caché:** Los módulos transaccionales (Stock, Movimientos) nunca usan caché. Cada consulta va directo a la base de datos para garantizar consistencia.

---

## Requisitos previos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- SQL Server 2019 o superior (Express incluido)

---

## Configuración

### 1. Cadena de conexión

Editá `src/CentroP.Api/appsettings.Development.json` y reemplazá los valores de plantilla con los datos reales de tu instancia:

```json
{
  "ConnectionStrings": {
    "CentroP": "Server=TU_SERVIDOR\\SQLEXPRESS;Database=CentroP;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;"
  }
}
```

> **Recomendación de seguridad:** Para evitar commitear credenciales reales, usá [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):
> ```bash
> dotnet user-secrets set "ConnectionStrings:CentroP" "Server=...;Password=...;"
> ```

### 2. Compilar

```bash
dotnet build src/CentroP.Api/CentroP.Api.csproj
```

### 3. Levantar la API

```bash
cd src/CentroP.Api
dotnet run
```

La API queda disponible en `http://localhost:5000`.

> **Windows 11:** Si Smart App Control está activado, puede bloquear los DLLs compilados.
> Desactivarlo en **Configuración → Seguridad de Windows → Control de aplicaciones y explorador → Configuración de Smart App Control → Desactivado**.

### 4. Documentación interactiva

Con la API corriendo, abrí `http://localhost:5000/scalar/v1` para explorar y testear todos los endpoints desde el navegador.

---

## Endpoints

### Sucursales

| Método | URL | Descripción |
|---|---|---|
| `GET` | `/api/v1/sucursales` | Lista paginada de sucursales |
| `GET` | `/api/v1/sucursales/{id}` | Sucursal por ID |

### Provincias y Laboratorios

| Método | URL | Descripción |
|---|---|---|
| `GET` | `/api/v1/provincias` | Lista de provincias |
| `GET` | `/api/v1/laboratorios` | Lista de laboratorios |

### Clientes

| Método | URL | Descripción |
|---|---|---|
| `GET` | `/api/v1/clientes` | Lista paginada (búsqueda por nombre/apellido con `?search=`) |
| `GET` | `/api/v1/clientes/{id}` | Detalle completo del cliente |

### Stock

| Método | URL | Descripción |
|---|---|---|
| `GET` | `/api/v1/stock/actual` | Stock actual por producto, agrupado por sucursal |
| `GET` | `/api/v1/stock/movimientos` | Lista paginada de movimientos de stock |
| `GET` | `/api/v1/stock/movimientos/{idSucursal}/{id}` | Detalle de un movimiento con sus lotes |
| `GET` | `/api/v1/stock/lotes` | Stock desglosado por lote y vencimiento |

#### Filtros disponibles — `/api/v1/stock/movimientos`

| Parámetro | Tipo | Descripción |
|---|---|---|
| `idSucursal` | `int?` | Filtra por sucursal |
| `idProducto` | `int?` | Filtra por producto |
| `idTipoMovimiento` | `int?` | Filtra por tipo de movimiento |
| `fechaDesde` | `DateTime?` | Desde esta fecha (inclusive) |
| `fechaHasta` | `DateTime?` | Hasta esta fecha (inclusive). Debe ser ≥ `fechaDesde` |

#### Filtros disponibles — `/api/v1/stock/lotes`

| Parámetro | Tipo | Descripción |
|---|---|---|
| `idSucursal` | `int?` | Filtra por sucursal |
| `idProducto` | `int?` | Filtra por producto |
| `vencimientoHasta` | `DateTime?` | Lotes con vencimiento hasta esta fecha |

### Health Checks

| Método | URL | Descripción |
|---|---|---|
| `GET` | `/health` | Estado de la app y la conexión a SQL Server |
| `GET` | `/health/live` | Liveness probe (siempre HTTP 200) |

---

## Paginación

Todos los endpoints de lista aceptan:

| Parámetro | Default | Máximo | Descripción |
|---|---|---|---|
| `page` | `1` | — | Número de página |
| `pageSize` | `20` | `100` | Resultados por página |

**Formato de respuesta:**

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

---

## Estructura del proyecto

```
src/CentroP.Api/
├── Common/
│   ├── Behaviors/        # ValidationBehavior — validación automática en pipeline MediatR
│   ├── Exceptions/       # NotFoundException, handlers de ProblemDetails
│   ├── Interfaces/       # IDbConnectionFactory
│   └── Pagination/       # PagedResult<T>
├── Infrastructure/
│   ├── Cache/            # CacheKeys — constantes de claves de caché
│   └── Data/
│       ├── Configurations/  # IEntityTypeConfiguration — mapeo EF Core → SQL
│       ├── Entities/        # Entidades EF Core por módulo
│       └── CentroPDbContext.cs
└── Features/
    ├── Clientes/         # GetAllClientes, GetClienteById, Endpoints
    ├── Sucursales/       # GetAllSucursales, GetSucursalById, Endpoints
    ├── Provincias/       # GetAllProvincias, Endpoints
    ├── Laboratorios/     # GetAllLaboratorios, Endpoints
    └── Stock/            # GetStockActual, GetMovimientosStock,
                          # GetMovimientoStockById, GetLotesVencimiento, Endpoints
```

---

## Rate Limiting

100 requests por IP cada 60 segundos. Superar el límite devuelve **HTTP 429**.
Configurable en `appsettings.json` bajo la clave `RateLimiting`.

---

## Logs

Los logs se generan en `logs/` con rotación diaria y retención de 30 días.
Cada request HTTP queda registrada con método, path, status code y tiempo de respuesta en milisegundos.
