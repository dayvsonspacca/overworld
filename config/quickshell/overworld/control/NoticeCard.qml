import Quickshell.Services.Notifications
import QtQuick
import qs.icons
import qs.notifications
import qs.theme

Rectangle {
    id: root

    required property Notification notification

    implicitHeight: content.implicitHeight + 20
    radius: Theme.radiusControl
    color: mouse.containsMouse ? Theme.surface : Qt.alpha(Theme.surface, 0.75)
    border.width: 1
    border.color: Theme.edge
    activeFocusOnTab: true
    Keys.onReturnPressed: Notifications.activate(notification)
    Keys.onSpacePressed: Notifications.activate(notification)
    Keys.onDeletePressed: Notifications.dismiss(notification)

    MouseArea {
        id: mouse

        anchors.fill: parent
        hoverEnabled: true
        cursorShape: Qt.PointingHandCursor
        onClicked: Notifications.activate(root.notification)
    }

    Image {
        id: icon

        anchors.left: parent.left
        anchors.top: parent.top
        anchors.margins: 10
        width: 20
        height: 20
        sourceSize: Qt.size(width, height)
        source: Notifications.icon(root.notification)
    }

    Column {
        id: content

        anchors.top: parent.top
        anchors.left: icon.right
        anchors.right: parent.right
        anchors.margins: 10
        spacing: 3

        Item {
            width: parent.width
            height: title.implicitHeight

            Text {
                id: title

                anchors.left: parent.left
                anchors.right: corner.left
                anchors.rightMargin: 8
                text: root.notification.summary
                color: Theme.text
                font.pixelSize: Theme.fontCallout
                font.weight: Font.DemiBold
                elide: Text.ElideRight
            }

            Item {
                id: corner

                anchors.right: parent.right
                anchors.verticalCenter: title.verticalCenter
                width: Math.max(time.implicitWidth, 20)
                height: 20

                Text {
                    id: time

                    anchors.right: parent.right
                    anchors.verticalCenter: parent.verticalCenter
                    text: Notifications.time(root.notification)
                    color: Theme.subtle
                    font.pixelSize: Theme.fontCallout - 1
                    visible: !mouse.containsMouse && !closeMouse.containsMouse && !root.activeFocus
                }

                Rectangle {
                    anchors.right: parent.right
                    anchors.verticalCenter: parent.verticalCenter
                    width: 20
                    height: 20
                    radius: Theme.radiusSmall
                    color: closeMouse.containsMouse ? Theme.hoverTint : "transparent"
                    visible: !time.visible

                    CloseIcon {
                        anchors.centerIn: parent
                        color: Theme.subtle
                    }

                    MouseArea {
                        id: closeMouse

                        anchors.fill: parent
                        hoverEnabled: true
                        cursorShape: Qt.PointingHandCursor
                        onClicked: Notifications.dismiss(root.notification)
                    }
                }
            }
        }

        Text {
            width: parent.width
            text: root.notification.body
            textFormat: Text.PlainText
            visible: text !== ""
            color: Theme.text
            font.pixelSize: Theme.fontCallout
            wrapMode: Text.Wrap
            maximumLineCount: 3
            elide: Text.ElideRight
        }
    }

    FocusRing {}
}
