# Guía de entrevista EPAM — backend implementado

Explicaciones basadas en este repositorio, sin inventar experiencia profesional ni métricas. Las respuestas en inglés están pensadas para unos 30–60 segundos, según ritmo. Las rutas son relativas a la raíz de la solución. Los conceptos no utilizados se identifican como alternativas o microejercicios.

Ver [arquitectura](ARCHITECTURE.md) y [evidencia real](VERIFICATION.md).

## 1. OOP, encapsulamiento, herencia y polimorfismo

**Explicación:** Stock protege Quantity mediante setters privados y operaciones Receive/Withdraw. Las entidades no necesitan herencia. El polimorfismo se aplica mediante IStockRepository.

**Archivo y método:** Inventory.Domain/Entities/Stock.cs: Receive/Withdraw; Inventory.Application/Abstractions/IStockRepository.cs.

**Funcionamiento interno:** El consumidor llama al contrato y DI proporciona la implementación. Composición separa responsabilidades; herencia requiere una relación is-a y sustitución válida.

**Cuándo usarlo, trade-offs y errores comunes:** Para conservar invariantes y sustituir dependencias. Una jerarquía artificial aumenta acoplamiento. Evitar setters públicos y creer que encapsulamiento resuelve concurrencia SQL.

**Interview question:** How does your domain apply object-oriented principles?

**Natural answer:** Stock owns its quantity and exposes operations rather than public setters. Receive checks overflow, and Withdraw rejects insufficient stock. Application uses interface polymorphism to access persistence without knowing the concrete repository. I did not introduce an entity inheritance hierarchy because there is no useful substitution relationship in this model. Composition makes the dependencies explicit. These boundaries protect local invariants, while database concurrency still needs a separate mechanism across requests and processes.

## 2. SOLID

**Explicación:** SRP separa dominio, coordinación, SQL y HTTP. DIP invierte dependencias hacia contratos internos. ISP mantiene contratos pequeños; LSP exige respetarlos y OCP permite otras implementaciones.

**Archivo y método:** GetStockUseCase.ExecuteAsync y GetStockUseCaseTests.ExecuteAsync_ShouldReturnStock_WhenStockExists.

**Funcionamiento interno:** El constructor recibe IStockRepository y la prueba usa Moq para sustituirlo. El caso de uso no cambia entre mock y repositorio SQL.

**Cuándo usarlo, trade-offs y errores comunes:** Aplicar en límites con responsabilidades distintas; demasiadas interfaces dificultan navegación. Evitar un repositorio genérico que exponga IQueryable o una interfaz por clase sin propósito.

**Interview question:** Where do you apply SOLID without overengineering?

**Natural answer:** I apply SOLID at boundaries that change for different reasons. Domain owns inventory rules, use cases coordinate operations, repositories implement persistence, and controllers translate HTTP. GetStockUseCase depends on a small repository interface and can run with a mock. I do not add abstractions just to satisfy an acronym. For example, a generic repository would obscure the specific atomic stock-and-movement operation rather than make this application easier to understand or maintain.

## 3. Interfaces y genéricos

**Explicación:** Interfaces expresan capacidades sin elegir implementación. Task<T>, IReadOnlyList<T> y DbSet<T> reutilizan estructura con tipos seguros.

**Archivo y método:** IProductRepository.ListAsync; InventoryDbContext.Products; Contracts/InventoryDtos.cs.

**Funcionamiento interno:** Los genéricos conservan tipos sin casts. IReadOnlyList limita la API de escritura, pero no convierte el objeto subyacente en una copia inmutable.

**Cuándo usarlo, trade-offs y errores comunes:** Útiles en límites y contenedores reutilizables. Los contratos específicos cuestan más declaraciones, pero expresan intención. Evitar devolver consultas diferidas que requieren un contexto ya dispuesto.

**Interview question:** Why specific repository interfaces instead of a generic repository?

**Natural answer:** The contracts describe operations the application actually needs, such as finding stock by product and warehouse or registering a movement atomically. A generic CRUD interface would not express that transaction guarantee well. I still use generics for tasks, collections, and entity sets because they provide clear type safety. IReadOnlyList prevents mutation through that interface, but it is not deep immutability. The goal is a useful contract, not a duplicate of every Entity Framework API.

## 4. List, Dictionary y HashSet

**Explicación:** List conserva orden y duplicados. Dictionary asocia claves y valores. HashSet representa pertenencia y unicidad según un comparador.

**Archivo y método:** ProductRepository.ListAsync materializa List<Product>. Dictionary/HashSet son alternativas de microejercicio, no funcionalidades añadidas.

**Funcionamiento interno:** List indexa en O(1) y busca linealmente. Colecciones hash buscan en O(1) promedio, con memoria, hashing y colisiones.

