import QtQuick
import qs.icons
import qs.system
import qs.theme

Column {
    id: root

    property var opened: null
    // The list holds still while someone is in it, so a scan doesn't rebuild the row being typed in.
    property var networks: []

    signal back

    spacing: 10

    Binding {
        target: Network.wifi
        property: "scannerEnabled"
        value: true
        when: Network.wifi !== null
    }

    Binding {
        target: root
        property: "networks"
        value: Network.wifiNetworks
        when: root.opened === null && !rows.activeFocus && !hover.hovered
        restoreMode: Binding.RestoreNone
    }

    Item {
        width: parent.width
        height: 26

        Rectangle {
            id: back

            width: 26
            height: 26
            activeFocusOnTab: true
            Keys.onReturnPressed: root.back()
            Keys.onSpacePressed: root.back()
            radius: Theme.radiusControl
            color: backMouse.containsMouse ? Theme.activeTint : Theme.hoverTint

            Glyph {
                anchors.centerIn: parent
                path: Glyphs.left
                size: 14
                color: Theme.text
            }

            FocusRing {}

            MouseArea {
                id: backMouse

                anchors.fill: parent
                hoverEnabled: true
                cursorShape: Qt.PointingHandCursor
                onClicked: root.back()
            }
        }

        PageTitle {
            anchors.left: back.right
            anchors.leftMargin: 10
            anchors.verticalCenter: parent.verticalCenter
            text: "Wi‑Fi"
        }

        Row {
            anchors.right: parent.right
            anchors.verticalCenter: parent.verticalCenter
            spacing: 6
            visible: Network.wifi?.scannerEnabled ?? false

            Glyph {
                anchors.verticalCenter: parent.verticalCenter
                path: Glyphs.restart
                size: 12
                color: Theme.subtle
            }

            Text {
                anchors.verticalCenter: parent.verticalCenter
                text: "Scanning"
                color: Theme.subtle
                font.pixelSize: Theme.fontCallout - 1
            }
        }
    }

    Text {
        text: !Network.wifiEnabled ? "Wi‑Fi is off" : "Looking for networks…"
        color: Theme.subtle
        font.pixelSize: Theme.fontCallout
        visible: !Network.wifiEnabled || root.networks.length === 0
    }

    FocusScope {
        id: rows

        width: parent.width
        height: list.implicitHeight
        visible: Network.wifiEnabled

        HoverHandler {
            id: hover
        }

        Column {
            id: list

            width: parent.width
            spacing: 2

            Repeater {
                model: root.networks

                Column {
                    id: entry

                    required property var modelData
                    required property int index
                    readonly property bool first: index === 0 || root.networks[index - 1].connected !== modelData.connected

                    width: list.width
                    spacing: 6
                    topPadding: first && index > 0 ? 10 : 0

                    SectionLabel {
                        text: entry.modelData.connected ? "Connected" : "Available"
                        visible: entry.first
                    }

                    NetworkRow {
                        width: parent.width
                        network: entry.modelData
                        open: root.opened === entry.modelData
                        onToggled: root.opened = open ? null : entry.modelData
                        onClosed: root.opened = null
                    }
                }
            }
        }
    }
}
