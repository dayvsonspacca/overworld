import Quickshell
import Quickshell.Io
import Quickshell.Wayland
import QtQuick
import qs.control
import qs.menu
import qs.notifications
import qs.osd
import qs.session
import qs.theme

PanelWindow {
    id: root

    required property ShellScreen modelData
    property string requestedPage: "general"

    function dismiss(): void {
        menu.active = false;
        control.active = false;
        preview.hide();
        tasks.close();
    }

    function openControl(page: string): void {
        menu.active = false;
        preview.hide();
        tasks.close();

        if (!control.active) {
            requestedPage = page;
            control.active = true;
        } else if (control.item.page === page) {
            control.active = false;
        } else {
            control.item.page = page;
        }
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

    LazyLoader {
        id: control

        ControlCenter {
            bar: root
            page: root.requestedPage
            onDismissed: control.active = false
        }
    }

    IpcHandler {
        target: "menu"

        function toggle(): void {
            control.active = false;
            tasks.close();
            menu.active = !menu.active;
        }
    }

    IpcHandler {
        target: "control"

        function toggle(): void {
            root.openControl("general");
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
        suppressed: control.active
    }

    Osd {
        suppressed: control.active
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
                control.active = false;
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
                    if (hovered && !menu.active && !control.active && !tasks.visible)
                        preview.show(button);
                    else
                        preview.leave();
                }
            }
        }
    }

    StatusButton {
        anchors.right: parent.right
        anchors.rightMargin: Theme.barPadding
        anchors.verticalCenter: parent.verticalCenter
        active: control.active
        onOpened: page => root.openControl(page)
    }
}
