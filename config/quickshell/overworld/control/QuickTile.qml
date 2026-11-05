import QtQuick
import qs.icons
import qs.theme

Rectangle {
    id: root

    required property string glyph
    required property string title
    property string detail
    property bool on
    property bool interactive: true

    signal clicked

    implicitHeight: Theme.quickTileHeight
    activeFocusOnTab: interactive
    Keys.onReturnPressed: clicked()
    Keys.onSpacePressed: clicked()
    radius: Theme.panelRadius
    color: on ? Theme.accent : mouse.containsMouse ? Theme.activeTint : Theme.hoverTint

    Glyph {
        id: icon

        anchors.left: parent.left
        anchors.leftMargin: 12
        anchors.verticalCenter: parent.verticalCenter
        path: root.glyph
        size: 18
        color: root.on ? "white" : Theme.subtle
    }

    Column {
        anchors.left: icon.right
        anchors.leftMargin: 10
        anchors.right: parent.right
        anchors.rightMargin: 8
        anchors.verticalCenter: parent.verticalCenter

        Text {
            width: parent.width
            text: root.title
            color: root.on ? "white" : Theme.text
            font.pixelSize: Theme.fontSmall + 1
            elide: Text.ElideRight
        }

        Text {
            width: parent.width
            text: root.detail
            visible: root.detail !== ""
            color: root.on ? Qt.alpha("white", 0.75) : Theme.subtle
            font.pixelSize: Theme.fontSmall - 1
            elide: Text.ElideRight
        }
    }

    FocusRing {
        radius: Theme.panelRadius + gap
    }

    MouseArea {
        id: mouse

        anchors.fill: parent
        hoverEnabled: true
        enabled: root.interactive
        cursorShape: Qt.PointingHandCursor
        onClicked: root.clicked()
    }
}