**Cuándo usarlo, trade-offs y errores comunes:** List para páginas ordenadas; Dictionary para búsquedas repetidas; HashSet para deduplicación local. Ninguna garantiza unicidad entre servidores. Evitar claves mutables y cargar tablas completas para filtrar en memoria.

**Interview question:** When would you use Dictionary or HashSet here?

**Natural answer:** The current queries return ordered pages, so List is a natural result. A Dictionary could help with repeated lookups over data that is already loaded, and a HashSet could deduplicate identifiers in a future batch operation. Neither is necessary in the MVP. I would not use an in-memory set to enforce unique SKUs because separate application instances would have separate sets. The SQL unique index is the authoritative guarantee across all writers.

## 5. LINQ y delegados

**Explicación:** LINQ compone filtros y transformaciones. Un delegado ejecuta comportamiento; un árbol de expresiones representa código como datos.

**Archivo y método:** ProductRepository.ListAsync: OrderBy/Skip/Take; GetProductsUseCase.ExecuteAsync: Select(ProductDto.From).

**Funcionamiento interno:** EF recibe Expression<Func<T,bool>> y traduce SQL. Después de ToListAsync, Select utiliza un delegado .NET. La enumeración puede ser diferida.

**Cuándo usarlo, trade-offs y errores comunes:** Componer antes de materializar. El costo aparece al ejecutar; evitar ToList antes de Where, múltiples enumeraciones SQL o métodos no traducibles en filtros.

**Interview question:** What is the difference between a delegate and an expression tree?

**Natural answer:** A delegate is executable behavior, while an expression tree describes behavior as data. Predicates over DbSet become expression trees that EF translates into parameterized SQL. Once the repository materializes a page, the use case maps entities to DTOs using an ordinary delegate through Select. That distinction matters because not every .NET method can be translated. I keep filtering, ordering, and pagination on the database side and materialize only when the query is complete.

## 6. Value types y reference types

**Explicación:** Guid, int y DateTimeOffset son structs. Entidades, string y los record class DTO son reference types; record no significa struct.

**Archivo y método:** Stock.Quantity/ProductId; StockMovement.CreatedAt; Contracts/InventoryDtos.cs.

**Funcionamiento interno:** La asignación copia valor o referencia. Structs pueden estar inline, en pila o boxed en heap; igualdad por valor de un record class no cambia su categoría.

**Cuándo usarlo, trade-offs y errores comunes:** Valores pequeños para semántica de copia, clases para identidad. Copiar structs grandes/boxing cuesta; compartir referencias mutables requiere cuidado. Evitar afirmar que todo value type vive en la pila.

**Interview question:** Are records value types?

**Natural answer:** The DTOs here are record classes, so they are reference types even though records provide value-based equality. Guid and DateTimeOffset are structs with value semantics. Copying a Guid copies its value, whereas copying a Stock variable copies a reference to the same entity. Storage is a separate issue: value types can be embedded inside heap objects or boxed. I would not describe all structs as stack allocations or assume a record is deeply immutable.

## 7. Garbage Collector y recursos

**Explicación:** El GC recupera memoria administrada inalcanzable; Dispose libera recursos de manera determinista.

**Archivo y método:** StockMovementRepository.RegisterAsync: await using transaction; DatabaseHealthCheck.CheckHealthAsync: scope.

**Funcionamiento interno:** El GC rastrea raíces y generaciones. Disponer conexiones/transacciones devuelve recursos al pool y permite rollback sin esperar una recolección.

**Cuándo usarlo, trade-offs y errores comunes:** Usar using/await using con recursos. Grandes listas y retención elevan presión de memoria. Evitar GC.Collect rutinario, contextos globales y esperar al GC para cerrar transacciones.

**Interview question:** Why use await using if .NET has a garbage collector?

**Natural answer:** The garbage collector manages memory, but a database transaction has a lifetime that must end predictably. The repository uses await using so failed operations dispose the transaction and roll back before leaving the method. Scoped contexts also release their resources at the end of a request. That is different from waiting for an object to become collectible. For performance issues, I would investigate allocation and retention patterns before considering any GC configuration changes.

## 8. Async/await, Task y ThreadPool

**Explicación:** Async evita bloquear un hilo durante la espera de I/O. Task representa una operación, no un hilo dedicado.

**Archivo y método:** GetStockUseCase.ExecuteAsync; StockRepository.GetByProductAndWarehouseAsync; UnitOfWork.SaveChangesAsync.

**Funcionamiento interno:** El compilador genera una máquina de estados. La espera libera capacidad del servidor y la continuación retoma al completarse I/O.

**Cuándo usarlo, trade-offs y errores comunes:** Async de extremo a extremo para EF. No vuelve rápido un SQL lento ni vuelve thread safe DbContext. Evitar .Result, .Wait, async void y Task.Run para envolver EF async.

**Interview question:** Does an async database call require a dedicated thread?

