# Usurper Reborn — Docker Deployment Guide

Host your own Usurper Reborn MUD server using Docker.

Verified against v1.1.1 (2026-09-07): `docker compose up -d` builds all three
images, serves the login menu on port 4000, and browser play, the dashboards,
the stats API, and the language files all answer through nginx on port 80.

---

## Quick Start

```bash
git clone https://github.com/binary-knight/usurper-reborn.git
cd usurper-reborn

# Edit docker-compose.yml first: replace "YourAdminName" with your username and
# set BALANCE_PASS to a real password (the admin dashboard stays locked until you do)
docker compose up -d
```

**Game server**: port 4000 (raw TCP — BBS clients, MUD clients, SSH relay)
**Web interface**: port 80 (browser play, live stats, admin dashboards)

---

## Architecture

```
docker-compose.yml
├── usurper-game     Game server + world simulator (C#/.NET 8)
├── usurper-web      Web proxy + stats API + dashboards (Node.js)
├── usurper-nginx    Reverse proxy (nginx)
└── volume: usurper-data  Shared SQLite database (/var/usurper/)
```

All three containers share the `/var/usurper/` volume where the SQLite database lives. The game server writes game state; the web proxy reads it for stats and dashboards and opens a second, writable handle for the admin panel (bans, player edits, config changes).

The web proxy opens the database once at startup and never reconnects, so it must not start before the game server has created the file. The compose file handles this: the game service has a healthcheck that passes once `usurper_online.db` exists, and the web service waits for it. On a brand new deploy the first `docker compose up` therefore takes a few seconds longer before the web container starts.

---

## Configuration

### docker-compose.yml

Key settings to customize before first run:

```yaml
services:
  usurper-game:
    command:
      - "--admin"
      - "YourAdminName"    # Replace with your username — grants God-level admin access
      - "--sim-interval"
      - "30"               # World sim tick rate in seconds (default: 30)
      # Optional additional flags:
      # - "--npc-xp"
      # - "0.25"           # NPC XP gain multiplier (default: 0.25)
      # - "--save-interval"
      # - "5"              # NPC state persistence interval in minutes (default: 5)

  usurper-web:
    environment:
      - BALANCE_USER=admin       # Dashboard login username
      - BALANCE_PASS=...         # Dashboard login password. Unset or "changeme" keeps
                                 # /balance and /admin locked to the change-password flow.
```

### Game Server Flags

| Flag | Default | Description |
|------|---------|-------------|
| `--mud-server` | — | Start as MUD server (required) |
| `--mud-port <port>` | 4000 | TCP listen port |
| `--db <path>` | (next to exe) | SQLite database path |
| `--auto-provision` | off | Auto-create accounts for trusted auth connections |
| `--log-stdout` | off | Route logs to stdout instead of files |
| `--sim-interval <sec>` | 60 | World simulation tick interval (compose sets 30) |
| `--npc-xp <mult>` | 0.25 | NPC XP gain multiplier (0.01–10.0) |
| `--save-interval <min>` | 5 | How often NPC state is saved to database |
| `--admin <user>` | — | Bootstrap admin user (repeatable) |
| `--no-worldsim` | off | Disable the embedded world simulator (run `--worldsim` as a separate process instead) |
| `--worldsim` | — | Headless 24/7 world simulator with no players (only with `--no-worldsim` on the server) |
| `--idle-timeout <min>` | 15 | Idle disconnect for BBS door sessions (1–60) |

Run `UsurperReborn --help` for the full list, including the single-player and BBS door flags.

### Web Proxy Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `MUD_MODE` | `1` | `1` = direct TCP to game server, `0` = legacy SSH relay |
| `MUD_HOST` | `127.0.0.1` | Game server hostname (use container name in Docker) |
| `MUD_PORT` | `4001` | Game server TCP port; compose sets `4000` to match `--mud-port` |
| `DB_PATH` | `/var/usurper/usurper_online.db` | SQLite database path |
| `BALANCE_USER` | `admin` | Dashboard login username |
| `BALANCE_PASS` | `changeme` | Dashboard login password; the shipped default keeps `/balance` and `/admin` locked |
| `BALANCE_SECRET` | (random) | JWT signing secret (auto-generated if not set) |
| `WS_MAX_CONN_PER_IP` | `8` | Browser sessions allowed per client IP |
| `BUG_REPORT_WEBHOOK_URL` | (unset) | Discord webhook that receives in-game `/bug` reports; reports are dropped when unset |
| `DISCORD_BOT_TOKEN` | (unset) | With `DISCORD_GOSSIP_CHANNEL_ID`, mirrors world news to a Discord channel (`discord.js` is installed in the image) |
| `DISCORD_GOSSIP_CHANNEL_ID` | (unset) | Channel for the news bridge |
| `DISCORD_STATS_CHANNEL_ID` | (unset) | Optional channel for a live stats embed |
| `GITHUB_PAT` | (unset) | Read-only token for the sponsors list on the landing page |
| `USURPER_NUKE_SCRIPT` | (unset) | Script the admin panel's world-wipe runs; leave unset unless you have written one |

The admin panel shows the game version from `/opt/usurper/version.txt`, which the web image writes from `GameConfig.Version` at build time. Rebuild the web image when you update.

---

## Connecting

### Browser
Open `http://your-server` in a browser. Click "Play Now" for the embedded terminal.

### SSH (requires additional sshd setup)
The Docker setup doesn't include an SSH server. Players connect via the browser terminal or raw TCP. For SSH access, run sshd on the host (or in a container that has the game binary) with a `ForceCommand` of `UsurperReborn --mud-relay --mud-port 4000`; the relay puts the terminal in raw mode and forwards to the game server with the player's login. `DOCS/SERVER_DEPLOYMENT.md` has the full sshd setup used on the public server.

