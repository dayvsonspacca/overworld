import Quickshell
import Quickshell.Wayland
import QtQuick
import qs.theme

PanelWindow {
    id: root

    required property ShellScreen modelData

    screen: modelData
    color: Theme.background
    exclusionMode: ExclusionMode.Ignore
    WlrLayershell.layer: WlrLayer.Background
    WlrLayershell.namespace: "overworld:wallpaper"

    anchors {
        top: true
        bottom: true
        left: true
        right: true
    }

    Backdrop {
        anchors.fill: parent
    }
}
