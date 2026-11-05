# Overworld

The user's own Linux desktop environment, and the dotfiles and C# code behind it, in one repository. It runs on Hyprland: a C# daemon starts and supervises Quickshell, which draws the shell (taskbar, lock screen, notifications, captures) from QML. Apps built for it share one foundation, Bedrock, so each app only cares about what it is for.

Every app serves two kinds of users: the person, by hand, and agents and programs, from outside. Today an app is reached through a window, a CLI (`overworld:<app>`) and MCP, all calling the same code.

## Merge in progress

Overworld is being built by merging two older repositories, one commit at a time, never as a bulk copy. Their history is not carried over.

- **Amethyst** (`~/Projetos/me/amethyst`): the environment. The daemon in `src/Amethyst` (supervision, processes, IPC, CLI, pins, XDG paths, logging), the QML shell in `shell/`, the Hyprland Lua config in `hypr/`. Amethyst's daemon becomes Overworld's.
- **Bedrock** (`~/Projetos/me/desktop/bedrock`): the foundation for apps (`Bedrock.Core`, `.Cli`, `.Mcp`, `.Desktop`, with the Avalonia theme) and the first app, Dye, a color palette app.

Bedrock has arrived whole: `Overworld.Bedrock` (results, operations, JSON, app identity), `.Cli`, `.Mcp`, `.Desktop` (window, theme, `TextField`, buttons, `Sidebar`, `HexBrushConverter`) and `.Hosting`, whose `AppHost` (Bedrock's `OverworldApp`) opens the window, the CLI or MCP depending on the arguments. So has Dye: `Overworld.Apps.Dye` (colors and palettes with the save, list and show operations), `.Presentation` (view models) and `.Desktop` (the `dye` executable: `overworld:dye`, `overworld:dye mcp`, and bare, a window of swatches with a form that saves palettes). Bringing Bedrock's code under Amethyst's analyzers took a few fixes: `Result<T>`'s factories moved to a `Result` class (CA1000), CA1716 is off since `Error` is the clearest name, and the CLI compares `--` ordinally (CA1310).

Amethyst is coming in, in the order of its history. Everything named `amethyst` at runtime becomes `overworld`: the binary, the socket `overworld.sock`, `OVERWORLD_SHELL`, the layer namespaces `overworld:*`, the `overworld-locked` file and the `x-overworld-preview` hint. The shell moves to `config/quickshell/overworld/` (installed as `~/.config/quickshell/overworld`), pins move to `~/.local/share/overworld/` on Chest, and `hyprland.local.lua` lives untracked in `config/hypr/`. Minecraft terms and visuals Amethyst shows in the UI (its amethyst-shard logo, the "Amethyst" notification name) come in as they are; the user reworks them later. The running session still uses Amethyst's repository until the user switches over at the end.

So far the daemon core has arrived as `src/Overworld` (binary `overworld`): `Lifetime` (`ShutdownSignal`, `OverworldException` with exit codes), `Logging` (`HH:mm:ss LVL Category message` on stderr), `Spawner` (launching processes, SIGTERM then kill, and `QuickshellSupervisor` with a 3-crashes-in-30-s `RestartPolicy`), `Shell` (`ShellLocator`: `$OVERWORLD_SHELL` or `$XDG_CONFIG_HOME/quickshell/overworld`), `Desktop` (the user's icon theme from gsettings, handed to Quickshell as `QS_ICON_THEME` unless already set) and `Paths` (`XdgDirectories`). Exit codes: 1 shell not found, 2 crash loop, 127 Quickshell missing. The Hyprland config is in `config/hypr/` (`hyprland.lua` loading `modules/*.lua`, then the untracked `hyprland.local.lua`), autostarting `overworld`; binds and rules that drive the shell arrive with the shell features they reach. `.luarc.json` at the root points the Lua language server at Hyprland's `hl` stubs.

## Layout

- `config/`: dotfiles, mirroring `~/.config` so each folder installs as one symlink (`hypr/`, `quickshell/overworld/` for the QML shell, `zsh/` via `ZDOTDIR`, ...). Machine-only settings live in untracked files.
- `src/`: C# for the foundation and the environment (`Overworld.Bedrock*`, `Overworld`); `tests/` mirrors it.
- Every project and namespace starts with `Overworld.`, apps with `Overworld.Apps.` (`Overworld.Bedrock.Results`, `Overworld.Apps.Dye`). Bedrock's old `Bedrock.Core` is now `Overworld.Bedrock`.
- `apps/`: one folder per app, holding its own projects and tests.
- Root: `Overworld.slnx`, `Directory.Build.props` (warnings as errors, latest-recommended analyzers, test conventions), `Directory.Packages.props` (central versions), `global.json`, `Directory.Build.rsp`, `Justfile`.

Folders appear with their first content.

## Parts

Structural parts are named after Minecraft blocks and mechanics, apps after items that are ordinary words; the UI never shows the theme. Bedrock is the foundation and depends on nothing else. Chest (`Overworld.Bedrock.Chest`) is Bedrock's storage protocol: `JsonFile<T>` reads a whole JSON document (missing or null means empty, unreadable comes back as a `file.unreadable` Error) and writes it atomically through a `.tmp` file; each part says only where the file lives and what empty is. Where data lives stays with each app (`AppIdentity.DataDirectory`), and locking stays with the caller. Spawner (`Overworld.Spawner`) starts and supervises processes, Quickshell first. Redstone (`Overworld.Redstone`) carries signals between processes: the daemon listens on `$XDG_RUNTIME_DIR/overworld.sock`, one line of JSON each way, `{"operation":"ping","input":{}}` answered by `{"result":...}` or `{"error":{"code","message"}}`. What it serves are Bedrock operations, so the environment's commands get the same typing, errors and JSON as any app's; a second daemon finds the socket answering and exits with 3. `overworld` with arguments is the environment's CLI (`overworld ping`, `overworld --help`) and `overworld mcp` its MCP server: Bedrock's `CliHost` and `McpHost` over `RemoteOperation`s, which keep each operation's name, description and input type but send it to the daemon; a daemon that is not running answers `daemon.not_running`. Pins (`Overworld.Pins`) are the first real operations: `pins-list`, `pins-add` and `pins-remove` take a `--place` (`menu` or `taskbar`) and an `--app` (a desktop entry id) and answer the place's list; each place is a Chest file in `~/.local/share/overworld/pins/` (`menu.json`, `taskbar.json`, the same string lists as Amethyst's `pins.json` and `taskbar.json`). The daemon's tests live in `tests/Overworld.Tests`.

The shell comes in one feature per commit, in the order the taskbar needs them: wallpaper, lock screen, captures, taskbar, start menu, notifications, control center. So far: the wallpaper on every screen (faceted backdrop in the theme colours, or `~/.config/overworld/wallpaper.*` when present) and the lock screen (Super+L, or `qs -p <shell> ipc call lock lock`; a restarted shell can take over a held lock) and captures (Print or Super+Shift+S for region, window or screen; Shift+Print for the whole screen) and the taskbar on the primary screen (open windows grouped by app, previews on hover, a menu to pin or close; pinned apps come from the daemon's `pins-*` operations through `daemon/Daemon.qml`, which speaks Redstone). Hyprland binds reach the shell through `qs -p <shell> ipc call`, with the shell found like the daemon finds it. Names already floated for later, to use only when real code needs them: Amethyst (the shared theme for the shell and apps), Seed (installing the environment on a machine).

## Stack

.NET 10 / C#, Avalonia for app windows, Quickshell (QML), Hyprland with a Lua config, zsh.

## Commands

- `just build`
- `just test`
- `just install-dye`: publishes Dye into `$OVERWORLD_APPS` (default `~/.local/share/overworld/apps`), adds it to the launcher and links `overworld:dye` into `~/.local/bin`. The launcher icon is the untracked `apps/Dye/icon.png` when present, `dye.svg` otherwise.
