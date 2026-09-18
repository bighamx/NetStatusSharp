# NetStatusSharp

[English README](README.en.md)

NetStatusSharp 是一个面向 Windows 的轻量网络连接监视器，用于按进程查看和筛选本机 IPv4 / IPv6 TCP / UDP 连接。

![NetStatusSharp 浅色界面](docs/screenshot.png)

## 功能特性

- 查看连接所属进程、PID、协议族、本地端点、远程端点和 TCP 状态
- 按进程名、PID、本地端口、远程端口、协议、IP 版本及 TCP 状态筛选
- IPv6 端点以 `[地址]:端口` 形式显示，表格中的 IP 列区分 IPv4 / IPv6
- 浅色高 DPI WPF 界面，支持表格虚拟化、列排序和多行复制
- 表头显示升序 / 降序排序指示，并支持拖动调整列宽
- 自动记住调整后的列宽，下次启动时恢复（`%APPDATA%\NetStatusSharp\layout.json`）
- 异步采集连接，刷新期间界面保持响应并可随时取消
- 刷新按钮在采集期间原位变为“停止”，标题区域不会闪烁跳动
- 同一 PID 每轮只解析一次进程信息，先过滤连接再读取进程与图标
- 可选每 5 秒自动刷新，底部显示连接数量、耗时和更新时间

## 快捷键

- `F5`：刷新
- `Enter`：应用文本框中的筛选条件
- `Esc`：取消当前刷新
- `Ctrl+L`：清空筛选
- `Ctrl+C`：复制表格中选中的连接

## 项目结构

- `NetStatusSharp/`：.NET 8 WPF 应用、MVVM、主题和连接查询服务
- `NetStatusAPI/`：Windows 原生 TCP / UDP 连接表封装
- `NetStatusSharp.sln`：Visual Studio 解决方案

## 技术说明

- 目标框架：`.NET 8 (net8.0-windows)`
- UI：`WPF`
- 原生 API：`GetExtendedTcpTable`、`GetExtendedUdpTable`（含 `AF_INET6`）
- 当前连接范围：IPv4 与 IPv6
- 运行平台：Windows 10 或更高版本

## 构建和运行

需要 Visual Studio 2022（含 .NET 桌面开发工作负载）或 .NET 8 SDK。

```powershell
dotnet build .\NetStatusSharp.sln -c Release
dotnet run --project .\NetStatusSharp\NetStatusSharp.csproj -c Release
```

常规构建依赖已安装的 .NET 8 Desktop Runtime。需要无需安装运行时的单文件版本时，可执行：

```powershell
dotnet publish .\NetStatusSharp\NetStatusSharp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

GitHub Release 工作流会自动生成 `win-x64` 自包含压缩包。

## 许可证

本项目采用 [Selective Freedom License (SFL) v1.0](https://github.com/bighamx/MIT-NoHuawei) 授权，详见 [LICENSE](LICENSE)。
