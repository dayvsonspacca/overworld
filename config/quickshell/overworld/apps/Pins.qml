pragma Singleton

import Quickshell
import QtQuick

Singleton {
    readonly property PinList menu: PinList {
        place: "menu"
    }

    readonly property PinList bar: PinList {
        place: "taskbar"
    }
}
