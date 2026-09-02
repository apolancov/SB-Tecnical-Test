# SB

Aplicación full-stack de referencia: API REST en **ASP.NET Core 8** +
cliente web en **Next.js 14**, respaldada por **SQL Server 2022** y
protegida con **JWT** y autorización basada en roles.

El repositorio incluye dos flujos de trabajo soportados que comparten
el mismo código fuente y los mismos archivos de configuración:

- **Desarrollo local** — backend, frontend y base de datos corren en
  la máquina del desarrollador sin necesidad de contenedores.
- **Docker / Podman** — la misma aplicación corre dentro de un stack
  de `compose`.

Cambiar de flujo **no** requiere editar el código fuente,
`Program.cs`, `appsettings.*`, `Dockerfile.*` ni `compose.yaml`; solo
cambian los valores de configuración (variables de entorno,
`.env.development` vs `.env.production`).

> Documentación relacionada:
> - Decisiones arquitectónicas: [`src/docs/arquitectura.md`](src/docs/arquitectura.md)
> - Evidencias de verificación: [`src/docs/evidencias.md`](src/docs/evidencias.md)

---

## Tabla de contenidos

1. [Componentes y puertos](#componentes-y-puertos)
2. [Requisitos previos](#requisitos-previos)
3. [Levantar el proyecto en local](#levantar-el-proyecto-en-local)
   - [Opción A — `podman compose` / `docker compose` (todo en contenedores)](#opción-a--podman-compose--docker-compose-todo-en-contenedores)
   - [Opción B — Sin contenedores (servicios nativos)](#opción-b--sin-contenedores-servicios-nativos)
   - [Opción C — Híbrido: SQL Server en Docker, API y frontend en el host](#opción-c--híbrido-sql-server-en-docker-api-y-frontend-en-el-host)
4. [Cómo funciona `sqlserver-init`](#cómo-funciona-sqlserver-init)
5. [Endpoints disponibles](#endpoints-disponibles)
6. [Usuarios de prueba y roles](#usuarios-de-prueba-y-roles)
7. [Notificaciones](#notificaciones)
8. [Configuración](#configuración)
9. [Pruebas](#pruebas)
10. [Comprobaciones rápidas (smoke tests)](#comprobaciones-rápidas-smoke-tests)
11. [Red: host vs contenedor](#red-host-vs-contenedor)
12. [Solución de problemas](#solución-de-problemas)

---

## Componentes y puertos

| Componente      | Stack                                                | URL local (sin Docker)                  | URL con Docker / Podman               |
|-----------------|------------------------------------------------------|----------------------------------------|---------------------------------------|
| Frontend        | Next.js 14 (App Router, `output: 'export'`), React 18, TS | <http://localhost:3000>           | <http://localhost:3000>               |
| Backend (API)   | .NET 8, ASP.NET Core, EF Core 8, JWT (HMAC-SHA256)  | <http://localhost:5180> (Swagger: `/swagger`)<br><https://localhost:7088> (perfil https) | <http://localhost:8080> (Swagger: `/swagger`) |
| SQL Server      | SQL Server 2022 (imagen MCR, contenedor)             | `localhost:1433` (instalación nativa) | `localhost:1433`                      |

Resumen rápido:

```text
flujo local sin Docker              flujo con compose (docker / podman)

[ Browser ]                         [ Browser ]
     | 3000 (frontend)                   | 3000
     v                                   v
[ Next.js ] ──── 5180 ────►         [ frontend container ]
     |                            ────┐
     | HTTP /api/...               8080│ (publish)
     |                                  v
[ ASP.NET Core API ]              [ backend container ]
     | 1433                             | sqlserver,1433
     v                                  v
[ SQL Server nativo ]              [ sqlserver container ]
```

Nota importante: el navegador **siempre** apunta a
`http://localhost:<puerto-publicado>` del host; los nombres
`backend`, `sqlserver`, etc. solo se usan para tráfico
**contenedor-a-contenedor**.

---

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 22+](https://nodejs.org/) y [pnpm 9+](https://pnpm.io/)
- Para el flujo sin contenedores: una instancia local de **SQL Server 2022**
  con las credenciales indicadas más abajo
  (la opción B funciona sin Docker si ya la tienes instalada).
- Para el flujo en contenedores: **Docker Compose v2** o **Podman Compose**.
- `sqlcmd` **no** es necesario en el host cuando SQL Server corre en
  Docker: el script auxiliar `scripts/start-local.sh` lo lanza con
  `docker exec`/`podman exec` desde el binario que ya trae la imagen
  oficial de MSSQL. Instala `mssql-tools` solo si prefieres saltarte
  Docker y hablar con un SQL Server nativo.

### Variables / secretos a conocer

| Variable                  | Origen                  | Para qué sirve                                                          |
|---------------------------|-------------------------|-------------------------------------------------------------------------|
| `SA_PASSWORD`             | `.env` (raíz)           | Contraseña del usuario `sa` de SQL Server (debe cumplir la política MSSQL). |
| `NEXT_PUBLIC_API_URL`     | `.env` (raíz) y `.env.production`/`development` del frontend | URL base que el navegador usa para llamar a la API.                    |
| `JWT_SECRET_KEY`          | `.env` (raíz)           | Llave HMAC para firmar JWT (>= 32 caracteres).                          |
| `JWT_ISSUER`              | `.env` (raíz)           | `iss` de los tokens emitidos.                                            |
| `JWT_AUDIENCE`            | `.env` (raíz)           | `aud` esperado al validar.                                               |
| `JWT_EXPIRATION_MINUTES`  | `.env` (raíz)           | Vigencia del `accessToken` (por defecto `60`).                          |

> El arranque valida que `Jwt`, `Cors` y `ConnectionStrings` estén
> presentes y sean correctos. Si falta algún valor crítico el proceso
> falla con `InvalidOperationException` antes de aceptar conexiones.

---

## Levantar el proyecto en local

Elige **una** de las tres opciones. Todas terminan con el stack
funcionando: podrás abrir el frontend en
<http://localhost:3000> y la API en <http://localhost:8080>
(con Docker) o <http://localhost:5180> (sin Docker).

### Opción A — `podman compose` / `docker compose` (todo en contenedores)

Esta es la forma **recomendada** porque reproduce el comportamiento de
producción y arranca en pocos minutos con un solo comando.

```bash
# 0. Una sola vez: crea tu .env a partir del template versionado.
cp .env.example .env
# (edita .env si quieres cambiar SA_PASSWORD, JWT_SECRET_KEY, etc.)

# 1. Levanta el stack completo (construye imágenes si hace falta).
podman compose up --build      # o: docker compose up --build
```

Servicios publicados al host:

| Servicio        | URL                                                     | Notas                              |
|-----------------|---------------------------------------------------------|------------------------------------|
| frontend        | <http://localhost:3000>                                 | Servidor estático Node.js sobre el export de Next.js |
| backend (API)   | <http://localhost:8080>                                 | `ASPNETCORE_URLS=http://+:8080`    |
| Swagger         | <http://localhost:8080/swagger>                          | UI de OpenAPI                      |
| SQL Server      | `localhost:1433` (user `sa`, password de `.env`)        | Volumen persistente `sqlserver-data` |

Logs:

```bash
podman compose logs -f backend
podman compose logs -f database-init   # salida del schema + seed
podman compose logs -f sqlserver
```

Detener y limpiar:

```bash
podman compose down                 # detiene contenedores, conserva volúmenes
podman compose down -v              # también borra el volumen de SQL Server
                                    #   (¡resetea los datos!)
```

La primera vez tarda varios minutos (descarga imágenes, restauración
de NuGet, `dotnet restore`, `pnpm install`, etc.); las siguientes son
casi instantáneas salvo que vuelvas a pasar `--build`.

### Opción B — Sin contenedores (servicios nativos)

Útil si prefieres ciclos rápidos de recarga con `dotnet run` y
`next dev`, y ya tienes SQL Server instalado.

```bash
# 0. SQL Server debe estar corriendo en localhost:1433 con
#    sa / YourStrong!Passw0rd (o el valor que pongas en .env).
#    Si no lo tienes nativo, puedes levantarlo solo en Docker con
#    `./scripts/start-sqlserver.sh` y seguir igual.

# 1. Inicializa el esquema y carga datos de prueba.
./scripts/start-local.sh --only-db

# 2. Backend (hot-reload).
dotnet run --project src/backend/Api
# Expone: http://localhost:5180 (perfil http) o https://localhost:7088 (perfil https)

# 3. Frontend (hot-reload), en otra terminal.
cd src/frontend
pnpm install --frozen-lockfile
pnpm dev
# Expone: http://localhost:3000
```

Atajos del script (`scripts/start-local.sh`):

```bash
./scripts/start-local.sh                    # stack completo (DB + API + frontend)
./scripts/start-local.sh --skip-db          # no tocar la base de datos
./scripts/start-local.sh --only-db          # solo aplicar esquema + seed
./scripts/start-local.sh --only-backend     # solo la API
./scripts/start-local.sh --only-frontend    # solo el cliente web
./scripts/start-local.sh --auto-start-sqlserver
                                           # si SQL Server no responde en 1433, lo
                                           # levanta desde compose.yaml automáticamente
```

`scripts/start-local.sh` detecta automáticamente cómo hablar con
SQL Server, en este orden:

1. Un binario `sqlcmd` en el `PATH` (host).
2. Un contenedor `project-sqlserver` ya corriendo (`docker exec` / `podman exec`).
3. `--auto-start-sqlserver` lo arranca con `scripts/start-sqlserver.sh`.

Pulsa `Ctrl+C` una vez para detener ambos procesos limpiamente.

### Opción C — Híbrido: SQL Server en Docker, API y frontend en el host

SQL Server es pesado de instalar; es muy común mantenerlo en contenedor
mientras la API y el frontend corren nativos para iterar rápido.

```bash
# 0. Una sola vez: arranca SQL Server con el script auxiliar.
./scripts/start-sqlserver.sh

# 1. Como en la opción B pero sin el flag --auto-start-sqlserver:
./scripts/start-local.sh
```

El script `scripts/start-sqlserver.sh` es idempotente: si el
contenedor ya está corriendo, simplemente lo reporta y sale con
código 0.

---

## Cómo funciona `sqlserver-init`

`sqlserver-init` es un servicio de **un solo uso** declarado en
`compose.yaml` que aplica `schema.sql` y `seed.sql` después de que
SQL Server está sano. Su imagen se construye con
`Dockerfile.database-init`.

```text
docker compose up
   │
   ├─► sqlserver   (healthcheck: "sqlcmd -Q SELECT 1" cada 10s)
   │      estado: starting → unhealthy → healthy
   │
   └─► database-init   (depends_on: sqlserver == service_healthy)
         └─ ejecuta ENTRYPOINT [./init.sh]:
              1. Bucle de espera hasta que SELECT 1 funcione.
              2. CREATE DATABASE IF NOT EXISTS ApplicationDb.
              3. Aplica schema.sql  (idempotente — solo crea lo que falte).
              4. Aplica seed.sql    (idempotente — usa IF NOT EXISTS / NEWID()).
              5. Sale con código 0.
                   │
                   └─► backend   (depends_on: database_init == service_completed_successfully)
                                  └─► frontend  (depends_on: backend == service_started)
```

Puntos importantes:

- El servicio **no persiste volumen propio**: tras aplicar
  `schema.sql` y `seed.sql` el contenedor termina (es esperado).
  Si lo levantas con `compose up` y quieres reejecutarlo, hazlo con
  `compose up --force-recreate database-init` después de editar los
  scripts.
- Como tanto `schema.sql` como `seed.sql` son idempotentes, puedes
  reiniciar el servicio sin corromper la base: las tablas e inserts
  usan `IF OBJECT_ID ... IS NULL` y `IF NOT EXISTS (...)`.
- La imagen base es `mcr.microsoft.com/mssql/server:2022-latest` (no
  `mcr.microsoft.com/mssql-tools18`, que no existe como tag publicable),
  y reusa el binario `/opt/mssql-tools18/bin/sqlcmd` que esa misma
  imagen ya trae.
- Scripts disponibles para reproducir la inicialización fuera del
  compose:

  ```bash
  # Aplicar a un SQL Server local o en contenedor desde la raíz:
  ./scripts/start-local.sh --only-db

  # Equivalente manual (asume SQL Server corriendo):
  docker exec -i project-sqlserver \
      /opt/mssql-tools18/bin/sqlcmd -S localhost,1433 -U sa \
      -P 'YourStrong!Passw0rd' -C \
      -Q "IF DB_ID(N'ApplicationDb') IS NULL CREATE DATABASE [ApplicationDb];"

  docker exec -i project-sqlserver \
      /opt/mssql-tools18/bin/sqlcmd -S localhost,1433 -U sa \
      -P 'YourStrong!Passw0rd' -C -d ApplicationDb < src/database/schema.sql

  docker exec -i project-sqlserver \
      /opt/mssql-tools18/bin/sqlcmd -S localhost,1433 -U sa \
      -P 'YourStrong!Passw0rd' -C -d ApplicationDb < src/database/seed.sql
  ```

- Para resetear la base completamente:

  ```bash
  podman compose down -v                # borra el volumen sqlserver-data
  podman compose up --build             # reaplica esquema y seed desde cero
  ```

---

## Endpoints disponibles

La API expone los siguientes recursos. Todos los endpoints devuelven
JSON, aceptan y producen `application/json`. Los tiempos se expresan
en UTC.

### Autenticación — `api/auth`

| Verbo | Ruta                  | Auth                               | Descripción                                                  |
|-------|-----------------------|------------------------------------|--------------------------------------------------------------|
| POST  | `/api/auth/login`     | Ninguna                            | Autentica con `{username, password}` y devuelve `accessToken` (JWT) + datos del usuario. |
| GET   | `/api/auth/me`        | Cualquier usuario autenticado      | Devuelve el usuario del JWT vigente.                         |
| GET   | `/api/auth/admin/health` | Solo rol `Admin` (`AdminOnly`) | Healthcheck protegido por la política `AdminOnly`.           |

Ejemplo de login:

```bash
curl -s -X POST http://localhost:8080/api/auth/login \
     -H 'Content-Type: application/json' \
     -d '{"username":"admin","password":"AdminPass123!"}'
```

Todas las llamadas autenticadas requieren el header
`Authorization: Bearer <accessToken>`.

### Instituciones — `api/institutions`

| Verbo | Ruta                              | Auth              | Descripción                                                                  |
|-------|-----------------------------------|-------------------|------------------------------------------------------------------------------|
| GET   | `/api/institutions`               | Pública           | Lista paginada con filtros (`page`, `pageSize`, `name`, `category`, `statePower`, `sector`). |
| GET   | `/api/institutions/filter-options`| Pública           | Catálogo de valores admitidos en los filtros (`category`, `statePower`, `sector`). |
| GET   | `/api/institutions/{id}`          | Auth             | Detalle de una institución.                                                  |
| POST  | `/api/institutions`               | Auth             | Crea una institución. Devuelve `201 Created` + `Location`.                   |
| PUT   | `/api/institutions/{id}`          | Auth             | Reemplaza los campos editables.                                              |
| DELETE| `/api/institutions/{id}`          | Auth             | Borrado lógico / físico. Devuelve `204 No Content`.                          |

### Solicitudes — `api/solicitudes`

| Verbo | Ruta                                            | Auth       | Descripción                                              |
|-------|-------------------------------------------------|------------|----------------------------------------------------------|
| GET   | `/api/solicitudes`                              | Auth       | Lista paginada con filtros: `status`, `priority`, `areaId`, `requestTypeId`, `requesterId`, `responsibleId`, `fromDate`, `toDate`, `code`, `search`, `sortBy`, `sortDirection`. |
| GET   | `/api/solicitudes/{id}`                         | Auth       | Detalle (incluye comentarios, historial y transiciones).  |
| POST  | `/api/solicitudes`                              | Auth       | Crea una solicitud.                                      |
| PATCH | `/api/solicitudes/{id}`                         | Auth       | Actualiza título, descripción, prioridad y evidencia.    |
| PATCH | `/api/solicitudes/{id}/estado`                  | Auth       | Cambia el estado (`NewStatus` + comentario opcional).    |
| PATCH | `/api/solicitudes/{id}/asignacion`              | Auth       | Asigna o reasigna un responsable.                        |
| POST  | `/api/solicitudes/{id}/reapertura`              | Auth       | Reabre una solicitud cerrada/en resolución.              |
| POST  | `/api/solicitudes/{id}/comentarios`             | Auth       | Agrega un comentario (visibilidad `Requester` o `Internal`). |

> Los permisos finos (quién puede asignar, cambiar de estado, comentar
> con visibilidad interna, etc.) se aplican en los `Handler` de la
> capa `Application`. Devuelven `403 Forbidden` cuando el usuario no
> cumple la regla.

### Catálogos — `api/catalogos`

| Verbo | Ruta                          | Auth       | Descripción                                |
|-------|-------------------------------|------------|--------------------------------------------|
| GET   | `/api/catalogos/areas`        | Auth       | Áreas activas (`Atención al Ciudadano`, `Soporte Técnico`, ...). |
| GET   | `/api/catalogos/tipos-solicitud` | Auth    | Tipos de solicitud activos (`Incidente`, `Requerimiento`, ...). |
| GET   | `/api/catalogos/responsables` | Auth + rol | Candidatos para asignar (`Staff`).         |

### Dashboard — `api/dashboard`

| Verbo | Ruta                       | Auth | Descripción                                |
|-------|----------------------------|------|--------------------------------------------|
| GET   | `/api/dashboard/resumen`   | Auth | Resumen para la página principal (totales por estado, prioridades, etc.). |

### Swagger

| Verbo | Ruta                               | Auth | Descripción                |
|-------|------------------------------------|------|----------------------------|
| GET   | `/swagger`                         | Ninguna (solo Development) | UI de OpenAPI.             |
| GET   | `/swagger/v1/swagger.json`         | Ninguna (solo Development) | Definición OpenAPI en JSON. |

> En el contenedor (perfil `Production`), el middleware de Swagger está
> deshabilitado. Para explorarlo más cómodamente, levanta la API con
> `dotnet run` (perfil Development) o con
> `ASPNETCORE_ENVIRONMENT=Development`.

---

## Usuarios de prueba y roles

Los siguientes usuarios se crean automáticamente al aplicar
`src/database/seed.sql` (idempotente: solo se insertan si el username
no existe). **Son credenciales de desarrollo — no las reutilices en
ningún otro entorno.**

| Usuario       | Email                       | Contraseña           | Rol           | Activo | Uso recomendado                                 |
|---------------|-----------------------------|----------------------|---------------|--------|-------------------------------------------------|
| `admin`       | `admin@example.local`       | `AdminPass123!`      | `Admin`       | Sí     | Acceso completo (incluye `GET /api/auth/admin/health`). |
| `user`        | `user@example.local`        | `UserPass123!`       | `User`        | Sí     | Smoke test genérico de endpoints autenticados.  |
| `analista`    | `analista@example.local`    | `AnalistaPass123!`   | `Analista`    | Sí     | Asignar, comentar internamente, cambiar estado. |
| `solicitante1`| `solicitante1@example.local`| `SolicitantePass123!`| `Solicitante` | Sí     | Crear solicitudes, comentar con visibilidad pública. |
| `solicitante2`| `solicitante2@example.local`| `SolicitantePass123!`| `Solicitante` | Sí     | Como `solicitante1` (útil para pruebas cruzadas). |

Roles definidos en `src/backend/Domain/Enums/UserRole.cs`:

```csharp
public enum UserRole { Admin, User, Analista, Solicitante }
```

Política de autorización registrada (`Program.cs`):

- `AdminOnly` → exige `RequireAuthenticatedUser().RequireRole("Admin")`.
  Aplicada en `GET /api/auth/admin/health` y, a nivel de handler,
  sobre operaciones que solo un `Admin` puede ejecutar (gestión de
  catálogos sensibles, etc.).

Los correos usan el TLD `.local` (reservado por RFC 6762 para
desarrollo) y nunca resuelven en DNS público.

### Datos sembrados

| Tabla           | Filas iniciales (orientativo) | Comentario                                      |
|-----------------|-------------------------------|-------------------------------------------------|
| `Sectors`       | unas pocas                    | Sectores institucionales.                       |
| `Categories`    | unas pocas                    | Categorías de institución.                      |
| `StatePowers`   | unas pocas                    | Poderes del Estado.                             |
| `Institutions`  | varias decenas                | Cargadas desde `resources/ListaEntidadesGubernamentales.xlsx` (codificadas en `seed.sql`). |
| `Users`         | 5                             | Los listados arriba.                            |
| `Areas`         | 6                             | `Atención al Ciudadano`, `Soporte Técnico`, `Mantenimiento`, `Seguridad`, `Administración`, `Logística`. |
| `RequestTypes`  | 6                             | `Incidente`, `Requerimiento`, `Consulta`, `Reclamo`, `Mantenimiento Preventivo`, `Cambio`. |
| `Requests`      | 5                             | `REQ-2026-0001`..`REQ-2026-0005`, uno por cada estado clave del flujo (Submitted, InReview, InProgress, OnHold, Closed). |

---

## Notificaciones

> **Estado actual: la aplicación persiste notificaciones, no las entrega.**

El backend ya genera una fila en la tabla `dbo.RequestNotifications`
cada vez que un caso de uso lo solicita:

- `CreateRequestHandler` → "Solicitud registrada".
- `AssignRequestHandler` → "Solicitud asignada" / "Solicitud reasignada".
- `ChangeRequestStatusHandler` → "Estado de la solicitud actualizado" /
  "Solicitud cerrada".
- `ReopenRequestHandler` → "Solicitud reabierta".

El contrato (`Application/Requests/Notifications/INotificationSender.cs`)
es una única operación asíncrona:

```csharp
public interface INotificationSender
{
    Task SendAsync(RequestNotification notification, CancellationToken ct = default);
}
```

La implementación por defecto
(`Infrastructure/Requests/Notifications/DatabaseNotificationSender.cs`)
hace `INSERT` en `dbo.RequestNotifications`, marca `Status = Sent` y
loguea el resultado. Los campos persistidos son: `Id`, `RequestId`,
`DestinationUserId`, `Channel` (enum: `Email`, `Sms`, `Push`, `InApp`),
`Status` (enum: `Pending`, `Sent`, `Failed`), `Subject`, `Message`,
`Date`.

Sin embargo:

- **No hay transporte**: no se envía correo, no se hace push, no hay
  SignalR ni SSE. La fila queda guardada pero el usuario no recibe
  nada en su navegador ni en su teléfono.
- **No hay UI**: el frontend no expone un buzón de notificaciones ni
  un contador. Solo `/api/solicitudes` y `/api/dashboard/resumen`
  consumen el resto del dominio.
- **Las consultas a `RequestNotifications` aún no están expuestas**:
  no existe `GET /api/notificaciones` ni equivalente; la tabla es
  únicamente log operativo hoy.

### Cómo implementar un sistema de notificaciones (guía paso a paso)

Las dos abrazaderas ya están pensadas para esto. La separación entre
`Application` (interfaz) e `Infrastructure` (implementación) hace que
agregar un canal real sea un cambio pequeño y testeable.

#### Opción 1 — Recomendada: chain-of-responsibility en `INotificationSender`

```csharp
// Application/Requests/Notifications/INotificationSender.cs — sin cambios.

// Infrastructure: composición de "senders" por canal.
public sealed class CompositeNotificationSender : INotificationSender
{
    private readonly IEnumerable<INotificationSender> _senders;
    private readonly ILogger<CompositeNotificationSender> _logger;

    public CompositeNotificationSender(
        IEnumerable<INotificationSender> senders,
        ILogger<CompositeNotificationSender> logger)
    {
        _senders = senders; _logger = logger;
    }

    public async Task SendAsync(RequestNotification n, CancellationToken ct)
    {
        foreach (var sender in _senders)
        {
            try { await sender.SendAsync(n, ct); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sender {Sender} failed for {Id}", sender, n.Id);
            }
        }
    }
}
```

```csharp
// Program.cs — reemplazar el registro simple por la composición.
builder.Services.AddScoped<INotificationSender>(sp =>
    new CompositeNotificationSender(new[]
    {
        sp.GetRequiredService<DatabaseNotificationSender>(),
        sp.GetRequiredService<EmailNotificationSender>(),     // nuevo
        sp.GetRequiredService<SignalRNotificationSender>(),   // opcional
    }));
```

#### Opción 2 — Canal `Email` (SMTP real)

1. Agregar `MailKit` o `System.Net.Mail.SmtpClient` en
   `Infrastructure`.
2. Crear `EmailNotificationSender : INotificationSender`:
   - Lee la plantilla según `notification.Subject`.
   - Resuelve el SMTP desde configuración (`Smtp:Host`,
     `Smtp:Port`, `Smtp:User`, `Smtp:Password`, `Smtp:From`).
   - En `SendAsync` envía el correo **sin persistir**; deja que la
     política de retries viva aquí dentro.
3. Registrar como segundo `INotificationSender` además de
   `DatabaseNotificationSender`.
4. Persistir el resultado en `RequestNotifications` con
   `Status = Sent` o `Status = Failed` para auditoría.

#### Opción 3 — Canal `InApp` con SignalR (tiempo real)

1. Agregar `Microsoft.AspNetCore.SignalR`.
2. Crear `Hubs/NotificationsHub.cs` y mapearlo
   (`app.MapHub<NotificationsHub>("/hubs/notifications")`).
3. `SignalRNotificationSender : INotificationSender` llama a
   `IHubContext<NotificationsHub>.Clients.User(destinationUserId).SendAsync(...)`.
4. Frontend: instalar `@microsoft/signalr`, crear
   `services/notificationsHub.ts`, suscribirse en el layout
   autenticado (`src/app/(protected)/layout.tsx`) y exponer un
   contador en `AppShell.tsx`.
5. Endpoint de listado (`GET /api/notificaciones?mine=true&status=Pending`)
   apoyándose en el `RequestReadRepository` ya existente.

#### Opción 4 — Server-Sent Events (más simple que SignalR)

1. Nuevo controller `NotificationsController` con
   `GET /api/notificaciones/stream` que devuelva
   `text/event-stream` y haga `await foreach` sobre un `Channel<...>`.
2. `InMemoryNotificationBus : INotificationBus` al que se suscribe el
   controller y al que escriben los handlers.
3. Frontend: `EventSource('/api/notificaciones/stream')` en el layout.

#### Opción 5 — Polling (mínimo viable)

1. Agregar un endpoint paginado:
   `GET /api/notificaciones?status=Pending&since=<iso>`.
2. Frontend: hook `useNotifications` que hace `fetch` cada 30s y
   actualiza un contador en `AppShell`.

#### Pruebas a añadir (lo que la falta actual ya permite testear)

- `SendAsync_PersistsRow_AndUpdatesStatus` (unit).
- Test funcional que cree una solicitud, capture la fila
  correspondiente en `RequestNotifications` y verifique
  `Status = Sent`.
- Para SignalR/SSE: test de integración con
  `Microsoft.AspNetCore.Mvc.Testing` + `WebApplicationFactoryClientOptions`
  configurando el `HttpMessageHandler` para hablar con el hub.

---

## Configuración

### Backend (`src/backend/Api`)

| Setting                                | Dónde                                                                                            |
|----------------------------------------|--------------------------------------------------------------------------------------------------|
| `ConnectionStrings:DefaultConnection`  | `appsettings.json` (dev) o env var `ConnectionStrings__DefaultConnection` (compose).             |
| `Cors:AllowedOrigins`                  | `appsettings.json` (`localhost:3000`, `localhost:5180`, `https://localhost:7088`).              |
| `Jwt:Issuer`, `Jwt:Audience`           | `appsettings.Development.json` (dev) / `Jwt__*` env vars (compose).                              |
| `Jwt:SecretKey`                        | `appsettings.Development.json` (placeholder dev), override con `Jwt__SecretKey` en producción.   |
| `Jwt:ExpirationMinutes`                | 60 por defecto.                                                                                   |

### Frontend (`src/frontend`)

| Archivo                       | Cargado por               | Default                                | Propósito                                      |
|-------------------------------|---------------------------|----------------------------------------|------------------------------------------------|
| `.env.development`            | `next dev`                | `NEXT_PUBLIC_API_URL=http://localhost:5180` | Backend corriendo con `dotnet run`.            |
| `.env.production`             | `next build`              | `NEXT_PUBLIC_API_URL=http://localhost:8080` | Backend en contenedor (`compose`).             |
| `.env.local`                  | override                  | (gitignored)                           | Overrides por desarrollador.                    |

El cliente HTTP centralizado (`src/frontend/src/services/api.ts`) lee
la URL base de `runtimeConfiguration.apiBaseUrl`.

### `.env` en la raíz del repo

Solo lo consume `compose.yaml`. Variables:

| Variable                  | Usado por                                       |
|---------------------------|-------------------------------------------------|
| `SA_PASSWORD`             | Contenedor SQL Server.                          |
| `NEXT_PUBLIC_API_URL`     | Build arg del frontend (se inlinea al bundle).   |
| `JWT_SECRET_KEY`          | `Jwt__SecretKey` del backend.                   |
| `JWT_ISSUER`              | `Jwt__Issuer`.                                  |
| `JWT_AUDIENCE`            | `Jwt__Audience`.                                |
| `JWT_EXPIRATION_MINUTES`  | `Jwt__ExpirationMinutes`.                       |

`.env` está en `.gitignore`. `.env.example` es la plantilla
versionada; cópiala una vez y ajústala.

---

## Pruebas

### Backend

```bash
dotnet test src/backend/Tests
```

- **Unitarias**: reglas de dominio, validadores, handlers.
- **Funcionales** (`Api/Functional/*`): usan
  `Microsoft.AspNetCore.Mvc.Testing` con **EF Core + SQLite
  in-memory**, no requieren SQL Server en ejecución.
- **De base de datos**: `Database/*` también usa SQLite para validar
  el modelo y los seeds.

### Frontend

```bash
cd src/frontend
pnpm install --frozen-lockfile
pnpm typecheck     # tsc --noEmit
pnpm lint          # eslint .
pnpm test          # vitest run (jsdom)
pnpm build         # genera out/ (output: 'export')
```

---

## Comprobaciones rápidas (smoke tests)

Asumiendo stack levantado por compose (cambia `:8080` por `:5180`
para entorno local).

```bash
# 1. OpenAPI disponible
curl -s http://localhost:8080/swagger/v1/swagger.json | head

# 2. Login (usuario admin)
curl -s -X POST http://localhost:8080/api/auth/login \
     -H 'Content-Type: application/json' \
     -d '{"username":"admin","password":"AdminPass123!"}'

# 3. Endpoint protegido con JWT
TOKEN=...
curl -s http://localhost:8080/api/institutions \
     -H "Authorization: Bearer $TOKEN"

# 4. Healthcheck exclusivo para Admin
curl -s -i http://localhost:8080/api/auth/admin/health \
     -H "Authorization: Bearer $TOKEN"

# 5. Catálogo público
curl -s http://localhost:8080/api/institutions/filter-options

# 6. Listado de solicitudes (paginado)
curl -s "http://localhost:8080/api/solicitudes?page=1&pageSize=10" \
     -H "Authorization: Bearer $TOKEN"
```

Desde el navegador:

1. Abre <http://localhost:3000>.
2. Inicia sesión con `admin` / `AdminPass123!`.
3. Recorre las rutas protegidas: `/dashboard`, `/institutions`,
   `/institutions/new`, `/requests`, `/requests/new`,
   `/requests/{id}`, `/areas`, `/request-types`, `/users`,
   `/directorio`.

---

## Red: host vs contenedor

Las dos "zonas" (host y red de compose) **no** deben confundirse.

| Conversación                                  | Endpoint                                                    |
|-----------------------------------------------|-------------------------------------------------------------|
| Navegador → Frontend (host)                   | `http://localhost:3000`                                     |
| Navegador → API (host nativo, `dotnet run`)   | `http://localhost:5180`                                     |
| Navegador → API (Docker/Podman)               | `http://localhost:8080`                                     |
| Backend contenedor → SQL Server               | `Server=sqlserver,1433` (DNS de compose, **NO** `localhost`) |
| Backend en host → SQL Server local            | `Server=localhost,1433`                                     |
| Healthcheck del SQL Server (dentro del contenedor) | `localhost,1433` (referido al propio contenedor)      |

Detalles del `ConnectionStrings__DefaultConnection` que verás en
`compose.yaml`:

```
Server=sqlserver,1433;
Database=ApplicationDb;
User Id=sa;
Password=${SA_PASSWORD};
TrustServerCertificate=True;
```

`TrustServerCertificate=True` se usa porque el certificado autofirmado
del contenedor no es de confianza y esta opción evita el handshake
extendiendo la validación.

---

## Solución de problemas

| Síntoma                                                                          | Causa probable y solución                                                                                                                            |
|----------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------|
| `500` al hacer login y log dice `JWT settings are missing or invalid`            | Falta `Jwt__SecretKey` o es < 32 caracteres. Ajusta `.env` y reinicia el backend.                                                                     |
| `backend` queda en restart-loop con `A network-related or instance-specific error` | `sqlserver` aún no está sano o el nombre DNS no resuelve. Verifica `podman compose ps` y `podman compose logs sqlserver`.                              |
| `database-init` termina con código distinto de 0                                | Contraseña `SA_PASSWORD` no cumple la política MSSQL o `schema.sql` choca con un estado previo. Revisa `podman compose logs database-init` y, si quieres resetear, ejecuta `podman compose down -v`. |
| `CORS policy: No 'Access-Control-Allow-Origin' header is present...`             | El frontend no está en `Cors:AllowedOrigins`. Añade la URL exacta (incluido el esquema y puerto) y reinicia el backend.                              |
| El frontend carga pero las llamadas devuelven `401`                              | El JWT está vencido (por defecto 60 min). Vuelve a iniciar sesión. Si es inmediatamente después de un cambio de `Jwt__SecretKey`, los tokens firmados con la llave anterior ya no son válidos. |
| `ECONNREFUSED 127.0.0.1:1433` desde el backend contenedor                        | Estás apuntando a `localhost` en lugar de `sqlserver`. Edita `compose.yaml` (`ConnectionStrings__DefaultConnection`).                                |
| `pnpm install` falla con `ERR_PNPM_PEER_ISSUES`                                 | Versión de Node o pnpm desalineada con `packageManager` (`pnpm@9.0.0`). Usa `nvm use` y Corepack: `corepack enable && corepack prepare pnpm@9.0.0 --activate`. |
| `dotnet` no se reconoce                                                           | No hay .NET 8 SDK instalado; este repositorio no viene con `dotnet` preinstalado en todos los entornos.                                             |
| `next build` falla por `NEXT_PUBLIC_API_URL undefined`                           | Construir la imagen sin pasar el `ARG NEXT_PUBLIC_API_URL`. Revisa `compose.yaml` (`build.args`) o pasa `NEXT_PUBLIC_API_URL=http://localhost:8080 pnpm build`. |
| El buzón de notificaciones no aparece                                            | Comportamiento esperado hoy. Revisa la sección [Notificaciones](#notificaciones) para implementarlo.                                                  |

---

## Licencia

Proyecto interno de referencia — sin licencia pública. Ajustar según
las políticas de tu organización antes de cualquier distribución.
