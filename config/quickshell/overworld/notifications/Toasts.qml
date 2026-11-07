import Quickshell
import Quickshell.Wayland
import QtQuick
import qs.theme

PanelWindow {
    id: root

    required property PanelWindow bar
    property bool suppressed
    readonly property var asking: Bell.popups.slice(-Theme.toastLimit)
    readonly property int room: Theme.toastLimit - asking.length
    readonly property var shown: room > 0 ? Notifications.popups.slice(-room) : []

    screen: bar.screen
    visible: !suppressed && shown.length + asking.length > 0
    implicitWidth: Theme.toastWidth
    implicitHeight: Math.max(1, stack.implicitHeight)
    color: "transparent"
    exclusiveZone: 0
    WlrLayershell.namespace: "overworld:notifications"
    WlrLayershell.layer: WlrLayer.Overlay
    WlrLayershell.keyboardFocus: asking.length > 0 ? WlrKeyboardFocus.OnDemand : WlrKeyboardFocus.None

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

        Repeater {
            model: ScriptModel {
                values: root.asking
            }

            BellToast {
                required property var modelData

                width: stack.width
                notice: modelData.notice
                said: modelData.said
            }
        }
    }
}
