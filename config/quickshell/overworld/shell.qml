import Quickshell
import Quickshell.Services.SystemTray
import qs.capture
import qs.display
import qs.lock
import qs.taskbar
import qs.theme
import qs.wallpaper

ShellRoot {
    readonly property var tray: SystemTray.items

    Variants {
        model: Quickshell.screens

        Wallpaper {}
    }

    Variants {
        model: Displays.primary ? [Displays.primary] : []

        Taskbar {}
    }

    LockScreen {}

    CaptureLayer {}

    Borders {}
}
