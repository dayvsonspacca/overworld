# Overworld

The user's own Linux desktop environment, its dotfiles and the C# behind it, in one repository.

## Parts

Structural parts are named after Minecraft blocks and mechanics, apps after items that are ordinary words.

- **Bedrock** (`src/Overworld.Bedrock*`): the foundation every app is built on, depending on nothing else in Overworld. It gives apps their operations, results and errors, one JSON shape, and the ways they are reached: a window in the shared theme, a CLI and MCP.
- **Chest** (`src/Overworld.Chest`): storage, the JSON files where Overworld keeps its data and the folder they live in.
- **Dye** (`apps/Dye`): an app for saving color palettes and seeing them as swatches.
- **The daemon** (`src/Overworld`, binary `overworld`): the environment's own process. Hyprland starts it; it keeps the shell running and answers the environment's operations, which people and agents reach as `overworld <operation>` and `overworld mcp`.
- **Spawner** (`Overworld.Spawner`): starts and supervises processes, Quickshell first.
- **Redstone** (`Overworld.Redstone`): carries operations between processes, from the shell, the CLI and agents to the daemon.
- **Pins** (`Overworld.Pins`): the apps pinned to the start menu and the taskbar.
- **The shell** (`config/quickshell/overworld`): the desktop itself, in QML: wallpaper, taskbar, start menu, control center, notifications, lock screen and captures.
- **Hyprland config** (`config/hypr`): the compositor's setup in Lua, with the binds that reach the shell.

Names floated for later, to use only when real code needs them: Amethyst (the shared theme for the shell and apps), Seed (installing the environment on a machine).

## Stack

- .NET / C#
- Avalonia
- Quickshell (QML)
- Hyprland, with a Lua config
- zsh

## Commands

- `just build`
- `just test`
