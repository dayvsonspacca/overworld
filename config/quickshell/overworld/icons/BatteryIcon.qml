import QtQuick
import qs.theme

Glyph {
    id: root

    property real level
    property bool charging
    property bool low
    readonly property real unit: size / 16

    path: charging ? Glyphs.charging : Glyphs.battery
    color: low ? Theme.danger : Theme.text

    Rectangle {
        x: 3 * root.unit
        y: 6.5 * root.unit
        width: Math.max(root.unit, 8.5 * root.unit * root.level)
        height: 3 * root.unit
        radius: root.unit / 2
        color: root.color
        visible: !root.charging
    }
}
