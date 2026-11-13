# WTDR.Next agent notes

## Toolchain

- This is a Windows-only WinUI 3 desktop app: .NET 11, Windows App SDK `2.6.5`, target `net11.0-windows10.0.19041.0`.
- The project is not WPF; UI entrypoints are `src/WTDR.Next/MainWindow.xaml` and `DeploymentWindow.xaml`.
- `NuGet.Config` intentionally uses only `https://api.nuget.org/v3/index.json`. If restore hangs in this environment, set `HTTP_PROXY` and `HTTPS_PROXY` (for example `http://127.0.0.1:7890`) for the command.

## Build and run

From the repository root:

```powershell
dotnet restore WTDR.Next.sln --configfile NuGet.Config
dotnet build WTDR.Next.sln --no-restore
dotnet run --project src/WTDR.Next
```

- There are currently no test, lint, or CI commands; a successful build is the focused verification step.
- The application manifest requests `requireAdministrator`; launching the app can trigger UAC.
- Stop any running `WTDR.Next` process before rebuilding, because the executable is locked while running.
- Runtime modes are selected by arguments: no argument opens the desktop console, `/Deploy` runs deployment stages, `/FirstDesktop` runs first-desktop stages, and `/Update` runs OS-update stages.

## Runtime layout and architecture

- `MainWindow` is the interactive desktop console; `DeploymentWindow` is the compact deployment UI.
- `Services/WtdrConfigurationLoader` and `TaskEngine` implement WTDR-style INI stages and `R|program|args` / `C|command` tasks.
- Deployment mode expects `WTDR_Config.ini` beside the executable and packs under `WTDR.Pack\Pack`; `.7z` packages require `7z.exe` beside the executable or on `PATH`.
- `SystemOperationsService` performs privileged hostname, account, UAC, Defender, RDP, and wallpaper operations from configuration; do not silently change these operations into unconditional behavior.
- `ImageService` wraps DISM. Use `/Apply-Image` for ESD files; direct ESD mounting is not reliable on the target Windows DISM version.
- Logs and deployment state are written below `%ProgramData%\WTDR.Next`.

## Repository hygiene

- `bin/`, `obj/`, IDE state, and package artifacts are ignored; do not commit them.
- The attached session JSON at the repository root is an untracked requirement artifact and must not be added to commits.
