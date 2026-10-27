# Preparación Azure: solo instrucciones

No se han creado recursos, imágenes remotas ni despliegues. El presupuesto autorizado es **$0 MXN**. Esta guía no supone que un servicio o una cuota gratuita garanticen costo cero. Cualquier aprovisionamiento requiere aprobación expresa y una revisión del costo total.

## Antes de desplegar

El código actual rechaza ambientes distintos de Development porque no implementa autenticación ni autorización. No debe habilitarse Production quitando solo esa protección. Primero hay que añadir autenticación con Microsoft Entra ID/OIDC, políticas de autorización por almacén, TLS, límites de tráfico y gestión de secretos; después se puede cambiar ese guard con una revisión de seguridad.

## Diseño propuesto

- Azure Container Apps para ejecutar la imagen ASP.NET Core, puerto interno 8080.
- Azure SQL para los datos; no se despliega el contenedor Developer de Compose en producción.
- Una identidad administrada para acceder a SQL y, si corresponde, al registro de imágenes.
- Application Insights/Azure Monitor para traces, métricas y logs.
- Credencial/identidad de migraciones separada de la identidad runtime; la aplicación no necesita CREATE/ALTER permanentes.

La identidad administrada permite obtener tokens para servicios compatibles con Entra, incluido Azure SQL. Es necesario además crear el usuario correspondiente en SQL y asignar permisos mínimos; asignar identidad al contenedor no otorga por sí solo acceso a la base. [Documentación oficial](https://learn.microsoft.com/en-us/azure/container-apps/managed-identity).

## Secuencia de un futuro despliegue autorizado

1. Revisar región, costos de cómputo, SQL, almacenamiento/registro, red e ingestión/retención de logs. Acordar límites y alertas; no tratar una alerta de presupuesto como un bloqueo de facturación.
2. Implementar y verificar los controles de acceso pendientes. Elegir conectividad SQL y evitar exposición pública innecesaria.
3. Compilar, probar y publicar una imagen inmutable etiquetada por commit, preferentemente fijando digest de las imágenes base.
4. Crear o seleccionar los recursos solo tras aprobación. Configurar identidad administrada y permisos SQL.
5. Configurar ConnectionStrings__Inventory externamente, por ejemplo:
   `Server=tcp:<servidor>.database.windows.net,1433;Database=<base>;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False`.
   Para identidad asignada por usuario, comprobar la sintaxis de SqlClient y el client ID que corresponda a esa identidad.
6. Revisar y ejecutar el script idempotente de migración con la identidad de migración. Respaldar datos antes de cambios incompatibles.
7. Configurar Container Apps con ambiente Production, puerto 8080 y AllowedHosts para su hostname. Esto requiere primero los cambios de seguridad del punto 2.
8. Configurar liveness /health y readiness /health/ready. Habilitar HTTPS y proteger Swagger o limitarlo al ambiente apropiado.
9. Instrumentar OpenTelemetry/Azure Monitor antes de configurar Application Insights. **El proyecto actual solo implementa ILogger; establecer APPLICATIONINSIGHTS_CONNECTION_STRING por sí solo no añade instrumentación.**
10. Ejecutar las verificaciones HTTP y de concurrencia en el ambiente destino, revisar traceId y dependencias SQL, y definir rollback de imagen compatible con el esquema.

No se incluye un workflow CD, comandos de aprovisionamiento automático ni SDK de telemetría que no se esté utilizando. El CI preparado únicamente compila y prueba.

Referencia para instrumentación futura: [Azure Monitor OpenTelemetry](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable).