**Natural answer:** Not necessarily. Task represents completion of an operation, not ownership of a thread. While the database is doing I/O, the server can release the request thread and resume the async state machine when the result arrives. This improves resource usage under concurrent requests, but it does not fix a slow query. I use EF's async APIs directly, propagate cancellation, and avoid Task.Run or parallel operations on the same DbContext.

## 9. Paralelismo, concurrencia, carreras y thread safety

**Explicación:** Concurrencia superpone progreso; paralelismo ejecuta al mismo tiempo. Una race condition depende de la intercalación de accesos a estado.

**Archivo y método:** StockMovementRepository.RegisterAsync; scripts/verify-api.ps1: dos PostAsync antes de esperar.

**Funcionamiento interno:** Cada petición tiene su contexto, pero comparte la base. Dos lecturas pueden observar el mismo saldo. El token SQL detecta escrituras obsoletas.

**Cuándo usarlo, trade-offs y errores comunes:** Concurrencia para solicitudes y paralelismo para trabajo independiente. Más simultaneidad aumenta conflictos/pool usage. Evitar lock local como garantía distribuida y compartir DbContext.

**Interview question:** Why is a C# lock insufficient for stock consistency?

**Natural answer:** A C# lock coordinates callers that share that exact lock object in one process. It cannot protect another API instance or another client writing to the same database. Here the shared consistency boundary is SQL Server. Each request uses its own DbContext, rowversion detects stale writes, and the transaction keeps stock and history atomic. The HTTP script starts overlapping withdrawals and checks both responses and persisted state, rather than assuming a private setter provides concurrency safety.

## 10. CancellationToken

**Explicación:** Cancelación cooperativa propagada desde HTTP a consultas, guardado y commit.

**Archivo y método:** Controllers y StockMovementRepository.RegisterAsync: cancellationToken; ApiExceptionHandler.TryHandleAsync.

**Funcionamiento interno:** El token no termina un hilo forzosamente. Las operaciones observan cancelación y pueden lanzar OperationCanceledException.

**Cuándo usarlo, trade-offs y errores comunes:** Usarlo en I/O largo. Cerca de commit el resultado puede ser ambiguo para el cliente. Evitar CancellationToken.None dentro de repositorios y reintentar POST automáticamente tras desconexión.

**Interview question:** What happens if the client disconnects during a movement?

**Natural answer:** The request token flows into EF queries, saving, and commit so the server can stop unnecessary work cooperatively. If an operation fails before commit, disposing the transaction rolls it back. But a disconnect around commit can leave the client uncertain even when the database committed successfully. The MVP has no idempotency keys, so I document that limitation and avoid automatic movement retries. Cancellation is a signal to stop work, not a distributed undo guarantee.

## 11. Middleware y excepciones centralizadas

**Explicación:** El pipeline procesa componentes ordenados; un handler exterior captura errores posteriores y los traduce a HTTP.

**Archivo y método:** Program: UseExceptionHandler/UseStatusCodePages/MapControllers; ApiExceptionHandler.TryHandleAsync.

**Funcionamiento interno:** IExceptionHandler clasifica excepciones y usa IProblemDetailsService. Los detalles internos se registran en servidor; 500 devuelve un mensaje genérico.

**Cuándo usarlo, trade-offs y errores comunes:** Centralizar para evitar try/catch repetidos. Orden y dependencias importan. Evitar stack traces en respuestas o convertir todo error en 400.

**Interview question:** How does your API handle exceptions consistently?

**Natural answer:** The exception handler is placed before the endpoints and maps known application failures to controlled ProblemDetails responses. Invalid arguments become 400, absent resources become 404, and inventory conflicts become 409. Unexpected errors return a generic 500 and are logged with a trace identifier. SQL details are not exposed to clients. This keeps controllers small and avoids making Application depend on HTTP status codes or ASP.NET exception types.

## 12. DI: Singleton, Scoped y Transient

**Explicación:** Singleton dura la aplicación; Scoped dura el scope; Transient se crea en cada resolución. DI arma el grafo por constructor.

**Archivo y método:** DependencyInjection.AddInfrastructure; Program; DatabaseHealthCheck.CheckHealthAsync.

**Funcionamiento interno:** DbContext/repositorios/casos son Scoped. IExceptionHandler es Singleton sin repositorios. HealthCheck abre scope para acceder a EF.

**Cuándo usarlo, trade-offs y errores comunes:** Scoped para unidad de trabajo HTTP; Singleton para compartidos seguros; Transient para lógica ligera. Evitar captive dependencies y asumir que Scoped garantiza thread safety.

**Interview question:** Why are repositories scoped?

**Natural answer:** Repositories depend on a scoped DbContext representing the request's unit of work. They share that context within the request and are disposed with it. A singleton repository would capture an inappropriate short-lived dependency or require a different factory-based design. The exception handler can be singleton because it does not capture repositories. The health check explicitly opens a scope for database access. Transient services are useful for lightweight independent behavior, but the MVP does not need them.

