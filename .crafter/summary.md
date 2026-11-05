# Overworld

The user's own Linux desktop environment, its dotfiles and the C# behind it, in one repository. Hyprland starts the `overworld` daemon, which supervises Quickshell, which draws the shell from QML. Apps share one foundation, Bedrock, so each app only cares about what it is for.

Everything serves two kinds of users: the person, by hand, and agents and programs, from outside. Apps and the environment are reached through a window or the shell, a CLI and MCP, all calling the same operations.

Overworld was built by merging two older repositories, Amethyst (the environment) and Bedrock (the app foundation, with Dye). Both are gone; this repository is the only record.

## Layout

- `src/`: Bedrock (`Overworld.Bedrock*`) and the daemon (`Overworld`); `tests/` mirrors it.
- `apps/<App>/`: each app's projects and tests.
- `config/`: dotfiles mirroring `~/.config`, each folder installed as one symlink: `hypr/`, `quickshell/overworld/` (the shell). Machine-only settings live in untracked files, like `config/hypr/hyprland.local.lua`.
- Root: `Overworld.slnx`, `Directory.Build.props` (warnings as errors, latest-recommended analyzers, CA1716 off, test conventions), `Directory.Packages.props` (central versions), `global.json`, `Directory.Build.rsp`, `Justfile`, `.luarc.json` (points the Lua language server at Hyprland's `hl` stubs).
- Every project and namespace starts with `Overworld.`, apps with `Overworld.Apps.`; folders match namespaces. Folders appear with their first content.

## Parts

Structural parts are named after Minecraft blocks and mechanics, apps after items that are ordinary words.

### Bedrock (`src/Overworld.Bedrock*`)

The foundation; it depends on nothing else in Overworld.

- `Overworld.Bedrock`:
  - `Results`: `Error(Code, Message)` and `Result<T>`, made with `Result.Success`/`Result.Failure` or implicitly from a value or an `Error`. Expected failures are values; exceptions are for bugs.
  - `Operations`: an `IOperation<TInput, TOutput>` with `[Operation(name, description)]` (kebab-case name) becomes an `Operation` through `Operation.From`; one operation runs at a time. `Operation`'s constructor is protected, so an operation can execute somewhere else.
  - `Json`: `BedrockJson`, the one JSON shape (camelCase, indented, nullability and required members enforced, read-only); a bad input is an `input.invalid` Error.
  - `Apps`: `AppIdentity`, giving an app its command `overworld:<name>` and data folder `~/.local/share/overworld/<name>`.
  - Chest (`Chest`): `JsonFile<T>` reads a whole JSON file (missing or null means the empty value, unreadable is a `file.unreadable` Error) and writes it atomically through a `.tmp` file. Callers decide where files live and do their own locking.
- `.Cli`: `CliHost` turns operations into subcommands with kebab-case options and `--help`, prints results as JSON and errors as `code: message`; exit 0 done, 1 failed, 2 misused.
- `.Mcp`: `McpHost` serves operations as MCP tools over stdio.
- `.Desktop`: the Avalonia window and the one Overworld theme (palette, tokens, `TextField`, primary and ghost buttons, `Sidebar`, `HexBrushConverter`).
- `.Hosting`: `AppHost.RunAsync` opens the window when started bare, MCP with `mcp`, and the CLI otherwise.

### Apps

- Dye (`apps/Dye`), color palettes: `Overworld.Apps.Dye` (`Colors`, and `Palettes` with `save`, `list` and `show`, kept in a Chest file), `.Presentation` (view models on CommunityToolkit.Mvvm) and `.Desktop` (the `dye` executable and its window). Reached as `overworld:dye`, `overworld:dye mcp`, or bare for the window.

### The daemon (`src/Overworld`, binary `overworld`)

- Bare, `overworld` is the daemon. With arguments it is the environment's CLI (`overworld ping`, `overworld pins-add ...`), and `overworld mcp` is its MCP server. Operations are declared once in `Program.Operations()`: the daemon runs them, and the client wraps them in `RemoteOperation`s that go over Redstone.
- Spawner (`Spawner`): launches processes (SIGTERM, then kill) and `QuickshellSupervisor`, which runs `qs -p <shell>` and restarts it after a crash, giving up after 3 in 30 s. It hands Quickshell the user's gsettings icon theme as `QS_ICON_THEME` unless one is set.
- Redstone (`Redstone`): the daemon listens on `$XDG_RUNTIME_DIR/overworld.sock`, one line of JSON each way: `{"operation","input"}` answered by `{"result"}` or `{"error":{"code","message"}}`. Its errors are `request.invalid`, `operation.unknown` and `operation.crashed`; the client adds `daemon.not_running` and `daemon.invalid_response`.
- `Pins`: `pins-list`, `pins-add` and `pins-remove` take `--place menu|taskbar` and `--app <desktop entry id>` and answer that place's list, kept in `~/.local/share/overworld/pins/menu.json` and `taskbar.json`.
- Also `Shell` (`ShellLocator`: `$OVERWORLD_SHELL` or `$XDG_CONFIG_HOME/quickshell/overworld`), `Desktop` (the icon theme), `Daemon`, `Lifetime`, `Logging` (`HH:mm:ss LVL Category message` on stderr) and `Paths`.
- Exit codes: 1 no shell, 2 crash loop, 3 already running, 127 no `qs`.

### The shell (`config/quickshell/overworld`)

Quickshell QML: a wallpaper on every screen (or `~/.config/overworld/wallpaper.*`), a taskbar on the primary screen (windows grouped by app, previews, pinned apps), a start menu, a control center (network, night light, sound, media, notices, calendar, tray), the notification server with toasts, a lock screen, and captures. It reaches the daemon only through `daemon/Daemon.qml`, which speaks Redstone. Its layers are named `overworld:*`, and Hyprland reaches it with `qs -p <shell> ipc call <target> <function>`.

### Hyprland (`config/hypr`)

Lua config: `hyprland.lua` loads `modules/*.lua`, then the untracked `hyprland.local.lua`, and autostarts `overworld`. Binds: Super for the menu, Super+A for the control center, Super+L to lock, Print, Super+Shift+S and Shift+Print for captures, Alt+Tab to switch windows in recently-used order. `Hyprland --verify-config` crashes on the switcher's timer; that is a limit of verify mode, not a config error.

Names floated for later, to use only when real code needs them: Amethyst (the shared theme for the shell and apps), Seed (installing the environment on a machine).

## Known gaps

- `AppHost` has no tests, and `file.unreadable` messages show .NET type names.
- Under the Lua config, `hyprctl dispatch exit` fails; `loginctl terminate-session` logs out.

## Stack

.NET 10 / C#, Avalonia 12, Quickshell (QML), Hyprland 0.57 with a Lua config, zsh.

## Commands

- `just build`, `just test`
- `just install-overworld`: publishes the daemon and CLI into `$OVERWORLD_BIN` (default `~/.local/share/overworld/bin`) and links `overworld` into `~/.local/bin`, where Hyprland's autostart finds it.
- `just install-dye`: publishes Dye into `$OVERWORLD_APPS` (default `~/.local/share/overworld/apps`), adds it to the launcher and links `overworld:dye` into `~/.local/bin`; the launcher icon is the untracked `apps/Dye/icon.png` when present, `dye.svg` otherwise.
- On this machine, `~/.config/hypr` links to `config/hypr` and `~/.config/quickshell/overworld` to `config/quickshell/overworld`.