### BBS Passthrough
BBS software connects via raw TCP to port 4000 and sends an AUTH header:

```
AUTH:username:connectionType\n
```

With `--auto-provision` enabled (default in Docker), the game server automatically creates an account if the username doesn't exist. No separate registration needed.

**Example BBS integration** (Synchronet JavaScript):
```javascript
var sock = new Socket();
sock.connect("your-server", 4000);
sock.send("AUTH:" + user.alias + ":BBS\n");
// Relay I/O between BBS user and game socket
```

**AUTH header formats**:
```
AUTH:username:connectionType              Trusted auth (auto-provision if enabled)
AUTH:username:password:connectionType     Password auth
AUTH:username:password:REGISTER:type      Register new account with password
```

Connection types: `BBS`, `MUD`, `Web`, `SSH`, `Steam`, `Local`

### MUD Clients (Mudlet, TinTin++, etc.)
Connect via raw TCP to port 4000. The server will detect no AUTH header (500ms timeout) and present an interactive login menu.

---

## Dashboards

All dashboards are accessible via the web interface:

| URL | Description | Auth Required |
|-----|-------------|---------------|
| `/` | Landing page with embedded terminal + live stats | No |
| `/dashboard` | NPC analytics (activities, relationships, timeline) | No |
| `/steam` | Steam client download and setup page | No |
| `/balance` | Combat balance analytics (win rates, damage stats) | Yes |
| `/admin` | Server admin panel (bans, player edits, config, world wipe) | Yes |

Dashboard credentials are set via `BALANCE_USER` and `BALANCE_PASS` environment variables.

---

## Customization

### Custom Landing Page
Mount your own `index.html` to replace the default landing page:

```yaml
services:
  usurper-web:
    volumes:
      - usurper-data:/var/usurper
      - ./my-landing-page.html:/opt/usurper/web/index.html
```

The page should include xterm.js for the embedded terminal. See `docker/web/index.html` for the reference implementation (the smaller page Docker ships) or `web/index.html` for the full usurper-reborn.net page. Both talk to the proxy over `/ws` and send terminal resizes as a `{"type":"resize"}` control message. The proxy serves any file under `/opt/usurper/web/`, including `lang/*.json` for the landing page's language switcher.

### SSL/HTTPS
The nginx container listens on port 80 (HTTP). To add SSL:

1. Mount your certificates into the nginx container
2. Add an HTTPS server block to the nginx config

```yaml
services:
  usurper-nginx:
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./my-nginx.conf:/etc/nginx/conf.d/default.conf
      - ./certs:/etc/nginx/certs
```

Or use a reverse proxy like Traefik or Caddy in front of the stack for automatic Let's Encrypt.

### Multiple Admin Users

```yaml
command:
  - "--admin"
  - "Player1"
  - "--admin"
  - "Player2"
```

---

## Operations

### View Logs
```bash
docker compose logs -f usurper-game    # Game server logs
docker compose logs -f usurper-web     # Web proxy logs
docker compose logs -f usurper-nginx   # Nginx access logs
```

### Restart Services
```bash
docker compose restart usurper-game    # Restart game server only
docker compose restart                 # Restart all services
```

### Database Backup
```bash
# Copy database out of the volume
docker compose exec usurper-game cp /var/usurper/usurper_online.db /var/usurper/backup.db
docker compose cp usurper-game:/var/usurper/backup.db ./usurper-backup.db
```

### Update to Latest Version
```bash
git pull
docker compose build
docker compose up -d
```

### View Online Players
```bash
docker compose exec usurper-game sqlite3 /var/usurper/usurper_online.db \
  "SELECT username, display_name FROM players ORDER BY last_login DESC LIMIT 20;"
```

### Stop Everything
```bash
docker compose down           # Stop containers (keeps data)
docker compose down -v        # Stop containers AND delete database volume
```

---

## Troubleshooting

### Game server won't start
Check logs: `docker compose logs usurper-game`

Common issues:
- Database volume permissions — ensure the container can write to `/var/usurper/`
- Port 4000 already in use — change the host port mapping in docker-compose.yml

### Web proxy can't connect to game server
The web proxy connects to `usurper-game:4000` (Docker internal networking). If you changed the game server port, update `MUD_PORT` in the web service environment.

### Browser terminal won't connect
- Check that nginx is running: `docker compose logs usurper-nginx`
- Verify WebSocket upgrade is working: the nginx config proxies `/ws` to the web container
- If behind another reverse proxy, ensure it passes WebSocket upgrade headers

### Players can't connect via BBS
- Ensure port 4000 is exposed and reachable from the BBS server
- BBS must send `AUTH:username:BBS\n` as the first line within 500ms of connecting
- Check game server logs for auth failures: `docker compose logs usurper-game | grep AUTH`

### Database is locked
SQLite doesn't handle high concurrency well. If you see "database is locked" errors, the game server and web proxy may be contending. The proxy's reads use a read-only handle and its writes are limited to admin actions, so this should be rare. Restart the web proxy if it persists.

### Stats and dashboards are empty but the game works
The proxy logs `Database not available` when it started before the database existed and it does not retry. `docker compose restart usurper-web` fixes it. This happens only if the game service's healthcheck was removed or the proxy was started on its own.

---

## System Requirements

- Docker Engine 20.10+ and Docker Compose v2
- 512MB RAM minimum (game server uses ~256MB, web proxy ~64MB, nginx ~16MB)
- 1GB disk for the Docker images + database
- Linux host recommended (the game server binary is compiled for linux-x64; the web image is Node 22 on Alpine)
