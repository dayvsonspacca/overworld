import Quickshell
import qs.capture
import qs.display
import qs.lock
import qs.taskbar
import qs.wallpaper

ShellRoot {
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
}
