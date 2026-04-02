# NetStatusSharp

[English README](README.en.md)

NetStatusSharp 是一个基于 Windows Forms 的网络连接查看工具，用于分析当前机器上各个进程占用的 TCP / UDP 连接情况。

它可以列出本机连接、显示连接所属进程，并通过桌面界面快速进行筛选和刷新。

## 功能特性

- 查看当前 Windows 系统中的 TCP 和 UDP 连接
- 按进程名、PID、本地端口、远程端口、协议、TCP 状态进行筛选
- 显示进程名称和对应图标
- 在桌面界面中一键刷新连接列表

## 项目结构

- `NetStatusSharp/`：WinForms 主程序与筛选界面
- `NetStatusAPI/`：底层网络连接枚举与进程信息读取封装
- `NetStatusSharp.sln`：Visual Studio 解决方案文件

## 技术说明

- 目标框架：`.NET Framework 4.8`
- UI：`Windows Forms`
- 底层能力：调用 Windows 原生 API `GetExtendedTcpTable`、`GetExtendedUdpTable`
- 运行平台：`仅支持 Windows`

## 构建方式

使用 Visual Studio 打开 `NetStatusSharp.sln` 后直接编译即可。

命令行示例：

```powershell
msbuild .\NetStatusSharp.sln /p:Configuration=Release
```

## 运行方式

编译完成后，可运行：

```text
NetStatusSharp\bin\Release\NetStatusSharp.exe
```

主界面支持以下筛选项：

- 进程名
- PID
- 本地端口
- 远程 TCP 端口
- 协议
- TCP 状态

## GitHub Actions

- 推送到 `main` 或提交 Pull Request 时，会自动执行编译检查。
- 推送形如 `v1.0.0` 的标签时，会自动编译并发布 GitHub Release。
- Release 附带的 `zip` 文件只包含运行应用所需的核心文件，并在构建产物存在时附带额外运行相关文件。