## 13. Routing, model binding y validación

**Explicación:** Routing elige acción; binding convierte ruta/query/JSON; validación comprueba forma e invariantes.

**Archivo y método:** ProductsController.Get: id:guid; StocksController.Get: BindRequired; Requests.cs: Required/Range; InputRules.

**Funcionamiento interno:** ApiController produce ValidationProblemDetails 400. Constructores del dominio validan también fuera de HTTP; una ruta no coincidente puede devolver 404.

**Cuándo usarlo, trade-offs y errores comunes:** Metadatos para transporte y reglas centrales en dominio. Evitar validación solo en Swagger, olvidar query obligatoria o duplicar reglas completas en cada capa.

**Interview question:** Why validate both HTTP requests and domain objects?

**Natural answer:** HTTP validation checks whether a request can be interpreted correctly, including required fields, numeric ranges, and binding failures. Domain objects still enforce invariants because they can be created through tests, background tasks, or another adapter. Stock checks overflow independently of MVC. I keep these checks complementary rather than duplicating every business rule in controllers. Transport validation improves client feedback, while the domain remains responsible for valid entities and valid state transitions.

## 14. REST, status codes y ProblemDetails

**Explicación:** GET consulta sin mutar; POST crea recursos. DTOs y ProblemDetails separan representación pública de persistencia.

**Archivo y método:** Controllers: List/Get/Create/Entry/Output; ApiExceptionHandler.TryHandleAsync.

**Funcionamiento interno:** 201 al crear, 200 al consultar, 400 para entrada inválida, 404 ausente, 409 conflicto con estado, 500 inesperado. traceId correlaciona errores.

**Cuándo usarlo, trade-offs y errores comunes:** 409 permite comunicar conflictos recuperables mediante decisión del cliente. Evitar GET mutante, 200 con error escondido y retry ciego de POST.

**Interview question:** Why return 409 for insufficient stock?

**Natural answer:** The request is structurally valid, but it conflicts with the current inventory state. That is a controlled business outcome rather than a server crash, so 409 is appropriate. Invalid quantities return 400, missing resources return 404, and successful creation returns 201. ProblemDetails gives clients a predictable error shape with a trace identifier. Clients should inspect current state before submitting a new movement instead of blindly retrying every conflict or timeout.

## 15. Swagger/OpenAPI

**Explicación:** OpenAPI describe contratos; Swagger UI los presenta para exploración y pruebas manuales.

**Archivo y método:** Program: AddSwaggerGen/UseSwaggerUI; Controllers: ProducesResponseType; /swagger/v1/swagger.json.

**Funcionamiento interno:** Swashbuckle usa ApiExplorer y metadatos MVC. El documento no ejecuta reglas ni verifica SQL.

**Cuándo usarlo, trade-offs y errores comunes:** Útil para descubrimiento local. Mantener metadatos alineados; restringir exposición futura. Evitar presentar Swagger como prueba de consistencia o mecanismo de seguridad.

**Interview question:** What does Swagger prove about the system?

**Natural answer:** Swagger shows that the API can describe its endpoints and contracts, and its UI helps developers send requests. It does not prove concurrency safety, transactional consistency, or authorization. I verify the document is reachable, then run actual HTTP scenarios and inspect stock and movement history. Successful create actions are documented as 201. The current API runs only in development, and public deployment remains blocked until authentication and authorization are implemented.

## 16. Autenticación y autorización

**Explicación:** Autenticación determina identidad; autorización decide permiso sobre recursos. Ninguna está implementada en el MVP.

**Archivo y método:** Program: IsDevelopment guard; docs/AZURE_PREPARATION.md.

**Funcionamiento interno:** Una futura validación de token debe complementarse con políticas, por ejemplo por almacén. El warehouseId del cuerpo no prueba permiso.

**Cuándo usarlo, trade-offs y errores comunes:** Necesarias antes de exposición pública. El guard reduce despliegues accidentales; no reemplaza red segura. Evitar confundir CORS, ocultar Swagger o tener token con autorización suficiente.

**Interview question:** What is the main security limitation?

**Natural answer:** The MVP has no authentication or authorization, so anyone who can reach its development endpoint can modify inventory. Program rejects non-development environments, and Compose publishes only loopback ports. Before public deployment I would validate identities, authorize operations at resource level, require TLS, protect configuration, and add operational safeguards. Hiding Swagger or enabling CORS would not address the missing access controls. The Azure document is preparation only, not a production deployment claim.

## 17. Clean Architecture y Dependency Inversion

**Explicación:** Las referencias apuntan hacia Domain/Application. Api es el composition root e Infrastructure implementa contratos internos.

**Archivo y método:** Inventory.Application.csproj; Inventory.Infrastructure.csproj; Inventory.Api/Program.cs.

