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
        menu.visible = false;
        control.active = false;
        preview.hide();
        tasks.close();
    }

    function toggleMenu(): void {
        control.active = false;
        tasks.close();
        menu.visible = !menu.visible;
    }

    function openControl(page: string): void {
        menu.visible = false;
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

    StartMenu {
        id: menu

        bar: root
        visible: false
        onDismissed: menu.visible = false
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
            root.toggleMenu();
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
            active: menu.visible
            onClicked: {
                preview.hide();
                root.toggleMenu();
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
                    if (hovered && !menu.visible && !control.active && !tasks.visible)
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
