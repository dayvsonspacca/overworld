import Quickshell
import Quickshell.Wayland
import QtQuick
import qs.theme

PanelWindow {
    id: root

    required property PanelWindow bar
    property bool suppressed
    readonly property var shown: Notifications.popups.slice(-Theme.toastLimit)

    screen: bar.screen
    visible: !suppressed && shown.length > 0
    implicitWidth: Theme.toastWidth
    implicitHeight: Math.max(1, stack.implicitHeight)
    color: "transparent"
    exclusiveZone: 0
    WlrLayershell.namespace: "overworld:notifications"
    WlrLayershell.layer: WlrLayer.Overlay

    anchors {
        right: true
        bottom: true
    }

    margins {
        right: Theme.gap
        bottom: Theme.gap
    }

    Column {
        id: stack

        width: parent.width
        spacing: 8

        Repeater {
            model: root.shown

            Toast {
                required property var modelData

                width: stack.width
                notification: modelData
            }
        }
    }
}
