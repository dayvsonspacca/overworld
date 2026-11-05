import Quickshell
import QtQuick
import qs.apps
import qs.daemon
import qs.theme

Item {
    id: root

    signal launched(DesktopEntry entry)

    Text {
        id: title

        text: "Pinned"
        color: Theme.text
        font.pixelSize: Theme.fontContent
        font.weight: Font.DemiBold
    }

    Grid {
        id: grid

        anchors.top: title.bottom
        anchors.topMargin: 12
        anchors.left: parent.left
        anchors.right: parent.right
        columns: 4
        spacing: 6

        Repeater {
            model: Pins.menu.entries

            PinnedTile {
                required property DesktopEntry modelData

                entry: modelData
                width: (grid.width - grid.spacing * (grid.columns - 1)) / grid.columns
                onClicked: root.launched(modelData)
            }
        }
    }

    Text {
        anchors.centerIn: parent
        width: parent.width - 2 * Theme.menuPadding
        horizontalAlignment: Text.AlignHCenter
        wrapMode: Text.WordWrap
        text: Daemon.connected
            ? "No pinned apps.\nSearch for an app and choose “Pin to Start”."
            : "Pinned apps are kept by the Overworld daemon, which isn't running."
        color: Theme.subtle
        font.pixelSize: Theme.fontContent - 1
        lineHeight: 1.3
        visible: Pins.menu.entries.length === 0
    }
}
