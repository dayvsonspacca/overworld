import Quickshell
import Quickshell.Hyprland
import Quickshell.Io
import Quickshell.Wayland
import QtQuick
import qs.session
import qs.theme

Scope {
    IpcHandler {
        target: "lock"

        function lock(): void {
            Locker.lock();
        }
    }

    WlSessionLock {
        locked: Locker.locked

        WlSessionLockSurface {
            id: surface

            color: Theme.background

            LockView {
                anchors.fill: parent
                active: Hyprland.focusedMonitor?.name === surface.screen?.name
                shown: !Locker.unlocking
                duration: Locker.transition
                snapshot: surface.screen ? Locker.snapshot(surface.screen) : ""
                user: Locker.user
                busy: Locker.busy
                error: Locker.error
                onSubmitted: password => Locker.unlock(password)
            }
        }
    }
}
