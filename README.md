# Smart Home IoT Platform — REST API

ASP.NET Core 8 Web API with Swagger, mapped via EF Core (Pomelo MySQL provider) onto the
project's existing MySQL schema (`Room`, `Device`, `SensorData`, `EventLog`), covering the
CRUD/query endpoints from the requirements spec (rooms, devices, sensor data, event log,
dashboard summary).

> **Heads up about this environment:** I built this project by hand in a sandbox that has no
> .NET SDK installed and no network access to NuGet (`api.nuget.org` is blocked here), so I was
> not able to run `dotnet restore` / `dotnet build` / `dotnet ef` myself to compile-check it.
> The code is complete and follows standard, well-trodden ASP.NET Core + EF Core patterns, but
> please run a build on your own machine before you rely on it — see "Getting it running" below.
> If anything doesn't compile, it's most likely a small typo I can fix in a follow-up once you
> paste me the error.

---

## What's included

```
SmartHomeIoT.Api/
├── SmartHomeIoT.Api.csproj        # net8.0, Pomelo.EntityFrameworkCore.MySql, Swashbuckle
├── Program.cs                     # DI, EF Core, Swagger, CORS, health checks, error middleware
├── appsettings.json                # connection string + feature flags (edit this)
├── appsettings.Development.json
├── schema.sql                      # reference SQL matching the EF Core mappings exactly
├── SmartHomeIoT.Api.http           # ready-to-run sample requests (VS Code REST Client / Rider)
├── Models/                         # Room, Device, SensorData, EventLog, DeviceStatus
├── Data/SmartHomeDbContext.cs      # Fluent API mapping onto the existing table/column names
├── Services/                       # ISensorValidationService — the D-05 valid-range rules
├── DTOs/                           # request/response shapes per controller area
├── Controllers/
│   ├── RoomsController.cs          # HK-06..HK-10 — create/rename/delete/list rooms + devices-in-room
│   ├── DevicesController.cs        # HK-02,03,05,11,13,14 — list/detail/register/update/remove,
│   │                                #   history, event log, control command
│   ├── SensorDataController.cs     # ingest + query measurements, D-05 range validation
│   ├── EventLogController.cs       # list/create system events
│   └── DashboardController.cs      # /dashboard/summary counts for the front page
└── Middleware/ExceptionHandlingMiddleware.cs   # I-03 — consistent JSON error responses
```

## Getting the database ready

You said the database already exists, so by default the API does **not** try to create or
migrate anything on startup — it just connects and maps onto whatever is there
(`Database:ApplyMigrationsOnStartup` and `Database:EnsureCreatedIfNoMigrations` are both `false`
in `appsettings.json`).

The EF Core model (`Data/SmartHomeDbContext.cs`) expects exactly the tables/columns in
[`schema.sql`](schema.sql). Three ways to get there:

1. **Already have the tables?** Just make sure the column names match `schema.sql`
   (case-insensitive in MySQL). If your real table/column names differ, it's a one-line change
   per column in `SmartHomeDbContext.OnModelCreating` (`.HasColumnName("...")`) — nothing else
   needs to change.
2. **Empty database?** Run `schema.sql` against it once (`mysql -u ... -p smarthome < schema.sql`).
3. **Prefer EF Core migrations?** From the project folder:
   ```bash
   dotnet tool install --global dotnet-ef   # first time only
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

## Getting it running

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Edit `appsettings.json` → `ConnectionStrings:Default` with your real MySQL host/credentials
   (or better, keep secrets out of source control with `dotnet user-secrets set "ConnectionStrings:Default" "..."`).
3. From the project folder:
   ```bash
   dotnet restore
   dotnet build
   dotnet run
   ```
4. Open **`http://localhost:5080/swagger`** (or `/` — it redirects there) to browse and try every
   endpoint interactively. `SmartHomeIoT.Api.http` has ready-made sample requests if you use VS
   Code's REST Client extension or JetBrains Rider's HTTP client instead.
5. `GET /health` gives a quick up/down + DB-connectivity check.

## Endpoint overview

All routes are versioned under `/api/v1/` (requirement I-08).

| Area | Method & route | Notes |
|---|---|---|
| Rooms | `GET /rooms` | list, with device counts |
| | `GET /rooms/{id}` | detail incl. devices |
| | `GET /rooms/{id}/devices` | devices in a room |
| | `POST /rooms` | create |
| | `PUT /rooms/{id}` | rename |
| | `DELETE /rooms/{id}` | delete — **409** if devices are still assigned |
| Devices | `GET /devices?roomId=&status=` | list, filterable |
| | `GET /devices/{id}` | full detail (name, id, ip, type, room, registration date) |
| | `POST /devices` | register/pair (manual/admin path — see note below) |
| | `PUT /devices/{id}` | update name/type/room/IP |
| | `DELETE /devices/{id}` | remove — history cascades, events are kept with `DeviceId = null` |
| | `GET /devices/{id}/history?range=24h\|7d\|30d` | sensor history |
| | `GET /devices/{id}/events` | event log for this device |
| | `POST /devices/{id}/command` | records a control command (see note below) |
| SensorData | `GET /sensordata?deviceId=&sensorType=&from=&to=` | query |
| | `POST /sensordata` | ingest — **201** if valid, **422** + EventLog entry if out of range |
| | `DELETE /sensordata/{id}` | admin cleanup |
| EventLog | `GET /eventlog?deviceId=&eventType=&from=&to=` | query |
| | `POST /eventlog` | manual/admin entry |
| Dashboard | `GET /dashboard/summary` | room/device counts, online/offline split, recent events |

## Not included in this API (by design — you asked for the CRUD API specifically)

- **Live MQTT integration.** `POST /devices/{id}/command` and the sensor ingestion endpoint model
  the *outcome* of the MQTT flows from the sequence diagrams (validate → store/reject → log), but
  nothing in this project opens a real connection to Mosquitto. Wiring that in is a background
  `IHostedService` using a client like [MQTTnet](https://github.com/dotnet/MQTTnet) that
  subscribes to `home/+/+/+`, calls into the same validation logic already in
  `SensorValidationService`, and republishes `home/{deviceId}/cmd` when `POST .../command` is
  called. Happy to build that out as a next step if useful.
- **WiFi provisioning / mDNS discovery.** That's a firmware + hub-side network-scanning concern,
  not something a REST API mapped onto the database does.
- **Auth.** Nothing in the requirements spec calls for user accounts/authentication, so none is
  implemented. Easy to add (e.g. ASP.NET Identity or a simple API key) if you want it before
  demo day.

## Design assumptions (please double-check against your final report)

1. **`Device.MacAddress` is the unique physical identity**; `IPv4Address` is just current network
   state and is overwritten on every heartbeat — matches the identify/update logic your docs and
   earlier sequence diagrams describe (MAC found → update record; MAC not found → new device).
2. **Device removal**: `SensorData` cascades (deleted with the device), `EventLog` entries are
   kept with `DeviceId` set to `NULL` so the historical record ("device X was removed on...")
   survives. If you'd rather keep full sensor history too, switch that one line in
   `OnModelCreating` from `Cascade` to `SetNull` (and make `SensorData.DeviceId` nullable).
3. **Room deletion is blocked (409)** while it still has devices assigned, rather than silently
   deleting or orphaning them, since your docs say every device must belong to a room.
4. **Heartbeat/command dispatch protocol** is out of scope here (see previous section) — the API
   only reflects the resulting state in the database.

If any of these should go the other way, they're all small, localized changes — just point me at
the ones you want flipped.
