# Overworld

The user's own Linux desktop environment, and the dotfiles and C# code behind it, in one repository. It runs on Hyprland: a C# daemon starts and supervises Quickshell, which draws the shell (taskbar, lock screen, notifications, captures) from QML. Apps built for it share one foundation, Bedrock, so each app only cares about what it is for.

Every app serves two kinds of users: the person, by hand, and agents and programs, from outside. Today an app is reached through a window, a CLI (`overworld:<app>`) and MCP, all calling the same code.

## Merge in progress

Overworld is being built by merging two older repositories, one commit at a time, never as a bulk copy. Their history is not carried over.

- **Amethyst** (`~/Projetos/me/amethyst`): the environment. The daemon in `src/Amethyst` (supervision, processes, IPC, CLI, pins, XDG paths, logging), the QML shell in `shell/`, the Hyprland Lua config in `hypr/`. Amethyst's daemon becomes Overworld's.
- **Bedrock** (`~/Projetos/me/desktop/bedrock`): the foundation for apps (`Bedrock.Core`, `.Cli`, `.Mcp`, `.Desktop`, with the Avalonia theme) and the first app, Dye, a color palette app.

So far the root build setup and all of Bedrock's foundation have arrived: `Overworld.Bedrock` (results, operations, JSON, app identity), `.Cli`, `.Mcp`, `.Desktop` (window, theme, `TextField`, buttons, `Sidebar`) and `.Hosting`, whose `AppHost` (Bedrock's `OverworldApp`) opens the window, the CLI or MCP depending on the arguments. Dye is coming in as `Overworld.Apps.Dye` (its logic, in `.Colors` and `.Palettes`), `.Presentation` (view models) and `.Desktop` (the `dye` executable and its window); so far its colors. Its `HexBrushConverter` comes into Bedrock's `.Desktop` with the window. Bedrock's code has never been built with Amethyst's analyzers; expect fixes as it comes in (so far: `Result<T>` lost its static factories to a `Result` class for CA1000, and the CLI compares `--` ordinally for CA1310).

## Layout

- `config/`: dotfiles, mirroring `~/.config` so each folder installs as one symlink (`hypr/`, `zsh/` via `ZDOTDIR`, ...). Machine-only settings live in untracked files.
- `shell/`: the Quickshell QML.
- `src/`: C# for the foundation and the environment (`Overworld.Bedrock*`, `Overworld`); `tests/` mirrors it.
- Every project and namespace starts with `Overworld.`, apps with `Overworld.Apps.` (`Overworld.Bedrock.Results`, `Overworld.Apps.Dye`). Bedrock's old `Bedrock.Core` is now `Overworld.Bedrock`.
- `apps/`: one folder per app, holding its own projects and tests.
- Root: `Overworld.slnx`, `Directory.Build.props` (warnings as errors, latest-recommended analyzers, test conventions), `Directory.Packages.props` (central versions), `global.json`, `Directory.Build.rsp`, `Justfile`.

Folders appear with their first content.

## Parts

Structural parts are named after Minecraft blocks and mechanics, apps after items that are ordinary words; the UI never shows the theme. Bedrock is the foundation and depends on nothing else. Names already floated for later, to use only when real code needs them: Redstone (signals between processes), Spawner (starting and supervising processes), Amethyst (the shared theme for the shell and apps), Chest (stored user data), Seed (installing the environment on a machine).

## Stack

.NET 10 / C#, Avalonia for app windows, Quickshell (QML), Hyprland with a Lua config, zsh.

## Commands

- `just build`
- `just test`
