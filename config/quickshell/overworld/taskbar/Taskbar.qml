import Quickshell
import Quickshell.Io
import Quickshell.Wayland
import QtQuick
import qs.menu
import qs.notifications
import qs.session
import qs.theme

PanelWindow {
    id: root

    required property ShellScreen modelData

    function dismiss(): void {
        menu.active = false;
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

    LazyLoader {
        id: menu

        StartMenu {
            bar: root
            onDismissed: menu.active = false
        }
    }

    IpcHandler {
        target: "menu"

        function toggle(): void {
            tasks.close();
            menu.active = !menu.active;
        }
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

    Toasts {
        bar: root
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

        StartButton {
            active: menu.active
            onClicked: {
                preview.hide();
                tasks.close();
                menu.active = !menu.active;
            }
        }

        Rectangle {
            anchors.verticalCenter: parent.verticalCenter
            width: 1
            height: 24
            color: Theme.edge
        }

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
                    if (hovered && !menu.active && !tasks.visible)
                        preview.show(button);
                    else
                        preview.leave();
                }
            }
        }
    }
}
