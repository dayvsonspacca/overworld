import Quickshell
import QtQuick
import qs.apps
import qs.theme

Item {
    id: root

    required property DesktopEntry entry
    property string detail
    property bool selected

    signal clicked

    implicitHeight: Theme.rowHeight

    Rectangle {
        anchors.fill: parent
        radius: Theme.itemRadius
        color: root.selected || mouse.containsMouse ? Theme.surface : "transparent"
    }

    Rectangle {
        anchors.left: parent.left
        anchors.verticalCenter: parent.verticalCenter
        width: 3
        height: parent.height - 16
        radius: 2
        color: Theme.accentLight
        visible: root.selected
    }

    Image {
        id: icon

        anchors.left: parent.left
        anchors.leftMargin: 10
        anchors.verticalCenter: parent.verticalCenter
        width: Theme.rowIconSize
        height: Theme.rowIconSize
        sourceSize: Qt.size(width, height)
        source: Apps.iconSource(root.entry.icon)
    }

    Column {
        anchors.left: icon.right
        anchors.leftMargin: 12
        anchors.right: parent.right
        anchors.rightMargin: 10
        anchors.verticalCenter: parent.verticalCenter

        Text {
            width: parent.width
            text: root.entry.name
            color: Theme.text
            font.pixelSize: Theme.fontSize - 1
            elide: Text.ElideRight
        }

        Text {
            text: root.detail
            color: Theme.subtle
            font.pixelSize: Theme.fontSmall
            visible: root.detail !== ""
        }
    }

    MouseArea {
        id: mouse

        anchors.fill: parent
        hoverEnabled: true
        cursorShape: Qt.PointingHandCursor
        onClicked: root.clicked()
    }
}
