import QtQuick
import qs.notifications
import qs.theme

// A Bell notice waiting on you in the control center; a question opens in place to be answered, buttons show at once.
Rectangle {
    id: root

    required property var notice
    readonly property bool questions: notice.ask?.type === "questions"
    property bool open: !questions

    implicitHeight: content.implicitHeight + 20
    radius: Theme.radiusControl
    color: Qt.alpha(Theme.surface, 0.75)
    border.width: 1
    border.color: Theme.edge
    clip: true

    Rectangle {
        width: 3
        height: parent.height
        color: Theme.accent
    }

    MouseArea {
        anchors.fill: parent
        enabled: !root.open
        cursorShape: Qt.PointingHandCursor
        onClicked: root.open = true
    }

    Image {
        id: icon

        anchors.left: parent.left
        anchors.top: parent.top
        anchors.margins: 10
        anchors.leftMargin: 13
        width: 20
        height: 20
        sourceSize: Qt.size(width, height)
        source: Bell.icon(root.notice)
    }

    Column {
        id: content

        anchors.top: parent.top
        anchors.left: icon.right
        anchors.right: parent.right
        anchors.margins: 10
        spacing: 6

        Item {
            width: parent.width
            height: title.implicitHeight

            Text {
                id: title

                anchors.left: parent.left
                anchors.right: time.left
                anchors.rightMargin: 8
                text: [Bell.app(root.notice), root.notice.context].filter(Boolean).join(" · ")
                color: Theme.subtle
                font.pixelSize: Theme.fontCallout - 1
                elide: Text.ElideRight
            }

            Text {
                id: time

                anchors.right: parent.right
                text: Bell.time(root.notice)
                color: Theme.subtle
                font.pixelSize: Theme.fontCallout - 1
            }
        }

        Text {
            width: parent.width
            text: root.notice.title
            visible: !root.questions
            color: Theme.text
            font.pixelSize: Theme.fontCallout
            font.weight: Font.DemiBold
            elide: Text.ElideRight
        }

        Text {
            width: parent.width
            text: root.questions ? root.notice.ask.asked[0].text : root.notice.body ?? ""
            visible: !root.open && text !== ""
            color: Theme.text
            font.pixelSize: Theme.fontCallout
            wrapMode: Text.Wrap
            maximumLineCount: 2
            elide: Text.ElideRight
        }

        Text {
            text: "Click to answer"
            visible: !root.open
            color: Theme.subtle
            font.pixelSize: Theme.fontCallout - 1
        }

        Ask {
            width: parent.width
            notice: root.notice
            visible: root.open
        }
    }
}
