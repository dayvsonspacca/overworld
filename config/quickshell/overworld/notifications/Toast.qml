import Quickshell.Services.Notifications
import QtQuick
import QtQuick.Effects
import qs.icons
import qs.theme

Rectangle {
    id: root

    required property Notification notification
    readonly property var actions: notification.actions.filter(a => a.identifier !== "default")
    readonly property bool preview: notification.image !== "" && notification.hints["x-overworld-preview"] === true
    readonly property bool avatar: notification.image !== "" && !preview
    readonly property int timeout: notification.urgency === NotificationUrgency.Critical
        ? 0
        : notification.expireTimeout > 0
            ? Math.min(notification.expireTimeout * 1000, 2 * Theme.toastTimeout)
            : Theme.toastTimeout

    implicitHeight: content.implicitHeight + 24
    radius: Theme.radiusPanel
    color: Theme.background
    border.width: 1
    border.color: Theme.edge

    HoverHandler {
        id: hover
    }

    Timer {
        running: root.timeout > 0 && !hover.hovered
        interval: root.timeout
        onTriggered: Notifications.hide(root.notification)
    }

    MouseArea {
        anchors.fill: parent
        cursorShape: Qt.PointingHandCursor
        onClicked: Notifications.activate(root.notification)
    }

    Column {
        id: content

        anchors.top: parent.top
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.margins: 12
        spacing: 8

        Item {
            width: parent.width
            height: 18

            Image {
                id: appIcon

                anchors.left: parent.left
                anchors.verticalCenter: parent.verticalCenter
                width: 16
                height: 16
                sourceSize: Qt.size(width, height)
                source: Notifications.icon(root.notification)
            }

            Text {
                anchors.left: appIcon.right
                anchors.leftMargin: 8
                anchors.right: close.left
                anchors.rightMargin: 8
                anchors.verticalCenter: parent.verticalCenter
                text: `${root.notification.appName || "System"} · now`
                color: Theme.subtle
                font.pixelSize: Theme.fontCallout - 1
                elide: Text.ElideRight
            }

            Rectangle {
                id: close

                anchors.right: parent.right
                anchors.verticalCenter: parent.verticalCenter
                width: 20
                height: 20
                radius: Theme.radiusSmall
                color: closeMouse.containsMouse ? Theme.hoverTint : "transparent"

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

        Rectangle {
            width: parent.width
            height: 150
            radius: Theme.radiusControl
            color: Theme.surface
            clip: true
            visible: root.preview

            Image {
                anchors.fill: parent
                source: root.preview ? root.notification.image : ""
                fillMode: Image.PreserveAspectCrop
                asynchronous: true
            }
        }

        Row {
            width: parent.width
            spacing: 12

            Item {
                width: 40
                height: 40
                visible: root.avatar

                Image {
                    id: picture

                    anchors.fill: parent
                    source: root.notification.image
                    fillMode: Image.PreserveAspectCrop
                    visible: false
                }

                Rectangle {
                    id: mask

                    anchors.fill: parent
                    radius: width / 2
                    visible: false
                    layer.enabled: true
                }

                MultiEffect {
                    anchors.fill: parent
                    source: picture
                    maskEnabled: true
                    maskSource: mask
                }
            }

            Column {
                width: parent.width - (root.avatar ? 52 : 0)
                spacing: 3

                Text {
                    width: parent.width
                    text: root.notification.summary
                    color: Theme.text
                    font.pixelSize: Theme.fontCallout + 1
                    font.weight: Font.DemiBold
                    elide: Text.ElideRight
                }

                Text {
                    width: parent.width
                    text: root.notification.body
                    textFormat: Text.PlainText
                    visible: text !== ""
                    color: Theme.text
                    font.pixelSize: Theme.fontCallout
                    wrapMode: Text.Wrap
                    maximumLineCount: 4
                    elide: Text.ElideRight
                    lineHeight: 1.15
                }
            }
        }

        Row {
            width: parent.width
            spacing: 8
            visible: root.actions.length > 0

            Repeater {
                model: root.actions

                Rectangle {
                    id: action

                    required property var modelData

                    width: (parent.width - parent.spacing * (root.actions.length - 1)) / root.actions.length
                    height: 30
                    radius: Theme.radiusSmall
                    color: actionMouse.containsMouse ? Theme.activeTint : Theme.hoverTint

                    Text {
                        anchors.centerIn: parent
                        width: parent.width - 16
                        horizontalAlignment: Text.AlignHCenter
                        text: action.modelData.text
                        color: Theme.text
                        font.pixelSize: Theme.fontCallout
                        elide: Text.ElideRight
                    }

                    MouseArea {
                        id: actionMouse

                        anchors.fill: parent
                        hoverEnabled: true
                        cursorShape: Qt.PointingHandCursor
                        onClicked: {
                            action.modelData.invoke();
                            Notifications.dismiss(root.notification);
                        }
                    }
                }
            }
        }
    }
}
