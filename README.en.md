# NetStatusSharp

[中文说明](README.md)

NetStatusSharp is a Windows Forms desktop utility for inspecting TCP and UDP connections opened by local processes on Windows.

It lists active connections, shows the owning process, and provides a lightweight UI for filtering and refreshing results.

## Features

- View current TCP and UDP connections on Windows
- Filter by process name, PID, local port, remote port, protocol, and TCP state
- Show process name together with the executable icon
- Refresh the connection list on demand from the desktop UI

## Solution Layout

- `NetStatusSharp/`: WinForms application and filtering UI
- `NetStatusAPI/`: low-level wrapper for connection enumeration and process metadata lookup
- `NetStatusSharp.sln`: Visual Studio solution file

## Technical Details

- Target framework: `.NET Framework 4.8`
- UI: `Windows Forms`
- Native APIs: `GetExtendedTcpTable` and `GetExtendedUdpTable`
- Platform: `Windows only`

## Build

Open `NetStatusSharp.sln` in Visual Studio and build the solution.

Command line example:

```powershell
msbuild .\NetStatusSharp.sln /p:Configuration=Release
```

## Run

After building, start:

```text
NetStatusSharp\bin\Release\NetStatusSharp.exe
```
