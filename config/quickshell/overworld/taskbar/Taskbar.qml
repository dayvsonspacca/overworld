import Quickshell
import Quickshell.Wayland
import QtQuick
import qs.session
import qs.theme

PanelWindow {
    id: root

    required property ShellScreen modelData

    function dismiss(): void {
        preview.hide();
        tasks.close();
    }

    screen: modelData
    implicitHeight: Theme.barHeight
    color: Theme.bar
    WlrLayershell.namespace: "overworld:taskbar"

    anchors {
        left: true
        right: true
        bottom: true
    }

    Connections {
        target: Locker

        function onLocking() {
            root.dismiss();
        }
    }

    WindowPreview {
        id: preview
    }

    TaskMenu {
        id: tasks
    }

    MouseArea {
        anchors.fill: parent
        onClicked: root.dismiss()
    }

    Rectangle {
        anchors.top: parent.top
        anchors.left: parent.left
        anchors.right: parent.right
        height: 1
        color: Theme.edge
    }

    Row {
        anchors.left: parent.left
        anchors.leftMargin: Theme.barPadding
        anchors.verticalCenter: parent.verticalCenter
        spacing: Theme.tileSpacing

        Repeater {
            model: Tasks.groups

            TaskButton {
                id: button

                required property var modelData

                group: modelData
                onClicked: root.dismiss()
                onContextRequested: {
                    root.dismiss();
                    tasks.open(button);
                }
                onHoveredChanged: {
                    if (hovered && !tasks.visible)
                        preview.show(button);
                    else
                        preview.leave();
                }
            }
        }
    }
}
