# Health Check Template

A small three tier starting point for a service health page. An HTML page calls a
C# backend, and the C# backend calls a Python API that performs the actual check
against a hosted service.

This is a personal template. Copy it, rename it, gut it, and rebuild it for
whatever service you need to monitor.

## Architecture

```
+-----------+        +----------------+        +----------------+        +------------------+
|  Browser  |  HTTP  |  C# backend    |  HTTP  |  Python API    |  HTTP  |  Hosted service  |
|  (HTML)   | -----> |  (ASP.NET Core)| -----> |  (FastAPI)     | -----> |  (target)        |
+-----------+        +----------------+        +----------------+        +------------------+
   index.html            /api/health              /health                  any endpoint
```

Each tier runs independently and can live on a different host. In practice the
Python API usually sits next to the service being monitored, and the C# backend
serves the page and acts as a server side proxy so the browser never needs to
know the address of the Python host.

## Repository layout

```
health-check-template/
├── web/                          # C# backend and front end
│   ├── Web.csproj
│   ├── Project.cs                # top level statements, entry point
│   └── wwwroot/
│       └── index.html            # the front end page
└── python-api/                   # Python health endpoint
    ├── main.py
    └── requirements.txt
```

## Prerequisites

- .NET SDK 8.0 or later
- Python 3.10 or later
- A service you want to check

## Running it

### 1. Python API

```bash
cd python-api
python -m venv .venv
source .venv/bin/activate          # Windows: .venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8999
```

Set `UPSTREAM_URL` in `main.py` to the endpoint you want to verify. The service
returns `{"status": true}` when the upstream responds with a 200, and
`{"status": false}` otherwise.

### 2. C# backend

```bash
cd web
dotnet run
```

Kestrel prints the address it binds to. By default this is something like
`http://localhost:5000`.

### 3. Browser

Open the address Kestrel reported, for example:

```
http://127.0.0.1:5000/
```

Click **Check health**. The page calls `/api/health` on the C# backend, which
forwards the request to the Python API and returns the result.

## Endpoints

### Python API

| Method | Path      | Response                          |
|--------|-----------|-----------------------------------|
| GET    | `/health` | `{"status": true}` or `{"status": false}` |

### C# backend

| Method | Path          | Response                                              |
|--------|---------------|-------------------------------------------------------|
| GET    | `/`           | The front end page                                    |
| GET    | `/api/health` | `{"status": true\|false}` or an error object with a 502 or 504 |

## Configuration

The Python host is configured in `Project.cs` through the named `HttpClient`:

```csharp
builder.Services.AddHttpClient("orin", client =>
{
    client.BaseAddress = new Uri("http://<change to your IP address>:8999/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
```

Note the trailing slash in `BaseAddress`. Without it, relative path resolution
does not behave as expected.

Move these values into `appsettings.json` if the address changes often enough
to justify avoiding a rebuild.

## Notes and gotchas

**The IP address in /web/Project.cs will need to be changed to either your own
or a loopback, depending on where the Python API is hosted.** I'm just running
it on another node as my own example.

**I know this is not the most efficient way to do it.** OpenWebUI has an API 
built in that I'm going through several hops to check when I could just do
everything in a single HTML page, because I do exactly that on another project. 
This is a reference and I used it as an example. 

**I originally compiled the web portion on a laptop and the python API portion on
an Nvidia Jetson Orin Nano.** Results may vary and there's dependencies you'll 
need to install to get it to work. Again, this is just a personal template. 

**Prefer the literal loopback address.** `http://127.0.0.1:5000/` is more
predictable than `http://localhost:5000/`. Some browsers resolve `localhost` to
IPv6 first, and a Kestrel instance bound only to IPv4 will not answer on `::1`.

**Timeout ordering matters.** Keep the Python upstream timeout shorter than the
C# client timeout. That way the Python service returns `{"status": false}` on a
slow upstream instead of the C# side giving up first. The page distinguishes the
two cases, and the distinction is useful during an incident.

**Bind to all interfaces for LAN access.**

```bash
dotnet run --urls "http://0.0.0.0:5000"
```

The page is then reachable from another machine at `http://<lan-ip>:5000/`.

## Status

Personal template, provided as is. No authentication, no rate limiting, and no
hardening of any kind. Add whatever the environment requires before exposing it
to anything beyond a trusted network.
