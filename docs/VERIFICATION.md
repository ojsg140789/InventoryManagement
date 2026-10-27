# Verificación real

Ejecución local del 27 de octubre de 2026. Esta evidencia no representa una prueba de carga ni una garantía para cualquier configuración de infraestructura.

## Entorno

- SDK .NET 10.0.402; proyectos net10.0.
- EF Core SQL Server 10.0.14; Swashbuckle 10.2.3.
- SQL Server local: ProductVersion 17.0.1000.7 (motor real instalado, no EF InMemory).
- Base nueva dedicada: InventoryManagementCodex_20261027, autenticación integrada.
- API local: http://localhost:5080, Development.
- Migración aplicada: 20261027152920_InitialCreate.
- No se crearon recursos Azure ni se ejecutaron despliegues.

## Compilación y pruebas

`dotnet build InventoryManagement.slnx --configuration Release --no-restore`: correcto, 0 errores, 0 advertencias tras actualizar la dependencia Pkcs.

`dotnet test InventoryManagement.slnx --configuration Release --no-build --no-restore --logger trx --results-directory artifacts/final-test-results`, con `INVENTORY_TEST_SQL_CONNECTION` configurada: **18 aprobadas, 0 fallos, 0 omitidas** (16 Domain, 1 Application, 1 integración Infrastructure). No se eliminaron ni sustituyeron las pruebas existentes.

La integración `ConcurrentOutputs_ShouldCommitOnlyOneMovement_WhenLastUnitIsRequestedTwice` sincroniza ambos comandos en `SavingChangesAsync`, tras leer y aplicar Withdraw en memoria, antes de enviar las escrituras. Verificó un éxito, un conflicto, saldo cero y exactamente una salida persistida. Creó una base aislada `InventoryIntegration_<guid>` y la eliminó al terminar. Sin conexión explícita esta prueba se omite, incluido el CI básico.

NuGet detectó System.Security.Cryptography.Pkcs 9.0.11 transitivo. Infrastructure fija 10.0.14 para usar una versión corregida. [Advisory de Microsoft](https://github.com/advisories/GHSA-846f-f764-76mx).

## HTTP y persistencia

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-api.ps1 -ConcurrencyRounds 3` terminó PASS a las 15:31:56 UTC y se repitió contra Release con PASS a las **15:48:46 UTC**.

| Escenario | Resultado observado |
| --- | --- |
| Readiness y OpenAPI | 200 |
| Crear producto y almacén | 201 |
| SKU/Code duplicados tras normalizar | 409 |
| Consultar listas y producto | 200 |
| Consultar Stock antes de inicializar | 404 |
| Entrada de 10 | 201; saldo 10 |
| Salida de 4 | 201; saldo 6 |
| Salida de 7 con saldo 6 | 409 |
| Cantidad 0 y negativa | 400 |
| Entrada int.MaxValue con saldo 6 | 400; saldo e historial conservados |
| Consulta de producto inexistente | 404 |
| Consulta Stock sin IDs | 400 |
| Historial del flujo simple | 2 movimientos, saldo 6 |

Tres rondas independientes iniciaron dos POST outputs antes de esperar cualquiera de las respuestas, cada una con Stock=1:

| Ronda | Respuestas | Stock final | Salidas persistidas |
| --- | --- | --- | --- |
| 1 | 201, 409 | 0 | 1 |
| 2 | 201, 409 | 0 | 1 |
| 3 | 201, 409 | 0 | 1 |

Cada ronda conservó exactamente dos movimientos totales: una entrada inicial y una salida. Las solicitudes concurrentes verifican el resultado observable; no fuerzan mediante una barrera interna que las dos lecturas ocurran antes del primer UPDATE.

Una consulta SQL independiente comparó cada Stock contra SUM(Entry - Exit), convirtiendo cantidades a bigint para no desbordar la suma. Resultado: **0 saldos inconsistentes/negativos** y **0 rowversions nulos**.

## No verificado o pendiente

- Docker Engine no estaba disponible; no se construyeron ni ejecutaron las imágenes/servicios de Compose. El archivo usa SQL Server 2022, mientras la verificación local usó el motor 17.0.
- `docker compose config --no-interpolate --quiet` terminó correctamente: validación sintáctica, no ejecución de contenedores.
- GitHub Actions preparado, sin ejecución remota y sin push.
- Azure solo documentado; ningún recurso creado.
- Sin prueba de carga, fallos de red durante commit, idempotencia o autorización.
- La integración sincronizada complementa las solicitudes HTTP simultáneas; ninguna de ellas constituye una prueba de estrés sostenida.
