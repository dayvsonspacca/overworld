# Overworld

The user's own Linux desktop environment, its dotfiles and the C# behind it, in one repository.

## Parts

Structural parts are named after Minecraft blocks and mechanics, apps after items that are ordinary words.

- **Bedrock** (`src/Overworld.Bedrock`): the core every part is built on, depending on nothing else in Overworld: operations and their catalog, results and errors, one JSON shape.
- **Chest** (`src/Overworld.Chest`): storage, the JSON files where Overworld keeps its data and the folder they live in.
- **Allay** (`src/Overworld.Allay`): agents anywhere in Overworld. What a conversation is in Overworld's words (prompts; replies made of moments, speech, thoughts, a plan, acts and their results, notices, requests; one outcome; the decisions a person hands back), what an agent and its sessions are, the brief an app hands a session once (context, rules, skills, tool servers) and rebriefs as a difference, the rules a reply keeps, and which agents are hired with their mode, model and effort, in Allay's Chest folder.
- **Claude Code** (`src/Overworld.Allay.ClaudeCode`): the first concrete agent, translating Claude Code's stream into Allay's moments.
- **The ways in** (`src/Overworld.Cli`, `src/Overworld.Mcp`, `src/Overworld.Desktop`, `src/Overworld.Hosting`): how people and agents reach operations: a CLI (operations reached by their words, like `overworld pins add` for `pins-add`), MCP (stdio, and an HTTP host that serves tools to agent sessions), a window in the shared theme, and the entry point that picks one. Desktop holds what apps share: the title bar every app window draws (an app can title the window and add its own buttons to it), icon, chip and tab buttons, text and combo boxes, a modal, a status bar, text selectable across blocks, a command palette with every app's shortcuts (Ctrl+Shift+P, Ctrl+/), and wearing a newly applied theme's colors as soon as the beacon signals it.
- **Themes** (`config/overworld/themes`): the colors, type sizes, radii and spacing the shell and the apps share, one JSON file per theme, Amethyst first. Seed links the folder as `~/.config/overworld/themes` and the theme worn as `~/.config/overworld/theme.json`, the only file either side reads; Dye applies another one.
- **Dye** (`apps/Dye`): an app for saving color palettes and seeing them as swatches, and for Overworld's themes: its window shows each theme in an app and the shell with its colors by role and applies one; new themes are saved only by operation (`overworld:dye themes list | show | save | apply`).
- **Crafter** (`apps/Crafter`): the user's AI-coding app. Its logic holds projects, axioms, summaries, todos, preferences, the skills library, chats, prompts built from what was typed, and a workshop that opens a chat's Allay session briefed with the project and hands it Crafter's tools (todos, propose-axiom, propose-summary). Its window has a project rail (with adding a project and the preferences page), the project's chats, the conversation as a timeline with a message box (mode, model and effort chips, @files, /skills, pictures, queue, stop), a page for each project (name, icon, summary, skills, removal) and an aside with todos, skills, axioms and the chat's background jobs.
- **The daemon** (`src/Overworld.Daemon`, binary `overworld`): the environment's own process. Hyprland starts it; it keeps the shell running, shines the beacon and answers the environment's operations, which people and agents reach as `overworld <operation>` and `overworld mcp`, among them pins, agents (`agents-list`, `agents-check`, `agents-hire`, `agents-configure`) and `beacon-light`.
- **Seed** (`src/Overworld.Seed`, `overworld:seed`): installs Overworld on an Arch machine: installs the packages it needs (`src/Overworld.Seed/packages.json`, the AUR ones built with makepkg), links its config, themes and the theme worn (keeping the one worn unless another is chosen), publishes the daemon and the apps (Dye and Crafter, each with a launcher), and puts them on the PATH.
- **Spawner** (`Overworld.Daemon.Spawner`): starts and supervises processes, Quickshell first.
- **Redstone** (`src/Overworld.Redstone`): carries operations between processes, from the shell, the CLI and agents to the daemon.
- **Beacon** (`src/Overworld.Beacon`): signals what changed across Overworld so whoever cares reacts. The daemon shines it on its own socket (`overworld-beacon.sock`, a line of JSON per signal); anyone lights a signal with `overworld beacon light --signal <name>` or the beacon client, and the shell and the apps watch it. Signals so far: `pins` and `theme`.
- **Pins** (`Overworld.Daemon.Pins`): the apps pinned to the start menu and the taskbar; changing them lights `pins`.
- **The shell** (`config/quickshell/overworld`): the desktop itself, in QML: wallpaper, taskbar, start menu, control center, notifications, lock screen and captures; it rereads the pins and the theme when the beacon signals them.
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
- On a new Arch machine: `curl -fsSL https://raw.githubusercontent.com/dayvsonspacca/overworld/main/scripts/install.sh | bash` (installs what Seed needs to run, clones the repository and runs Seed)
