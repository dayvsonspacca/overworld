import QtQuick
import qs.icons
import qs.theme

Rectangle {
    id: root

    required property string glyph
    required property string label
    property bool selected

    signal clicked

    implicitHeight: 38
    activeFocusOnTab: true
    Keys.onReturnPressed: clicked()
    Keys.onSpacePressed: clicked()
    radius: Theme.radiusControl
    color: selected ? Theme.activeTint : mouse.containsMouse ? Theme.hoverTint : "transparent"

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
        anchors.leftMargin: 10
        anchors.right: check.left
        anchors.rightMargin: 10
        anchors.verticalCenter: parent.verticalCenter
        text: root.label
        color: Theme.text
        font.pixelSize: Theme.fontCallout + 1
        elide: Text.ElideRight
    }

    Glyph {
        id: check

        anchors.right: parent.right
        anchors.rightMargin: 10
        anchors.verticalCenter: parent.verticalCenter
        path: Glyphs.check
        color: Theme.accentLight
        visible: root.selected
    }

    FocusRing {}

    MouseArea {
        id: mouse

        anchors.fill: parent
        hoverEnabled: true
        cursorShape: Qt.PointingHandCursor
        onClicked: root.clicked()
    }
}
