# Inventory Management Backend

API REST de inventario para practicar una entrevista Senior .NET. Conserva las entidades y las 17 pruebas originales, y añade Application, SQL Server, EF Core y ASP.NET Core sobre .NET 10. Sin frontend.

## Requisitos

- SDK .NET 10.
- SQL Server local con permiso para crear una base de desarrollo, o Docker Engine con contenedores Linux.
- PowerShell para el script de verificación.
- Acceso a NuGet para restaurar dependencias.

## Ejecutar con SQL Server local

Desde la raíz de esta solución, en PowerShell:

```powershell
dotnet restore InventoryManagement.slnx
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__Inventory = 'Server=localhost;Database=InventoryManagementDev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.Infrastructure
dotnet run --project Inventory.Api --no-launch-profile --urls http://localhost:5080
```

La conexión es un ejemplo para Windows Authentication; usa un servidor y una identidad propios. Para autenticación SQL proporciona la cadena mediante variables de entorno o un gestor de secretos. No la escribas en archivos versionados. `TrustServerCertificate=True` es exclusivamente para certificados locales; en un entorno desplegado usa un certificado válido y `TrustServerCertificate=False`.

- Swagger UI: http://localhost:5080/swagger
- OpenAPI: http://localhost:5080/swagger/v1/swagger.json
- Liveness: http://localhost:5080/health
- Readiness SQL: http://localhost:5080/health/ready (200 o 503).

No se ejecutan migraciones al arrancar normalmente. Para una imagen publicada sin SDK se puede ejecutar `dotnet Inventory.Api.dll --migrate`, con las mismas variables; aplica migraciones y termina. Separa esta operación del proceso de la API y usa una identidad de migración diferente en un entorno real.

## Migraciones

La migración inicial está en `Inventory.Infrastructure/Persistence/Migrations`, con su snapshot y un script SQL idempotente. Para cambios posteriores:

```powershell
dotnet ef migrations add NombreDelCambio --project Inventory.Infrastructure --startup-project Inventory.Infrastructure --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project Inventory.Infrastructure --startup-project Inventory.Infrastructure --output artifacts/migration.sql
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.Infrastructure
```

Revisa el script antes de aplicarlo a datos importantes. La API usa `Database.MigrateAsync` únicamente en el modo explícito `--migrate`; nunca `EnsureCreated`.

## Endpoints

| Método | Ruta | Resultado |
| --- | --- | --- |
| GET | /api/products?page=1&pageSize=50 | Productos, 200 |
| GET | /api/products/{id} | Producto, 200 o 404 |
| POST | /api/products | Crear producto, 201 |
| GET | /api/warehouses?page=1&pageSize=50 | Almacenes, 200 |
| POST | /api/warehouses | Crear almacén, 201 |
| GET | /api/stocks?productId={id}&warehouseId={id} | Existencias, 200 o 404 |
| POST | /api/stock-movements/entries | Entrada, 201 |
| POST | /api/stock-movements/outputs | Salida, 201 |
| GET | /api/stock-movements?productId={id}&warehouseId={id}&page=1&pageSize=50 | Historial, 200 |

En el historial los filtros son opcionales. Las listas tienen paginación: página 1–1000000 y tamaño 1–100; no incluyen conteo total. Los identificadores vacíos y cantidades no positivas devuelven 400. El SKU se normaliza con Trim/ToUpperInvariant (3–40), el código de almacén también (3–20); nombres de producto hasta 120 y almacén hasta 100.

El primer ingreso exitoso crea el Stock. Una combinación todavía no inicializada responde 404 al consultar y 409 al retirar. SKU/código duplicados, salidas insuficientes y conflictos concurrentes producen 409. Overflow produce 400 conservando el estado. Errores no previstos producen ProblemDetails 500 sin detalles SQL ni stack trace en la respuesta.

## Ejemplo HTTP completo

En otra terminal PowerShell:

```powershell
$base = 'http://localhost:5080'
$product = Invoke-RestMethod "$base/api/products" -Method Post -ContentType 'application/json' -Body '{"sku":"LAP-001","name":"Laptop"}'
$warehouse = Invoke-RestMethod "$base/api/warehouses" -Method Post -ContentType 'application/json' -Body '{"code":"MOR-001","name":"Morelia"}'
$body = @{productId=$product.id; warehouseId=$warehouse.id; quantity=10} | ConvertTo-Json
Invoke-RestMethod "$base/api/stock-movements/entries" -Method Post -ContentType 'application/json' -Body $body
Invoke-RestMethod "$base/api/stocks?productId=$($product.id)&warehouseId=$($warehouse.id)"
$body = @{productId=$product.id; warehouseId=$warehouse.id; quantity=4} | ConvertTo-Json
Invoke-RestMethod "$base/api/stock-movements/outputs" -Method Post -ContentType 'application/json' -Body $body
Invoke-RestMethod "$base/api/stock-movements?productId=$($product.id)&warehouseId=$($warehouse.id)"
```

