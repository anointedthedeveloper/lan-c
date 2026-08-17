# LanC - LAN Command & Control System

LanC is a fully offline, LAN-based IT management system that allows a server machine to manage, control, and deploy software to client machines on the same network. It consists of two standalone desktop applications — a **Server** and a **Client** — both built in C# and compiled as self-contained `.exe` files requiring no additional installations.

----

## Overview

LanC is designed to simplify software deployment and system management across a local area network. The server administrator can upload installers, issue commands, monitor connected machines, and remotely control client systems — all from a desktop GUI. Clients auto-discover the server, run silently in the background, and execute commands even when their GUI is not open.

---

## Architecture

```
LAN Network
│
├── LanServer.exe  (runs on the main/admin machine)
│   ├── Admin GUI Panel          - manage clients, upload files, issue commands
│   ├── Embedded HTTP Server     - serves files + web fallback for non-Windows clients
│   ├── WebSocket Hub            - real-time command push to all connected clients
│   ├── UDP Beacon               - broadcasts server presence on LAN for auto-discovery
│   ├── Client Manager           - tracks all connected machines (name, IP, status)
│   ├── Command Dispatcher       - sends targeted or broadcast commands
│   ├── File Manager             - handles uploaded installer files
│   └── Config                   - stores admin password and server settings
│
└── LanClient.exe  (runs on each managed machine)
    ├── Background Service       - runs silently, starts on boot
    ├── System Tray Icon         - shows connection status, quick access
    ├── GUI Dashboard            - view server info, command history, status
    ├── Progress Window          - auto-pops up during installs showing progress
    ├── UDP Discovery            - auto-finds server on LAN, no manual config
    ├── WebSocket Connection     - receives real-time commands from server
    ├── Command Executor         - handles install, download, shutdown commands
    └── Startup Manager          - registers itself to run on Windows boot
```

---

## Features

### Server
- Desktop GUI admin panel built with WinForms
- First-launch password setup (one-time), default is `admin234`
- Upload `.exe`, `.msi`, and other installer files to the server
- Select installer type per upload for correct silent install flags
- View all connected client machines with their name, IP, and online/offline status
- Issue commands to all clients or select specific machines
- Command types:
  - **Download** - push a file to client machines
  - **Download & Install** - push and silently install a file
  - **Shutdown** - remotely shut down selected or all machines
- Command history and logs per session
- Embedded HTTP web server for web-based file access (fallback for Mac/Linux)
- UDP beacon that broadcasts server presence so clients find it automatically
- Fully offline, no internet required

### Client
- Single self-contained `.exe`, no installation wizard needed
- Auto-discovers server on LAN via UDP broadcast, no manual IP entry
- Runs as a background process silently after launch
- Registers itself to auto-start on Windows boot
- Lives in the system tray with a status icon (connected/disconnected)
- Desktop shortcut opens the GUI dashboard
- GUI dashboard shows:
  - Server connection status
  - Server IP and name
  - This machine's computer name and IP
  - List of received commands and their results
  - Current status
- When a command is received (even in tray/background mode):
  - Progress window automatically pops up
  - Shows download progress, install progress, and result
- Supports silent installation of:
  - `.msi` files
  - `.exe` NSIS installers
  - `.exe` Inno Setup installers
  - `.exe` InstallShield installers
- Handles installs that require admin privileges automatically (UAC elevation)
- Web fallback: non-Windows clients (Mac/Linux) can visit the server IP in a browser to download files manually

---

## Silent Install Flags

When uploading a file on the server, the admin selects the installer type. LanC automatically applies the correct silent flags:

| Installer Type | Flags Applied |
|---|---|
| MSI | `/quiet /norestart` via `msiexec` |
| NSIS `.exe` | `/S` |
| Inno Setup `.exe` | `/VERYSILENT /NORESTART` |
| InstallShield `.exe` | `/s /v"/qn"` |

This ensures installs run completely silently with no prompts on the client machine.

---

## Tech Stack

