import Quickshell
import QtQuick
import qs.daemon

// Hyprland draws window borders in the theme's colors, read when its config loads; a new theme reloads it.
Connections {
    target: Beacon

    function onLit(name: string): void {
        if (name === "theme")
            Quickshell.execDetached(["hyprctl", "reload"]);
    }
}
