import QtQuick
import qs.icons
import qs.theme

Column {
    id: root

    required property string label
    required property string glyph
    property bool apart

    signal clicked

    spacing: 4

    Rectangle {
        width: parent.width
        height: 1
        color: Theme.edge
        visible: root.apart
    }

    Rectangle {
        width: parent.width
        height: 36
        radius: Theme.radiusControl
        color: mouse.containsMouse ? Theme.activeTint : "transparent"

        Glyph {
            id: icon

            anchors.left: parent.left
            anchors.leftMargin: 10
            anchors.verticalCenter: parent.verticalCenter
            path: root.glyph
            color: Theme.text
        }

        Text {
            anchors.left: icon.right
            anchors.leftMargin: 12
            anchors.right: parent.right
            anchors.rightMargin: 10
            anchors.verticalCenter: parent.verticalCenter
            text: root.label
            color: Theme.text
            font.pixelSize: Theme.fontContent - 1
            elide: Text.ElideRight
        }

        MouseArea {
            id: mouse

            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: root.clicked()
        }
    }
}
