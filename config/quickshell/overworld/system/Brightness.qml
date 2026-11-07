pragma Singleton

import Quickshell
import Quickshell.Io
import QtQuick

Singleton {
    id: root

    property string device
    readonly property bool available: device !== ""
    readonly property real max: Number(maxFile.text()) || 1
    readonly property real level: Math.max(0, Math.min(1, Number(current.text()) / max))

    signal adjusted

    Process {
        command: ["sh", "-c", "ls /sys/class/backlight | head -n 1"]
        running: true

        stdout: StdioCollector {
            onStreamFinished: root.device = text.trim()
        }
    }

    FileView {
        id: maxFile

        path: root.available ? `/sys/class/backlight/${root.device}/max_brightness` : ""
    }

    FileView {
        id: current

        path: root.available ? `/sys/class/backlight/${root.device}/brightness` : ""
        watchChanges: true
        onFileChanged: {
            reload();
            root.adjusted();
        }
    }
}
