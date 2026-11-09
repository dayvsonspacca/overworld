build:
    dotnet build
    cargo build --release --manifest-path src/overworld-window/Cargo.toml

test:
    dotnet test

# Installs Overworld on this machine: links its config, publishes the daemon and the apps, and puts them on the PATH.
# Set OVERWORLD_BIN or OVERWORLD_APPS to keep the daemon or the apps somewhere else.
seed:
    dotnet run --project src/Overworld.Seed -- install
