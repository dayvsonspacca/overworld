import QtQuick
import qs.theme

Rectangle {
    property real gap: 3

    anchors.fill: parent
    anchors.margins: -gap
    radius: Theme.itemRadius + gap
    color: "transparent"
    border.width: 2
    border.color: Theme.accentLight
    visible: parent.activeFocus
}