Después de la entrada y la salida, Quantity es 6 y existen dos movimientos. Repetir la creación del mismo SKU/código produce 409.

## Verificación

```powershell
dotnet build InventoryManagement.slnx --configuration Release --no-restore
dotnet test InventoryManagement.slnx --configuration Release --no-build --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-api.ps1 -ConcurrencyRounds 3
```

Para habilitar la única prueba de integración SQL (requiere permiso para crear y eliminar una base aislada):

```powershell
$env:INVENTORY_TEST_SQL_CONNECTION = 'Server=localhost;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test InventoryManagement.slnx --configuration Release --no-build --no-restore
```

La prueba crea una base con nombre `InventoryIntegration_<guid>` y elimina únicamente esa base al terminar. Sin la variable se omite: quedan las 17 pruebas originales. Con SQL habilitado se ejecutan 18. Un interceptor de pruebas sincroniza ambos comandos antes de guardar, después de leer y retirar la unidad en memoria; así se comprueba también el caso de dos escritores con la misma versión original.

La última opción limita la política de ejecución al proceso y permite ejecutar este script local revisado sin modificar la política del sistema. El script necesita la API ejecutándose con SQL Server; crea datos con identificadores únicos y los conserva para inspección. No borra bases ni datos. Comprueba OpenAPI, altas, duplicados, consultas, entradas/salidas, invalidaciones, overflow e historial. En cada ronda concurrente inicia dos solicitudes HTTP antes de esperar sus respuestas y exige estados 201/409, saldo cero y un solo movimiento de salida.

Resultados reales y límites: [docs/VERIFICATION.md](docs/VERIFICATION.md).

## Docker local

```powershell
Copy-Item .env.example .env
# Edita .env y establece MSSQL_SA_PASSWORD antes de continuar.
docker compose up -d sqlserver
docker compose run --rm migrate
docker compose up -d api
docker compose logs -f api
```

Se usan SQL Server Developer y la API sin autenticación solo para desarrollo local. Los puertos publicados se enlazan a 127.0.0.1: API 5080 y SQL 14333. La API en contenedor usa el nombre sqlserver y puerto interno 1433. El volumen sql-data conserva la información. `docker compose down` detiene los servicios sin eliminar el volumen. No se requiere frontend ni un servicio Azure.

Docker no pudo ejecutarse en esta sesión: el daemon no estaba disponible. El backend sí se verificó con SQL Server local instalado. El ejemplo de Compose utiliza sa por simplicidad local; no es una configuración de producción.

## CI y estructura

`.github/workflows/backend-ci.yml` restaura, compila Release y ejecuta las pruebas existentes en Ubuntu; publica TRX. La integración SQL se omite cuando no hay conexión configurada. No despliega y no verifica SQL Server ni Docker. El workflow está preparado; su ejecución remota requiere un push autorizado.

- Inventory.Domain: invariantes, entidades y enums originales.
- Inventory.Application: interfaces, DTOs y nueve casos de uso.
- Inventory.Infrastructure: repositorios EF Core, Unit of Work, configuraciones y migraciones SQL Server.
- Inventory.Api: Controllers, DI, validación, ProblemDetails, Swagger y health checks.
- Inventory.Domain.Test / Inventory.Application.Tests: pruebas originales.
- Inventory.Infrastructure.Tests: una prueba de integración concurrente contra SQL Server real, habilitada explícitamente.

## Concurrencia y límites

`StockMovementRepository.RegisterAsync` lee Stock con tracking dentro de una transacción ReadCommitted. EF añade el rowversion original al UPDATE. Un conflicto revierte Stock y movimiento; no se reintenta automáticamente. La clave compuesta protege también la primera entrada simultánea. La base añade índices únicos y checks de cantidades.

No hay autenticación, autorización, idempotency keys, reservas, transferencias, borrados ni ediciones de movimientos. Una respuesta perdida tras commit requiere conciliación manual; reenviar un POST podría duplicar la operación. El proceso rechaza ambientes distintos de Development para impedir desplegar accidentalmente este MVP abierto. Antes de exposición pública hacen falta identidad, permisos por almacén, TLS, gestión de secretos, protección de abuso y observabilidad operativa.

[Arquitectura](docs/ARCHITECTURE.md) · [Guía EPAM](docs/EPAM_BACKEND_INTERVIEW_GUIDE.md) · [Preparación Azure sin aprovisionar](docs/AZURE_PREPARATION.md)
