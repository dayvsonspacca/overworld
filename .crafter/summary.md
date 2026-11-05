# Overworld

The user's own Linux desktop environment, its dotfiles and the C# behind it, in one repository.

## Parts

Structural parts are named after Minecraft blocks and mechanics, apps after items that are ordinary words.

- **Bedrock** (`src/Overworld.Bedrock`): the core every part is built on, depending on nothing else in Overworld: operations and their catalog, results and errors, one JSON shape.
- **Chest** (`src/Overworld.Chest`): storage, the JSON files where Overworld keeps its data and the folder they live in.
- **The ways in** (`src/Overworld.Cli`, `src/Overworld.Mcp`, `src/Overworld.Desktop`, `src/Overworld.Hosting`): how people and agents reach operations: a CLI, MCP, a window in the shared theme, and the entry point that picks one.
- **Themes** (`config/overworld/themes`): the colors, type sizes, radii and spacing the shell and the apps share, one JSON file per theme, Amethyst first. Seed installs the chosen one as `~/.config/overworld/theme.json`, the only file either side reads.
- **Dye** (`apps/Dye`): an app for saving color palettes and seeing them as swatches.
- **The daemon** (`src/Overworld.Daemon`, binary `overworld`): the environment's own process. Hyprland starts it; it keeps the shell running and answers the environment's operations, which people and agents reach as `overworld <operation>` and `overworld mcp`.
- **Seed** (`src/Overworld.Seed`, `overworld:seed`): installs Overworld on a machine: links its config, publishes the daemon and the apps, and puts them on the PATH.
- **Spawner** (`Overworld.Daemon.Spawner`): starts and supervises processes, Quickshell first.
- **Redstone** (`src/Overworld.Redstone`): carries operations between processes, from the shell, the CLI and agents to the daemon.
- **Pins** (`Overworld.Daemon.Pins`): the apps pinned to the start menu and the taskbar.
- **The shell** (`config/quickshell/overworld`): the desktop itself, in QML: wallpaper, taskbar, start menu, control center, notifications, lock screen and captures.
- **Hyprland config** (`config/hypr`): the compositor's setup in Lua, with the binds that reach the shell.


## Stack

- .NET / C#
- Avalonia
- Quickshell (QML)
- Hyprland, with a Lua config
- zsh

## Commands

- `just build`
- `just test`
- `just seed`