**Funcionamiento interno:** El flujo runtime puede salir hacia SQL mientras la dependencia de compilación apunta al contrato en Application. Domain permanece sin EF/HTTP.

**Cuándo usarlo, trade-offs y errores comunes:** Útil para aislar reglas y sustituir adaptadores; más proyectos implican navegación. Evitar referencias Infrastructure desde Application o IQueryable escapando del repositorio.

**Interview question:** How can Application call Infrastructure without depending on it?

**Natural answer:** Application calls an interface that it owns. At runtime, the composition root supplies an Infrastructure implementation of that interface. The direction of the runtime call differs from the direction of the compile-time reference. GetStockUseCase can therefore query stock without knowing EF Core or SQL Server. The project dependencies enforce the boundary, and the existing mock-based test exercises the same use case with a different repository implementation.

## 18. Repository y Unit of Work

**Explicación:** Repository encapsula consultas; Unit of Work agrupa cambios. EF ya implementa gran parte de estos mecanismos.

**Archivo y método:** IUnitOfWork; UnitOfWork.SaveChangesAsync; IStockMovementRepository.RegisterAsync.

**Funcionamiento interno:** Catálogos añaden y guardan. RegisterAsync abre transacción, carga Stock, aplica dominio, inserta historial, guarda y confirma.

**Cuándo usarlo, trade-offs y errores comunes:** Contratos específicos expresan intención y atomicidad. Un CRUD genérico ocultaría la operación importante. Evitar SaveChanges por cada Add o contextos distintos para Stock e historial.

**Interview question:** Where is the transaction boundary?

**Natural answer:** For a movement, the boundary is RegisterAsync on the movement repository contract. It loads stock, invokes the domain operation, adds history, saves, and commits one transaction. Application coordinates references but never accesses DbContext or provider transaction types. Catalog creation uses a smaller unit-of-work contract around SaveChanges. I did not add a generic repository framework because EF already provides tracking and transactional persistence; the abstraction here makes the business-relevant atomic operation explicit.

## 19. Strategy, Factory Method, Builder, Singleton y patrones GoF

**Explicación:** Strategy intercambia algoritmos; Factory Method delega creación polimórfica; Builder construye por pasos; Singleton controla instancia. Repository/UoW son patrones empresariales.

**Archivo y método:** No se añadieron al runtime sin necesidad. Los microejercicios al final muestran las variantes. AddInfrastructure no se presenta como Factory Method GoF.

**Funcionamiento interno:** Strategy usa composición; Factory Method un método de creación sobreescribible; Builder acumula configuración; Singleton requiere publicación segura y estado thread safe.

**Cuándo usarlo, trade-offs y errores comunes:** Usar cuando haya variación o construcción compleja. Añaden indirection y pueden esconder estado global. Evitar Singleton para DbContext y llamar Factory Method a cualquier Create estático.

**Interview question:** Which patterns did you deliberately avoid?

**Natural answer:** I did not add Strategy or Factory Method because the current MVP has no interchangeable algorithm or useful creation hierarchy. Repository and Unit of Work describe persistence boundaries and are enterprise patterns rather than GoF patterns. Builder and Singleton are shown as separate exercises, not claimed as production features. A static singleton DbContext would be incorrect. DI lifetime management is also distinct from writing a Singleton class; patterns should solve a demonstrated problem.

## 20. EF Core, Change Tracking y AsNoTracking

**Explicación:** EF traduce consultas y materializa entidades. Tracking conserva identidad y valores originales; NoTracking evita ese costo en consultas de lectura.

**Archivo y método:** StockRepository.GetByProductAndWarehouseAsync: AsNoTracking; StockMovementRepository.RegisterAsync: consulta tracked.

**Funcionamiento interno:** El tracker retiene rowversion original y detecta Quantity modificada. SaveChanges genera UPDATE con ese token.

**Cuándo usarlo, trade-offs y errores comunes:** Tracking para modificar dentro de unidad de trabajo; NoTracking para consultas. Evitar quitar tracking a la lectura que necesita token o usar contextos de vida larga.

**Interview question:** Why is the movement's stock read tracked?

**Natural answer:** The operation needs the original rowversion for concurrency checking. EF retains that token when it tracks stock, then includes it in the update predicate after the domain changes quantity. Read-only endpoints use AsNoTracking because they do not need original values or identity tracking. Contexts are scoped to individual requests, which keeps tracked state bounded. Tracking is a persistence mechanism, not an audit history; StockMovement is the separate business record.

## 21. Migraciones

**Explicación:** Una migración versiona cambios de esquema; el snapshot describe el modelo anterior para comparar.

**Archivo y método:** Persistence/Migrations/20261027152920_InitialCreate.cs: Up/Down; InventoryDbContextFactory.CreateDbContext; Program: --migrate.

