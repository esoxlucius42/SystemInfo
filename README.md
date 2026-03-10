# SystemInfo

A real-time **Windows system monitoring** Terminal User Interface (TUI) built with C# and [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui).

## Features

| Panel | Information displayed |
|---|---|
| **System** | Machine name, OS description, platform, version |
| **Network** | All active IPv4 addresses |
| **Memory** | Total, available, and used RAM (GB / %) |
| **Temperature** | CPU / thermal-zone temperatures via WMI (requires admin) |
| **Storage** | Per-drive capacity, used GB, free GB, and a live two-color bar (red = used · green = free) |

The display refreshes automatically every **500 ms**. The screensaver and monitor sleep are suppressed while the app is running and restored on exit.

## TUI Layout

```
┌─ SystemInfo ────────────────────────────────────────────────────────────────┐
│ ┌─ System ──────────────────────┐  ┌─ Network ───────────────────────────┐ │
│ │ Machine : DESKTOP-ABC         │  │ IP: 192.168.1.10                    │ │
│ │ OS      : Microsoft Windows…  │  │ IP: 10.0.0.5                        │ │
│ │ Platform: Win32NT             │  │                                     │ │
│ │ Version : 10.0.22621.0        │  │                                     │ │
│ └───────────────────────────────┘  └─────────────────────────────────────┘ │
│ ┌─ Temperature ─────────────────┐  ┌─ Memory ────────────────────────────┐ │
│ │ ACPI\Thermal…: 45.0 °C        │  │ Total    :   16.00 GB               │ │
│ │ (run as admin to read temps)  │  │ Available:    8.42 GB               │ │
│ │                               │  │ Used     :   47.38 %                │ │
│ └───────────────────────────────┘  └─────────────────────────────────────┘ │
│ ┌─ Storage ───────────────────────────────────────────────────────────────┐ │
│ │ C:\   476.94 GB   234.44 GB used  [████████████████░░░░░░]  242.50 GB free│
│ │ D:\   931.51 GB   181.31 GB used  [██████░░░░░░░░░░░░░░░░]  750.20 GB free│
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                          Press Q to quit     │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Prerequisites

- **Windows** (10 / 11 or Windows Server 2016+)
- [**.NET 8 SDK**](https://dotnet.microsoft.com/download/dotnet/8.0) or newer

> **Note:** .NET 9 SDK is also compatible — the project targets `net8.0-windows`.

## Build

```bash
cd SystemInfo        # the subfolder containing SystemInfo.csproj
dotnet build
```

A debug build is placed in `SystemInfo\bin\Debug\net8.0-windows\`.

To build a self-contained release executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

## Run

```bash
cd SystemInfo        # the subfolder containing SystemInfo.csproj
dotnet run
```

Or run the compiled binary directly:

```bash
.\bin\Debug\net8.0-windows\SystemInfo.exe
```

Press **Q** (or close the terminal window) to exit.

## Temperature readings

Temperature data is read from WMI (`MSAcpi_ThermalZoneTemperature`).  
This requires **administrator privileges**. If the app is started without them the Temperature panel will show:

```
(run as admin to read temps)
```

To enable temperature readings, launch the terminal as Administrator before running the app.

## Project structure

```
SystemInfo/
├── SystemInfo.sln
└── SystemInfo/
    ├── SystemInfo.csproj   # SDK-style, targets net8.0-windows
    ├── Program.cs          # TUI layout and refresh loop (Terminal.Gui)
    ├── IPAddress.cs        # Enumerates local IPv4 addresses
    ├── Temperature.cs      # WMI thermal-zone query
    ├── Storage.cs          # DriveInfo fixed-drive enumeration
    └── StorageBar.cs       # Custom two-color Terminal.Gui View
```

## Dependencies

| Package | Purpose |
|---|---|
| [Terminal.Gui 1.x](https://www.nuget.org/packages/Terminal.Gui) | TUI framework (panels, labels, layout, event loop) |
| [System.Management 9.x](https://www.nuget.org/packages/System.Management) | WMI access for temperature readings |

## License

[MIT](LICENSE) © 2022-2026 Esox Lucius
