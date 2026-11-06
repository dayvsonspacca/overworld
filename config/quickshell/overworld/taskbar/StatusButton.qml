import QtQuick
import qs.icons
import qs.notifications
import qs.system
import qs.theme

Rectangle {
    id: root

    property bool active

    signal opened(string page)

    implicitWidth: row.implicitWidth + 24
    implicitHeight: Theme.tileSize
    radius: Theme.radiusSmall
    color: active
        ? (hover.hovered ? Theme.activeHoverTint : Theme.activeTint)
        : (hover.hovered ? Theme.hoverTint : "transparent")

    HoverHandler {
        id: hover
    }

    MouseArea {
        anchors.fill: parent
        cursorShape: Qt.PointingHandCursor
        onClicked: root.opened("general")
        onWheel: event => Audio.setVolume(Audio.sink, Audio.volume + (event.angleDelta.y > 0 ? 0.05 : -0.05))
    }

    Row {
        id: row

        anchors.centerIn: parent
        spacing: 12

        Glyph {
            anchors.verticalCenter: parent.verticalCenter
            path: Network.glyph
            color: Theme.text
        }

        Glyph {
            anchors.verticalCenter: parent.verticalCenter
            path: Audio.glyph
            color: Theme.text
        }

        Row {
            anchors.verticalCenter: parent.verticalCenter
            spacing: 4
            visible: Battery.available

            BatteryIcon {
                anchors.verticalCenter: parent.verticalCenter
                level: Battery.level
                charging: Battery.charging
                low: Battery.low
            }

            Text {
                anchors.verticalCenter: parent.verticalCenter
                text: Battery.percent
                color: Battery.low ? Theme.danger : Theme.text
                font.pixelSize: Theme.fontCallout
            }
        }

        Glyph {
            anchors.verticalCenter: parent.verticalCenter
            path: Glyphs.bell
            color: Theme.text

            Rectangle {
                x: parent.width - width / 2
                y: -height / 2
                width: Math.max(height, count.implicitWidth + 7)
                height: 14
                radius: height / 2
                color: Theme.accent
                visible: Notifications.count > 0

                Text {
                    id: count

                    anchors.centerIn: parent
                    text: Notifications.count > 99 ? "99+" : Notifications.count
                    color: "white"
                    font.pixelSize: 9
                    font.weight: Font.DemiBold
                }
            }

            MouseArea {
                anchors.fill: parent
                anchors.margins: -6
                cursorShape: Qt.PointingHandCursor
                onClicked: root.opened("notices")
            }
        }

        Item {
            anchors.verticalCenter: parent.verticalCenter
            width: clock.implicitWidth
            height: clock.implicitHeight

            Clock {
                id: clock
            }

            MouseArea {
                anchors.fill: parent
                anchors.margins: -6
                cursorShape: Qt.PointingHandCursor
                onClicked: root.opened("agenda")
            }
        }
    }
}