| Component | Technology |
|---|---|
| Server app | C# WinForms (.NET) |
| Client app | C# WinForms (.NET) |
| WebSocket | Fleck (NuGet) |
| JSON messaging | Newtonsoft.Json (NuGet) |
| HTTP file server | Embedded via `System.Net.HttpListener` |
| UDP discovery | `System.Net.Sockets.UdpClient` |
| Tray icon | `System.Windows.Forms.NotifyIcon` |
| Boot startup | Windows Registry (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) |

All dependencies are NuGet packages bundled into the final `.exe`. No external runtime or framework installation is required on target machines.

---

## Project Structure

```
lan-c/
├── README.md
│
├── LanServer/
│   ├── LanServer.sln
│   └── LanServer/
│       ├── Program.cs               # Entry point, first-launch password setup
│       ├── MainForm.cs              # Admin panel GUI
│       ├── WebServer.cs             # Embedded HTTP + WebSocket server
│       ├── UdpBeacon.cs             # Broadcasts server presence on LAN
│       ├── ClientManager.cs         # Tracks connected clients
│       ├── CommandDispatcher.cs     # Sends commands to clients
│       ├── FileManager.cs           # Handles file uploads and storage
│       └── Config.cs                # Password and settings storage
│
└── LanClient/
    ├── LanClient.sln
    └── LanClient/
        ├── Program.cs               # Entry point, single instance enforcement
        ├── TrayApp.cs               # Tray icon, context menu, app lifecycle
        ├── MainForm.cs              # GUI dashboard
        ├── ProgressForm.cs          # Install/download progress popup
        ├── ServerDiscovery.cs       # UDP listener for auto server discovery
        ├── WebSocketClient.cs       # Connects to server, receives commands
        ├── CommandExecutor.cs       # Executes install, download, shutdown
        └── StartupManager.cs        # Manages Windows boot auto-start
```

---

## How to Build

### Requirements
- Visual Studio 2022 or later
- .NET 8 SDK
- NuGet packages (auto-restored on build):
  - `Fleck`
  - `Newtonsoft.Json`

### Server
```
cd LanServer
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
Output: `LanServer/bin/Release/net8.0/win-x64/publish/LanServer.exe`

### Client
```
cd LanClient
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
Output: `LanClient/bin/Release/net8.0/win-x64/publish/LanClient.exe`

---

## How to Use

### Server Setup
1. Run `LanServer.exe` on the main/admin machine
2. On first launch, you will be prompted to set an admin password (default: `admin234`)
3. The server starts automatically and begins broadcasting its presence on the LAN
4. The admin panel opens showing connected clients, file upload, and command controls

### Client Setup
1. Copy `LanClient.exe` to the target machine and run it
2. The client automatically scans the LAN for the server via UDP
3. Once found, it connects and registers itself with the server
4. The client minimizes to the system tray and starts on every boot
5. Click the tray icon or desktop shortcut to open the GUI dashboard

### Issuing Commands (Server)
1. In the admin panel, select target machines (or leave as "All Connected")
2. For file deployment: upload the file, select installer type, click "Deploy"
3. For shutdown: select machines, click "Shutdown"
4. Monitor command status in the command log panel

### Web Fallback (Mac/Linux)
1. On a non-Windows machine, open a browser
2. Navigate to `http://<server-ip>:<port>` (port shown in server admin panel)
3. Browse and download available files manually

---

## Offline Support

LanC is fully designed for offline LAN environments:
- No internet connection required at any point
- All communication is local UDP and WebSocket over LAN
- Files are stored and served locally from the server machine
- No cloud services, telemetry, or external dependencies

---

## Security Notes

- Admin panel is password protected (set on first launch)
- Password is stored locally in a config file on the server machine
- All communication is within the local network only
- It is recommended to use LanC only on trusted internal networks

---

## Limitations

- Client auto-discovery requires the server and client to be on the same LAN subnet
- Silent install flags may not work for all custom or proprietary installers
- UAC elevation on client machines requires the logged-in user to have admin rights or the machine to allow elevation
- Web fallback is for file download only; Mac/Linux clients cannot receive push commands

---

## License

Internal use. Not intended for public distribution.
