pragma Singleton

import Quickshell
import Quickshell.Io
import QtQuick

Singleton {
    id: root

    property bool available
    property bool enabled

    Process {
        command: ["sh", "-c", "command -v wlsunset"]
        running: true
        onExited: code => root.available = code === 0
    }

    Process {
        command: ["wlsunset", "-t", "4000", "-T", "4001", "-S", "06:00", "-s", "18:00"]
        running: root.available && root.enabled
    }
}
