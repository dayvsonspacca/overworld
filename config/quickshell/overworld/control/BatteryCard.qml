import QtQuick
import qs.icons
import qs.system
import qs.theme

Rectangle {
    implicitHeight: column.implicitHeight + 24
    radius: Theme.radiusPanel
    color: Theme.raised

    Column {
        id: column

        anchors.left: parent.left
        anchors.right: parent.right
        anchors.top: parent.top
        anchors.margins: 12
        spacing: 10

        Item {
            width: parent.width
            height: 22

            BatteryIcon {
                id: icon

                anchors.left: parent.left
                anchors.verticalCenter: parent.verticalCenter
                size: 18
                level: Battery.level
                charging: Battery.charging
                low: Battery.low
            }

            Text {
                anchors.left: icon.right
                anchors.leftMargin: 10
                anchors.verticalCenter: parent.verticalCenter
                text: Battery.percent
                color: Battery.low ? Theme.danger : Theme.text
                font.pixelSize: Theme.fontContent + 2
            }

            Text {
                anchors.right: parent.right
                anchors.verticalCenter: parent.verticalCenter
                text: Battery.remaining
                color: Theme.subtle
                font.pixelSize: Theme.fontCallout - 1
            }
        }

        Rectangle {
            width: parent.width
            height: 6
            radius: 3
            color: Theme.surface

            Rectangle {
                width: parent.width * Battery.level
                height: parent.height
                radius: 3
                color: Battery.low ? Theme.danger : Theme.accentLight
            }
        }

        ProfilePicker {
            width: parent.width
        }
    }
}
