import QtQuick
import qs.icons
import qs.theme

// A Bell notice as a toast: it waits for an answer instead of timing out, and says how it ended before leaving.
Rectangle {
    id: root

    required property var notice
    required property string said
    readonly property bool questions: notice.ask?.type === "questions"

    implicitHeight: content.implicitHeight + 24
    radius: Theme.radiusPanel
    color: Theme.background
    border.width: 1
    border.color: Theme.edge

    Timer {
        running: root.said !== ""
        interval: 1500
        onTriggered: Bell.hide(root.notice)
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
                source: Bell.icon(root.notice)
            }

            Rectangle {
                id: agent

                anchors.left: appIcon.right
                anchors.leftMargin: 8
                anchors.verticalCenter: parent.verticalCenter
                width: visible ? 8 : 0
                height: 8
                radius: 4
                color: root.notice.color ?? "transparent"
                visible: !!root.notice.color
            }

            Text {
                anchors.left: agent.right
                anchors.leftMargin: agent.visible ? 6 : 0
                anchors.right: close.left
                anchors.rightMargin: 8
                anchors.verticalCenter: parent.verticalCenter
                text: [Bell.app(root.notice), root.notice.context, "now"].filter(Boolean).join(" · ")
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
                    onClicked: Bell.hide(root.notice)
                }
            }
        }

        Text {
            width: parent.width
            text: root.notice.title
            visible: !root.questions || root.said !== ""
            color: Theme.text
            font.pixelSize: Theme.fontCallout + 1
            font.weight: Font.DemiBold
            elide: Text.ElideRight
        }

        Text {
            width: parent.width
            text: root.notice.body ?? ""
            textFormat: Text.PlainText
            visible: text !== "" && !root.questions && root.said === ""
            color: Theme.text
            font.pixelSize: Theme.fontCallout
            wrapMode: Text.Wrap
            maximumLineCount: 4
            elide: Text.ElideRight
            lineHeight: 1.15
        }

        Ask {
            width: parent.width
            notice: root.notice
            visible: root.said === ""
        }

        Text {
            text: root.said
            visible: root.said !== ""
            color: root.said.startsWith("✓") ? Theme.accentLight : Theme.subtle
            font.pixelSize: Theme.fontCallout - 1
        }
    }
}
