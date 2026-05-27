# Deployment Guide

How to run **Manufacturing Cost Management** on Ubuntu and Windows.

The architecture is the same on every platform:

```
[ Browser ] ─→ [ ASP.NET Core 8 (Kestrel, port 5080) ] ─→ [ SQL Server (port 1433) ]
                                                         (Docker container)
```

The database schema and ~250 seed records (suppliers, materials, products, BOM,
production orders, labor & overhead costs, departments, roles, permissions) are
created automatically by `DbSeeder` on first launch — no manual data import.

---

## A. Ubuntu Server (22.04 / 24.04, x86_64)

### 1. Install prerequisites

```bash
# Docker
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg lsb-release
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | \
    sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
    https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | \
    sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Add yourself to the docker group (logout/login after)
sudo usermod -aG docker $USER

# .NET 8 SDK
sudo apt-get install -y dotnet-sdk-8.0

# Git
sudo apt-get install -y git

# Verify
docker --version
docker compose version
dotnet --version    # should print 8.0.x
```

### 2. Clone the project

```bash
git clone git@dr4kedk:dr4kedk/mfg-management.git
cd mfg-management
```

### 3. Start the database with the Linux compose file

The repo ships with two compose files:

| File                       | Image                                          | Use on               |
|----------------------------|------------------------------------------------|----------------------|
| `docker-compose.yml`       | `mcr.microsoft.com/azure-sql-edge:latest`      | Apple Silicon Mac    |
| `docker-compose.linux.yml` | `mcr.microsoft.com/mssql/server:2022-latest`   | Ubuntu / Linux x86_64 |

Use the **Linux** file on the server — it ships with `sqlcmd`, has a working
healthcheck, and uses the official full SQL Server image:

```bash
docker compose -f docker-compose.linux.yml up -d
docker compose -f docker-compose.linux.yml logs -f sqlserver
# Ctrl-C once you see "SQL Server is now ready"
```

> The container name, volume name, port, and SA password are identical in both
> files, so the app's connection string works without any change.

### 4. Build & run the app

```bash
dotnet restore
dotnet build -c Release
ASPNETCORE_URLS="http://0.0.0.0:5080" \
ASPNETCORE_ENVIRONMENT=Production \
dotnet run --project ManufacturingCostManagement.Web -c Release --no-launch-profile
```

On first launch the seeder:
1. Applies EF Core migrations (creates all tables)
2. Inserts roles, departments, users, suppliers, materials, products, BOM,
   production orders, labor and overhead costs, permissions

Open `http://<server-ip>:5080` and log in as `admin / admin123`.

### 5. Open the firewall

```bash
sudo ufw allow 5080/tcp
sudo ufw allow 1433/tcp   # only if you need direct DB access from outside
```

### 6. Run as a service (auto-start on boot)

Create `/etc/systemd/system/mfgcost.service`:

```ini
[Unit]
Description=Manufacturing Cost Management
After=network.target docker.service
Requires=docker.service

[Service]
WorkingDirectory=/home/ubuntu/mfg-management
ExecStart=/usr/bin/dotnet run --project ManufacturingCostManagement.Web -c Release --no-launch-profile
Restart=always
RestartSec=10
User=ubuntu
Environment=ASPNETCORE_URLS=http://0.0.0.0:5080
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now mfgcost
sudo systemctl status mfgcost
sudo journalctl -u mfgcost -f      # tail logs
```

### 7. (Optional) nginx reverse proxy on port 80

```bash
sudo apt-get install -y nginx
```

`/etc/nginx/sites-available/mfgcost`:

```nginx
server {
    listen 80;
    server_name your-domain-or-ip;

    location / {
        proxy_pass         http://127.0.0.1:5080;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/mfgcost /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

Now reachable at `http://your-domain-or-ip/`.

### 8. Stopping / updating

```bash
# Stop
sudo systemctl stop mfgcost
docker compose -f docker-compose.linux.yml down       # keeps DB volume
docker compose -f docker-compose.linux.yml down -v    # ALSO deletes DB (will re-seed on next start)

# Pull updates and restart
cd ~/mfg-management
git pull
dotnet build -c Release
sudo systemctl restart mfgcost
```

