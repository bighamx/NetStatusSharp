# NetStatusSharp

NetStatusSharp is a Windows Forms desktop tool for viewing the network connections opened by local processes.

It can enumerate TCP and UDP connections, show the owning process, and filter the results from a lightweight desktop UI.

## Features

- View current TCP and UDP connections on Windows
- Filter by process name, PID, local port, remote port, protocol, and TCP state
- Show process name together with the executable icon
- Refresh the connection list on demand from the desktop UI

## Solution Layout

- `NetStatusSharp/`: Windows Forms application and filtering UI
- `NetStatusAPI/`: low-level API wrapper for enumerating connections and resolving process metadata
- `NetStatusSharp.sln`: Visual Studio solution file

## Technical Details

- Target framework: `.NET Framework 4.0`
- UI: `Windows Forms`
- Native APIs: `GetExtendedTcpTable`, `GetExtendedUdpTable`, and related Windows process metadata lookups
- Platform: `Windows only`

## Build

Open `NetStatusSharp.sln` in Visual Studio and build the solution.

Or build from the command line:

```powershell
msbuild .\NetStatusSharp.sln /p:Configuration=Release
```

## Run

After building, start:

```text
NetStatusSharp\bin\Release\NetStatusSharp.exe
```

The main window lets you filter by:

- Process name
- PID
- Local port
- Remote TCP port
- Protocol
- TCP state

## Development Notes

- This repository intentionally ignores local IDE state and generated build output.
- Older commits previously included Visual Studio cache files and compiled artifacts; those are being cleaned out going forward.
- The app relies on Windows-specific APIs and is not expected to build or run on macOS or Linux.
