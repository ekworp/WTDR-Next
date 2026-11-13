# WTDR.Next

现代版 WTDR 的第一阶段实现：面向 Windows 的软件包扫描与执行器，使用 C#、.NET 11、WinUI 3 和 Windows App SDK。

## 当前功能

- WPF 深色仪表盘界面
- 扫描程序目录下的 `Pack` 文件夹
- 读取 `@@Pack@@/Config.ini`
- 支持 `Pack.Before`、`Pack.After` 中的 `R|程序|参数` 任务
- 支持 `RunTiming` 的部署环境/桌面环境匹配
- 实时日志、错误提示和软件包应用状态

## 软件包格式（MVP）

当前 `.Pack` 文件使用 ZIP 内容格式，结构如下：

```text
MyApp.Pack
├── @@Pack@@
│   └── Config.ini
└── files...
```

示例配置：

```ini
[Pack]
ConfigVer=2
Title=我的软件包
Apply_Path=%ProgramData%\MyApp
RunTiming=1

[Pack.Before]
Task1=R|Setup.exe|/silent

[Pack.After]
Task1=
```

> `.7z` 扩展名会被识别，但当前版本暂不内置 7-Zip 解压；后续将增加原生 7z 支持或外部工具配置。

## 编译

需要安装 **.NET 11 SDK**（不是只有 Runtime）：

```powershell
dotnet build WTDR.Next.sln
dotnet run --project src/WTDR.Next
```

本目录不包含旧的 WTDR 文档项目；附件 JSON 仅作为需求参考保留。
