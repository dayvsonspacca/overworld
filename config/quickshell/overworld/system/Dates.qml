pragma Singleton

import Quickshell
import QtQuick

Singleton {
    readonly property string dayFormat: Qt.locale().dateFormat(Locale.LongFormat)
        .replace(/^y+[^dM]*?(?=[dM])/, "")
        .replace(/[\s,.]*('[^']*'\s*)?y+$/, "")
        .trim()
    readonly property string timeFormat: Qt.locale().timeFormat(Locale.LongFormat)
        .replace(/\s*\(?t+\)?/g, "")
        .trim()
}