---

## B. Windows 10 / 11

### Option 1 — Visual Studio 2022 (easiest)

1. Install **Visual Studio 2022** with the *ASP.NET and web development* workload (this includes .NET 8 SDK and **SQL Server LocalDB**).
2. Clone the repo:
   ```powershell
   git clone git@dr4kedk:dr4kedk/mfg-management.git
   cd mfg-management
   ```
3. Open `ManufacturingCostManagement.sln`.
4. Edit `ManufacturingCostManagement.Web/appsettings.json` connection string to use LocalDB (no Docker needed):
   ```json
   "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ManufacturingCostDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
   ```
5. Set `ManufacturingCostManagement.Web` as the startup project and press **F5**.
6. Browser opens automatically; login as `admin / admin123`.

### Option 2 — Command line + Docker Desktop

1. **Install .NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
2. **Install Docker Desktop for Windows** — https://docs.docker.com/desktop/install/windows-install/ (enable WSL 2 backend).
3. **Install Git** — https://git-scm.com/download/win
4. Clone & start the database (Windows x64 uses the same file as Linux):
   ```powershell
   git clone git@dr4kedk:dr4kedk/mfg-management.git
   cd mfg-management
   docker compose -f docker-compose.linux.yml up -d
   ```
5. Run the app:
   ```powershell
   dotnet build
   $env:ASPNETCORE_URLS="http://localhost:5080"
   dotnet run --project ManufacturingCostManagement.Web --no-launch-profile
   ```
6. Open `http://localhost:5080` and log in as `admin / admin123`.

### Option 3 — Local SQL Server Express (no Docker)

1. Install .NET 8 SDK and Git as above.
2. Install **SQL Server 2022 Express** — https://www.microsoft.com/sql-server/sql-server-downloads
3. Edit the connection string in `appsettings.json`:
   ```json
   "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ManufacturingCostDB;Trusted_Connection=True;TrustServerCertificate=True"
   ```
4. `dotnet run --project ManufacturingCostManagement.Web`

---

## Default accounts (seeded automatically)

| Role        | Username     | Password    |
|-------------|--------------|-------------|
| Admin       | `admin`      | `admin123`  |
| Manager     | `manager`    | `manager123`|
| Accountant  | `accountant` | `acc123`    |
| Employee    | `phuc.tran1` | `password123` (any of `*.{lastname}{N}`) |

Plus 22 regular users with `password123`.

---

## Customising the connection string

Don't edit `appsettings.json` for production — use the environment variable
instead, which overrides the JSON setting:

```bash
export ConnectionStrings__DefaultConnection="Server=db.example.com,1433;Database=ManufacturingCostDB;User Id=sa;Password=YourStrongP@ss;TrustServerCertificate=True;Encrypt=False"
```

Place this in the systemd unit's `[Service] Environment=` lines (Ubuntu) or
set in a PowerShell session (Windows).

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `Failed to bind to address http://...: address already in use` | Another process holds port 5080. Kill it: `sudo lsof -ti:5080 \| xargs -r sudo kill -9` |
| `Invalid object name 'X'` on startup | Migrations didn't apply. `dotnet ef database update --project ManufacturingCostManagement.DAL --startup-project ManufacturingCostManagement.Web` |
| SQL container restart-looping on Apple Silicon | Use `mcr.microsoft.com/azure-sql-edge:latest` in `docker-compose.yml`. The full SQL Server image needs x86_64. |
| Login returns 500 with `Invalid object name 'RolePermissions'` | Pending migration not applied yet — restart the app, which runs `MigrateAsync` on startup, or run `dotnet ef database update`. |
| Vietnamese characters showing as `&#x1EDD;` in charts | Hard refresh (Cmd/Ctrl-Shift-R). Razor view cache. |
| Charts blank, console error `Identifier 'top' has already been declared` | Hard refresh — old cached script. |
| Want to wipe data and start fresh | `docker compose down -v && docker compose up -d` then restart the app. |