**Funcionamiento interno:** EF registra migraciones aplicadas en la base. El SQL idempotente consulta el historial para no repetirlas.

**Cuándo usarlo, trade-offs y errores comunes:** Aplicar con identidad separada y revisión SQL. Idempotencia de migración no corrige drift arbitrario ni vuelve seguros cambios destructivos. Evitar EnsureCreated junto con migraciones o migrar desde cada réplica.

**Interview question:** How would you apply schema changes safely?

**Natural answer:** I generate a migration, review its SQL, and apply it as a separate deployment step rather than from every API instance. This repository includes the initial migration, model snapshot, and idempotent script. A published image can run an explicit migration mode and then exit. In a real environment I would use a separate identity, verify compatibility with the application version, and plan backup and recovery before destructive changes. Idempotent does not mean risk-free.

## 22. LINQ to Entities, joins e índices

**Explicación:** LINQ SQL filtra en servidor. Joins relacionan filas; índices aceleran acceso y pueden garantizar unicidad.

**Archivo y método:** ProductRepository.ListAsync/SkuExistsAsync; EntityConfigurations: índices únicos y compuesto de historial.

**Funcionamiento interno:** El proveedor traduce expresiones y el optimizador decide el plan según estadísticas. El prefijo de un índice compuesto importa.

**Cuándo usarlo, trade-offs y errores comunes:** SKU/Code únicos y búsqueda Stock por PK. Futuros nombres en historial podrían proyectarse con joins. Más índices cuestan escrituras/espacio. Evitar añadirlos sin medir o filtrar después de materializar.

**Interview question:** Does the history index support all filters equally?

**Natural answer:** No. Its leading columns are product and warehouse, followed by creation time, so queries constrained by that key sequence benefit most. A warehouse-only query may need another access path. I would inspect actual plans and workload before adding an index, because indexes consume storage and increase write cost. The current API limits page size and performs filtering in SQL. If history later needs names, I would consider a projection or join rather than per-row queries.

## 23. N+1 y optimización de consultas

**Explicación:** N+1 es una consulta inicial seguida de una por fila. El MVP no usa lazy loading ni lecturas de navegaciones en un foreach.

**Archivo y método:** StockMovementRepository.ListAsync; GetStockMovementsUseCase.ExecuteAsync.

**Funcionamiento interno:** Una consulta materializa una página; el mapeo DTO no dispara I/O. Skip/Take limita resultados con orden estable.

**Cuándo usarlo, trade-offs y errores comunes:** Medir comandos, latencia y planes; proyectar lo necesario. Páginas profundas pueden requerir keyset. Evitar resolver cada ProductId individualmente o esperar que NoTracking arregle un mal índice.

**Interview question:** How would you diagnose an N+1 problem?

**Natural answer:** I would inspect database command logs or tracing for repeated queries whose count grows with the returned rows. The current repositories load a page of scalar entity data and do not use lazy loading. If history needs product or warehouse names later, I would project the required fields in one query instead of calling a repository inside a loop. I would measure the resulting SQL as well, since an oversized join can introduce a different performance problem.

## 24. ACID, transacciones e isolation levels

**Explicación:** Atomicidad agrupa cambios, consistencia conserva reglas, aislamiento define intercalaciones y durabilidad conserva commits. ReadCommitted evita lecturas sucias, pero no Lost Update por sí solo.

**Archivo y método:** StockMovementRepository.RegisterAsync: BeginTransactionAsync(ReadCommitted), SaveChangesAsync, CommitAsync.

**Funcionamiento interno:** Locks de escritura duran hasta terminar. Con RCSI las lecturas pueden usar versiones; rowversion sigue siendo necesario para detectar escritura obsoleta.

**Cuándo usarlo, trade-offs y errores comunes:** Transacciones cortas para saldo/historial; aislamiento mayor cuando decisiones sobre rangos lo requieran. Más aislamiento aumenta bloqueo/deadlocks. Evitar llamadas externas dentro de transacciones.

**Interview question:** Why use rowversion if you already have a transaction?

**Natural answer:** The transaction ensures that stock and history commit or roll back together. ReadCommitted does not prevent two transactions from reading the same quantity before either writes. Rowversion adds a stale-write check, so one cannot silently overwrite the other's result. Stronger isolation or explicit locks could protect that decision differently, but would hold more locks. This design keeps transactions short and communicates conflicts explicitly rather than assuming atomic commit alone solves concurrent read-modify-write operations.

## 25. Rowversion, Lost Update, optimistic y pessimistic concurrency

**Explicación:** Lost Update sobrescribe un cambio usando estado viejo. Rowversion detecta ese estado; locking pesimista impide intercalaciones antes de decidir.

**Archivo y método:** StockConfiguration.Configure: IsRowVersion; PersistenceErrors.Translate; StockMovementRepository.RegisterAsync.

