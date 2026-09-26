# Plataforma de Créditos - ASP.NET Core MVC

## Deploy en Render

### Configuración de Variables de Entorno

En Render, configura las siguientes variables de entorno en tu Web Service:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ConnectionStrings__DefaultConnection=DataSource=app.db;Cache=Shared
```

### Documentación Importante

**Persistence de SQLite:** 
- SQLite se guarda en el archivo `app.db` en el directorio raíz
- Render reinicia los servicios con frecuencia, por lo que los datos se perderán al reiniciar
- Para persistir datos, se recomienda usar el disco persistente de Render o migrar a PostgreSQL

### Pasos para desplegar

1. Conecta tu repositorio de GitHub a Render
2. Crea un nuevo Web Service
3. Configura las variables de entorno mencionadas arriba
4. Build Command: `dotnet build`
5. Start Command: `dotnet PlataformaCreditos.dll`
6. ¡Listo!

### Funcionalidades Implementadas

- **Q1**: Modelos Bootstrap + Dominio (Cliente, SolicitudCredito)
- **Q2**: Catálogo de solicitudes con filtros
- **Q3**: Registro de solicitudes con validaciones server-side
- **Q4**: Sesiones y cache con Redis (nota: en Render usar Redis add-on o caché en memoria)
- **Q5**: Panel de Analista con aprobar/rechazar
- **Q6**: Notificaciones WebSocket en tiempo real
- **Q7**: *(Nota: RabbitMQ fue omitido por problemas de compatibilidad de API)*
- **Q8**: *(Este deploy)*

### Credenciales de Prueba

- **Usuario Analista**: Se crea automáticamente en el seed
- **Usuario Cliente**: Se crea automáticamente en el seed
- **Contraseña**: `Password123!` (usada en el seed de datos)

### Notas Adicionales

- El proyecto usa Identity con roles Individual Account
- Las solicitudes se guardan en SQLite (archivo `app.db`)
- Las notificaciones WebSocket funcionan en tiempo real
- El panel del analista requiere inicio de sesión con rol "Analista"