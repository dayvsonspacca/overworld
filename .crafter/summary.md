# Overworld

The user's own Linux desktop environment, its dotfiles and the C# behind it, in one repository.

## Parts

Structural parts are named after Minecraft blocks and mechanics, apps after items that are ordinary words.

- **Bedrock** (`src/Overworld.Bedrock`): the core every part is built on, depending on nothing else in Overworld: operations and their catalog, results and errors, one JSON shape.
- **Chest** (`src/Overworld.Chest`): storage, the JSON files where Overworld keeps its data and the folder they live in.
- **Allay** (`src/Overworld.Allay`): agents anywhere in Overworld. What a conversation is in Overworld's words (prompts; replies made of moments, speech, thoughts, a plan, acts and their results, notices, requests; one outcome; the decisions a person hands back), what an agent and its sessions are (each agent with its own color), the brief an app hands a session once (context, rules, skills, tool servers) and rebriefs as a difference, the rules a reply keeps, which agents are hired with their mode, model and effort, in Allay's Chest folder, and the usage an agent that tells it reports (`Metering`: its limits, how much of each is used and when it resets, and what it has spent).
- **Claude Code** (`src/Overworld.Allay.ClaudeCode`): the first concrete agent, translating Claude Code's stream into Allay's moments, and reading its plan's usage from `claude -p /usage` (no tokens spent, no session kept).
- **The ways in** (`src/Overworld.Cli`, `src/Overworld.Mcp`, `src/Overworld.Desktop`, `src/Overworld.Hosting`): how people and agents reach operations: a CLI (operations reached by their words, like `overworld pins add` for `pins-add`; options that take an object read it as JSON, and ids are read as Guids), MCP (stdio, and an HTTP host that serves tools to agent sessions), a window in the shared theme, and the entry point that picks one. Desktop holds what apps share: the title bar every app window draws (an app can title the window and add its own buttons to it), icon, chip and tab buttons, text and combo boxes, a modal, a status bar, text selectable across blocks, a command palette with every app's shortcuts (Ctrl+Shift+P, Ctrl+/), an agent's icon (the official Allay recolored to the agent's color), wearing a newly applied theme's colors as soon as the beacon signals it, and the item frame (`Framing.ItemFrame`): one app's thing shown inside another, a card naming the app that lends it, the thing's title and an expand button, whose content lives only while seen (it comes alive once scrolling pauses near it and lets go a second after it leaves sight or what holds it is hidden), and over which the wheel scrolls what holds it.
- **Themes** (`config/overworld/themes`): the colors, type sizes, radii and spacing the shell and the apps share, one folder per theme with a dark and a light JSON file, Amethyst first. Seed links the folder as `~/.config/overworld/themes` and the theme worn as `~/.config/overworld/theme.json`, the only file either side reads; Dye applies another one.
- **Dye** (`apps/Dye`): an app for saving color palettes and seeing them as swatches, for Overworld's themes (its window shows each theme in an app and the shell with its colors by role and applies one; new themes are saved only by operation, `overworld:dye themes list | show | save | apply`, and saving one lights `themes`), and for the profile picture the shell shows (setting or removing it lights `profile`).
- **Allay app** (`apps/Allay`): the app to hire agents for every app, see whether each is ready, choose the mode, model and effort it starts with and see its usage; its operations are `overworld:allay agents list | check | hire | configure | usage`, and hiring, letting go or setting up lights `agents`.
- **Crafter** (`apps/Crafter`): the user's AI-coding app. Its logic holds projects, axioms, summaries, todos, preferences, the skills library, chats, prompts built from what was typed, a workshop that opens a chat's Allay session briefed with the project and hands it Crafter's tools (todos, propose-axiom, propose-summary, show-picture, and Painting's paint), and its bells (`Bells`): an agent's questions and permissions ring the Bell, answers come back as the request's decision, answering in the chat withdraws the notice, and Open brings the window and the chat forward. Its window has a project rail (with adding a project and the preferences page), the project's chats, the conversation as a timeline with a message box (mode, model and effort chips, @files, /skills, pictures, queue, stop), a page for each project (name, icon, summary, skills, removal) and an aside with todos, skills, axioms and the chat's background jobs; it stays shut behind a gate until a hired agent is ready. A painting an act painted (by the paint tool or `overworld:painting paint`) shows in the timeline under that act in an item frame, once per painting, redrawn when the beacon signals `painting` (`Paintings.Gallery`), and expands to fill the window until Esc.
- **Painting** (`apps/Painting`): the app where agents show a page of HTML, Overworld's take on artifacts. Its paintings (`Artwork`: id, title, HTML, when painted) live in its Chest folder; `overworld:painting paint` publishes one from html or a file, or changes one by its id, and lights `painting`; `show` reads one, or the one painted last. Its canvas (`Overworld.Apps.Painting.Views.ArtworkCanvas`) draws a page with WebKitGTK through Avalonia's WebView offscreen so it stays on Wayland, and other apps use it to show paintings inside themselves. Its window is the shared title bar with the painting's title and the canvas filling the rest; it shows the latest painting and redraws when the beacon signals `painting`.
- **The daemon** (`src/Overworld.Daemon`, binary `overworld`): the environment's own process. Hyprland starts it; it keeps the shell running, shines the beacon, keeps the bell's notices and answers the environment's operations, which people and agents reach as `overworld <operation>` and `overworld mcp`, among them pins, bell and `beacon-light`.
- **Seed** (`src/Overworld.Seed`, `overworld:seed`): installs Overworld on an Arch machine: installs the packages it needs (`src/Overworld.Seed/packages.json`, the AUR ones through yay or paru), links its config, themes and the theme worn (keeping the one worn unless another is chosen), publishes the daemon and the apps (Dye, Crafter, Allay and Painting, each with a launcher), and puts them on the PATH.
- **Spawner** (`Overworld.Daemon.Spawner`): starts and supervises processes, Quickshell first.
- **Redstone** (`src/Overworld.Redstone`): carries operations between processes, from the shell, the CLI and agents to the daemon.
- **Beacon** (`src/Overworld.Beacon`): signals what changed across Overworld so whoever cares reacts. The daemon shines it on its own socket (`overworld-beacon.sock`, a line of JSON per signal); anyone lights a signal with `overworld beacon light --signal <name>` or the beacon client, and the shell and the apps watch it. Signals so far: `pins`, `theme`, `themes`, `profile`, `agents`, `bell` and `painting`.
- **Bell** (`src/Overworld.Bell`): the notices Overworld dictates, rung by its apps and kept by the daemon in its Chest folder until answered or withdrawn, so they survive a restart. A notice may ask questions (choices, several or one, and words of one's own) or offer buttons (plain, primary, danger); the reply is answered, pressed or skipped. Operations: `bell-ring`, `bell-list`, `bell-answer`, `bell-open`, `bell-withdraw`, `bell-collect` (an app takes its answers and opens); `BellClient` is how an app reaches them. Freedesktop notifications from other apps still come over D-Bus.
- **Pins** (`Overworld.Daemon.Pins`): the apps pinned to the start menu and the taskbar; changing them lights `pins`.
- **The shell** (`config/quickshell/overworld`): the desktop itself, in QML: wallpaper, taskbar, start menu, control center, notifications (D-Bus ones and the bell's, answered in a toast or under Waiting on you in the control center), lock screen and captures; it rereads the pins, the theme, the profile picture and the bell when the beacon signals them. Its IPC targets (`qs ipc call menu toggle`, `control toggle`, `lock lock`, `capture open | screen`) are what the Hyprland binds call.
- **Hyprland config** (`config/hypr`): the compositor's setup in Lua, with the binds that reach the shell; an untracked `hyprland.local.lua`, loaded last, holds what belongs to one machine.

## Docs

- `README.md`: the front page, showing the desktop and its apps from the outside (no code details), with screenshots in `.github/assets/screenshots` and the CI badge.
- The GitHub wiki (`dayvsonspacca/overworld.wiki`, its own git repository): how Overworld is built, a page per part and per app, how an operation travels, building from source, the shell, the Hyprland config and the themes.
- `LICENSE`: MIT.
- `.github/workflows/ci.yml`: CI that builds and tests every push and pull request to main.

## Stack

- .NET / C#
- Avalonia (WebKitGTK through Avalonia.Controls.WebView for Painting's canvas)
- Quickshell (QML)
- Hyprland, with a Lua config
- zsh

## Commands

- `just build`
- `just test`
- `just seed`
- On a new Arch machine: `curl -fsSL https://raw.githubusercontent.com/dayvsonspacca/overworld/main/scripts/install.sh | bash` (installs what Seed needs to run, clones the repository and runs Seed)