**Funcionamiento interno:** SQL genera token binario, no fecha. EF compara token original en WHERE; cero filas actualizadas dispara DbUpdateConcurrencyException y rollback del movimiento.

**Cuándo usarlo, trade-offs y errores comunes:** Optimista con contención moderada; UPDATE condicional para alta contención; locks/range locks para serializar. Puede rechazar operaciones que funcionarían tras releer. Evitar retries automáticos sin idempotencia y token para filas inexistentes.

**Interview question:** Explain two withdrawals of the final unit.

**Natural answer:** Both requests may read one unit with the same rowversion. The first update changes the quantity and token. The second update no longer matches its original version, so EF raises a concurrency exception and its transaction rolls back the movement. If it reads after the first commit, it sees zero and fails for insufficient stock instead. Either path returns one success and one conflict, leaves zero stock, and persists exactly one exit.

## 26. SQL injection y consultas parametrizadas

**Explicación:** Parametrización separa SQL y datos. Las consultas LINQ de EF no concatenan SKU ni identificadores en texto ejecutable.

**Archivo y método:** ProductRepository.SkuExistsAsync; StockRepository.GetByProductAndWarehouseAsync.

**Funcionamiento interno:** SqlClient transmite parámetros como valores. Identificadores/columnas dinámicas requieren allowlist, porque no son valores parametrizables.

**Cuándo usarlo, trade-offs y errores comunes:** LINQ o parámetros explícitos en SQL raw. No sustituye autorización ni límites de consultas. Evitar FromSqlRaw con concatenación y credenciales con permisos excesivos.

**Interview question:** Does using an ORM eliminate SQL injection?

**Natural answer:** Not automatically. The LINQ queries here are parameterized, so user input is supplied as values instead of executable SQL. Raw SQL APIs can still be misused, and dynamic identifiers such as table names require an allowlist rather than ordinary value parameters. I keep the query surface narrow and would review any raw SQL addition. Parameterization also does not replace resource authorization, least-privilege credentials, or protection against expensive but syntactically valid queries.

## 27. Docker

**Explicación:** Contenedores empaquetan runtime y aplicación; Compose coordina SQL local. El volumen conserva datos al reemplazar contenedores.

**Archivo y método:** Dockerfile; compose.yaml; .dockerignore; .env.example.

**Funcionamiento interno:** Multi-stage usa SDK para compilar y runtime con usuario app para ejecutar. Migraciones son un servicio explícito, separado del arranque API.

**Cuándo usarlo, trade-offs y errores comunes:** Útil para reproducibilidad local y artefactos. Mantener imágenes actualizadas; no prueba lógica ni seguridad. Evitar copiar secretos o publicar SQL en todas las interfaces. Solo se validó sintaxis aquí.

**Interview question:** What does Docker improve, and what does it not guarantee?

**Natural answer:** Docker provides a repeatable application environment and separates the build stage from the smaller runtime image. Compose describes local SQL, a persistent volume, health checks, and loopback ports. It does not prove transactional correctness or make the API production-ready. In this session the daemon was unavailable, so containers were not executed. Functional verification used an installed SQL Server instance. I distinguish that evidence from the separate claim that the Compose configuration passed syntax validation.

## 28. Git Flow y CI/CD

**Explicación:** Feature contiene trabajo; develop integra; master permanece estable. CI verifica; CD entrega/despliega y no está configurado.

**Archivo y método:** Rama feature/application-layer; .github/workflows/backend-ci.yml.

**Funcionamiento interno:** Actions restaura, compila Release, ejecuta tests y publica TRX. No aplica migraciones ni despliega.

**Cuándo usarlo, trade-offs y errores comunes:** Usar PR revisada hacia develop. CI rápido no valida SQL real. Evitar push/merge sin autorización o afirmar ejecución remota por resultados locales.

**Interview question:** What does your CI pipeline cover?

**Natural answer:** The workflow restores the solution, builds Release, runs the existing tests, and uploads test result files. It does not deploy or validate SQL Server concurrency. The database checks are a separate reproducible HTTP script against a real SQL instance. The workflow is prepared, but no remote execution is claimed because no push was authorized. A future pipeline could add an isolated SQL integration job without putting production secrets or deployments into the basic build job.

## 29. Azure Container Apps, Azure SQL y Application Insights

**Explicación:** Container Apps ejecuta imagen; Azure SQL persiste; Application Insights observa una aplicación instrumentada. Solo preparación.

**Archivo y método:** docs/AZURE_PREPARATION.md; Program y Dockerfile como puntos de partida.

**Funcionamiento interno:** Managed identity puede obtener token; SQL necesita usuario y permisos. Telemetría requiere instrumentación, no solo variable de entorno.

