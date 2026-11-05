import QtQuick
import qs.icons
import qs.theme

Rectangle {
    id: root

    required property var item
    readonly property string title: item.title || item.tooltipTitle || item.id

    signal inspect

    implicitHeight: 44
    activeFocusOnTab: true
    Keys.onReturnPressed: open()
    Keys.onSpacePressed: open()
    Keys.onRightPressed: event => {
        if (item.hasMenu)
            inspect();
        else
            event.accepted = false;
    }
    radius: Theme.itemRadius
    color: mouse.containsMouse ? Theme.activeTint : Theme.hoverTint

    function open(): void {
        if (item.onlyMenu && item.hasMenu)
            inspect();
        else
            item.activate();
    }

    Image {
        id: icon

        anchors.left: parent.left
        anchors.leftMargin: 12
        anchors.verticalCenter: parent.verticalCenter
        width: 22
        height: 22
        sourceSize: Qt.size(width, height)
        source: root.item.icon
    }

    Text {
        anchors.left: icon.right
        anchors.leftMargin: 12
        anchors.right: arrow.left
        anchors.rightMargin: 8
        anchors.verticalCenter: parent.verticalCenter
        text: root.title
        color: Theme.text
        font.pixelSize: Theme.fontSmall + 1
        elide: Text.ElideRight
    }

    FocusRing {}

    MouseArea {
        id: mouse

        anchors.fill: parent
        hoverEnabled: true
        cursorShape: Qt.PointingHandCursor
        onClicked: root.open()
    }

    Rectangle {
        id: arrow

        anchors.right: parent.right
        anchors.rightMargin: 6
        anchors.verticalCenter: parent.verticalCenter
        width: 32
        height: 32
        radius: Theme.tileRadius
        color: arrowMouse.containsMouse ? Theme.hoverTint : "transparent"
        visible: root.item.hasMenu

        Glyph {
            anchors.centerIn: parent
            path: Glyphs.right
            size: 12
            color: Theme.subtle
        }

        MouseArea {
            id: arrowMouse

            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: root.inspect()
        }
    }
}
