build:
    dotnet build
    cargo build --release --manifest-path src/Overworld.Glass/Cargo.toml

test:
    dotnet test

# Runs an app from its sources with its page live from Vite, like `just dev crafter`: saving a page reloads it.
dev app:
    OVERWORLD_WEB_DEV={{justfile_directory()}}/apps/{{capitalize(app)}}/Overworld.Apps.{{capitalize(app)}}.Desktop/web dotnet run --project apps/{{capitalize(app)}}/Overworld.Apps.{{capitalize(app)}}.Desktop -p:OverworldDev=true

# Installs Overworld on this machine: links its config, publishes the daemon and the apps, and puts them on the PATH.
# Set OVERWORLD_BIN or OVERWORLD_APPS to keep the daemon or the apps somewhere else.
seed:
    dotnet run --project src/Overworld.Seed -- install