**Cuándo usarlo, trade-offs y errores comunes:** Solo después de seguridad y aprobación de costo. Servicios administrados reducen operación pero añaden límites/costos. Evitar prometer costo cero por cuotas gratuitas o decir que ILogger ya envía a Insights.

**Interview question:** How would you prepare the application for Azure?

**Natural answer:** First I would implement authentication and resource-level authorization because the MVP deliberately refuses non-development startup. Then I would use an immutable image, managed identity with least-privilege SQL access, and a separate migration identity. Telemetry instrumentation and health probes would support operations. No Azure resources were created in this task. I would review compute, database, networking, and logging costs explicitly rather than assume that free allowances guarantee a zero bill.

## 30. Logs y troubleshooting

**Explicación:** traceId correlaciona fallos; readiness prueba conectividad SQL. Verificar inventario exige respuestas y estado persistido.

**Archivo y método:** ApiExceptionHandler.TryHandleAsync; DatabaseHealthCheck.CheckHealthAsync; scripts/verify-api.ps1; docs/VERIFICATION.md.

**Funcionamiento interno:** El script inicia HTTP concurrente y consulta saldo/historial. Una consulta SQL adicional compara Quantity contra suma firmada de movimientos.

**Cuándo usarlo, trade-offs y errores comunes:** Investigar traceId, estado del commit y consultas. Los logs internos requieren protección. Tres rondas no son carga sostenida ni fuerzan ambas lecturas antes del UPDATE. Evitar registrar secretos o exagerar evidencia.

**Interview question:** How would you troubleshoot an inconsistent inventory report?

**Natural answer:** I would correlate the request trace identifier with logs, determine whether the transaction committed, and compare stock with the signed sum of its movements. I would inspect concurrency and integrity failures without exposing SQL details to clients. The local check found no mismatches, but it is not a load test. I would also look for writers bypassing the transaction contract and ambiguous client retries, because the MVP has no idempotency keys.

## Microejercicios de patrones — separados del runtime

Estos fragmentos explican patrones; no agregan funcionalidades al MVP.

### Strategy

```csharp
interface IStockPolicy { bool CanWithdraw(int available, int requested); }
sealed class NoNegativePolicy : IStockPolicy
{
    public bool CanWithdraw(int available, int requested) =>
        requested > 0 && available >= requested;
}
```

El consumidor recibiría IStockPolicy por constructor. No se necesita en Stock porque hoy existe una sola regla estable. Tampoco resolvería concurrencia entre procesos.

### Factory Method

```csharp
abstract class PolicyCreator
{
    public abstract IStockPolicy CreatePolicy();
}
sealed class StandardPolicyCreator : PolicyCreator
{
    public override IStockPolicy CreatePolicy() => new NoNegativePolicy();
}
```

La creación es polimórfica. Un método estático Create o un registro DI no son por sí solos el patrón GoF Factory Method.

### Builder

```csharp
sealed class ProductBuilder
{
    private string sku = "DEMO-001";
    private string name = "Demo";
    public ProductBuilder WithSku(string value) { sku = value; return this; }
    public ProductBuilder WithName(string value) { name = value; return this; }
    public Product Build() => new(sku, name);
}
```

Usaría Inventory.Domain.Entities.Product. Útil para fixtures con muchos campos; el constructor del dominio conserva validación. No compartir un Builder mutable entre pruebas paralelas.

### Singleton

```csharp
sealed class ExampleCatalog
{
    private static readonly Lazy<ExampleCatalog> instance = new(() => new());
    public static ExampleCatalog Instance => instance.Value;
    private ExampleCatalog() { }
}
```

Lazy<T> publica la instancia de forma segura por defecto; no vuelve seguro su futuro estado mutable. Para servicios se prefiere DI que permita sustitución. Nunca convertir DbContext en un Singleton.

## Cinco preguntas difíciles sobre esta implementación

1. **How would you make retries safe after a timeout during commit?** Idempotency key única, huella del payload y resultado guardados en la misma transacción; detectar reutilización con payload distinto. No está implementado.
2. **What happens when two requests initialize a missing Stock?** La PK compuesta arbitra; una puede devolver 409. El token solo existe para filas persistidas.
3. **Would ReadCommitted alone prevent a lost update?** No: explicar read/read/write/write y comparar rowversion, UPDATE condicional y locks.
4. **How do you prove the movement insert rolls back after a stale update?** Comparten transacción; HTTP observa saldo e historial correctos. La integración adicional sincroniza los dos comandos antes de guardar, después de ambas lecturas y operaciones de dominio.
5. **How would you scale a frequently contested stock pair?** Medir conflictos, revisar UPDATE condicional y considerar retries solo con idempotencia. No introducir caches de saldo sin protocolo de consistencia.

## Referencias oficiales

- [EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [EF Core tracking](https://learn.microsoft.com/en-us/ef/core/querying/tracking)
- [ASP.NET Core DI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0)
- [ASP.NET Core errores](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0)
