#!/usr/bin/env bash
# Installs Overworld on an Arch machine: clones the repository and lets Seed do the rest.
#   curl -fsSL https://raw.githubusercontent.com/dayvsonspacca/overworld/main/scripts/install.sh | bash
# Set OVERWORLD_REPO to clone it somewhere else. Seed links the config into the clone, so keep it.
set -euo pipefail

main() {
    local repo="${OVERWORLD_REPO:-$HOME/Projetos/me/overworld}"

    if ! command -v pacman >/dev/null; then
        echo "Overworld installs on Arch Linux only." >&2
        exit 1
    fi

    sudo pacman -S --needed --noconfirm dotnet-sdk just git

    if [ -d "$repo/.git" ]; then
        git -C "$repo" pull --ff-only
    else
        git clone https://github.com/dayvsonspacca/overworld.git "$repo"
    fi

    cd "$repo"
    just seed

    echo "Overworld is installed. Log out and pick Hyprland at the login screen."
}

main "$@"
