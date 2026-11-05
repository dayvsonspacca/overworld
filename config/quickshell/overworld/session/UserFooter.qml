import Quickshell
import QtQuick
import qs.icons
import qs.popups
import qs.theme

Rectangle {
    id: root

    readonly property string user: Quickshell.env("USER") ?? ""
    readonly property var power: [
        { label: "Lock", glyph: Glyphs.lock, run: () => Locker.lock() },
        { label: "Sign out", glyph: Glyphs.logout, run: () => Quickshell.execDetached(["sh", "-c", "loginctl terminate-session \"$XDG_SESSION_ID\""]) },
        { label: "Sleep", glyph: Glyphs.sleep, run: () => Quickshell.execDetached(["systemctl", "suspend"]) },
        { label: "Restart", glyph: Glyphs.restart, run: () => Quickshell.execDetached(["systemctl", "reboot"]) },
        { label: "Shut down", glyph: Glyphs.power, run: () => Quickshell.execDetached(["systemctl", "poweroff"]), apart: true }
    ]
    property bool powerOpen

    signal done

    function run(option: var): void {
        powerOpen = false;
        option.run();
        done();
    }

    onPowerOpenChanged: {
        if (!powerOpen)
            return;

        const corner = button.mapToItem(menu.parent, button.width, 0);
        menu.x = corner.x - menu.width;
        menu.y = corner.y - menu.height - 8;
    }

    implicitHeight: Theme.footerHeight
    color: Theme.raised

    Rectangle {
        anchors.top: parent.top
        anchors.left: parent.left
        anchors.right: parent.right
        height: 1
        color: Theme.divider
    }

    Row {
        anchors.left: parent.left
        anchors.leftMargin: Theme.menuPadding
        anchors.verticalCenter: parent.verticalCenter
        spacing: 10

        Avatar {
            user: root.user
        }

        Text {
            anchors.verticalCenter: parent.verticalCenter
            text: root.user
            color: Theme.text
            font.pixelSize: Theme.fontSize - 1
        }
    }

    Rectangle {
        id: button

        anchors.right: parent.right
        anchors.rightMargin: Theme.menuPadding - 8
        anchors.verticalCenter: parent.verticalCenter
        width: 36
        height: 36
        radius: Theme.itemRadius
        color: root.powerOpen || powerMouse.containsMouse ? Theme.surface : "transparent"

        Glyph {
            anchors.centerIn: parent
            path: Glyphs.power
            size: 18
            color: root.powerOpen ? Theme.accentLight : Theme.text
        }

        MouseArea {
            id: powerMouse

            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: root.powerOpen = !root.powerOpen
        }
    }

    MouseArea {
        parent: root.Window.contentItem
        anchors.fill: parent
        z: 1000
        visible: root.powerOpen
        onClicked: root.powerOpen = false
    }

    Rectangle {
        id: menu

        parent: root.Window.contentItem
        z: 1001
        width: 200
        height: options.implicitHeight + 12
        radius: Theme.panelRadius
        color: Theme.background
        border.width: 1
        border.color: Theme.edge
        visible: root.powerOpen

        Column {
            id: options

            anchors.fill: parent
            anchors.margins: 6
            spacing: 2

            Repeater {
                model: root.power

                MenuOption {
                    required property var modelData

                    width: options.width
                    label: modelData.label
                    glyph: modelData.glyph
                    apart: modelData.apart === true
                    onClicked: root.run(modelData)
                }
            }
        }
    }
}
