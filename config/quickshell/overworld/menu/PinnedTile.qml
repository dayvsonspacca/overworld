import Quickshell
import QtQuick
import qs.apps
import qs.theme

Item {
    id: root

    required property DesktopEntry entry

    signal clicked

    implicitHeight: Theme.tileHeight

    Rectangle {
        anchors.fill: parent
        radius: Theme.itemRadius
        color: mouse.containsMouse ? Theme.surface : "transparent"
    }

    Image {
        id: icon

        anchors.horizontalCenter: parent.horizontalCenter
        anchors.top: parent.top
        anchors.topMargin: 14
        width: Theme.tileIconSize
        height: Theme.tileIconSize
        sourceSize: Qt.size(width, height)
        source: Apps.iconSource(root.entry.icon)
    }

    Text {
        anchors.top: icon.bottom
        anchors.topMargin: 8
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.margins: 4
        horizontalAlignment: Text.AlignHCenter
        text: root.entry.name
        color: Theme.text
        font.pixelSize: Theme.fontSmall
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
