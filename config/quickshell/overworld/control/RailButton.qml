import QtQuick
import qs.icons
import qs.theme

Item {
    id: root

    required property string glyph
    property bool selected
    property bool focused
    property int badge

    signal clicked

    width: 40
    height: 40

    Rectangle {
        anchors.fill: parent
        radius: Theme.radiusControl
        color: root.selected ? Theme.activeTint : mouse.containsMouse ? Theme.hoverTint : "transparent"
    }

    Rectangle {
        x: -6
        anchors.verticalCenter: parent.verticalCenter
        width: 3
        height: 20
        radius: 2
        color: Theme.accentLight
        visible: root.selected
    }

    Glyph {
        anchors.centerIn: parent
        path: root.glyph
        size: 18
        color: root.selected ? Theme.text : Theme.subtle
    }

    Rectangle {
        anchors.top: parent.top
        anchors.right: parent.right
        anchors.topMargin: 3
        anchors.rightMargin: 2
        width: Math.max(height, count.implicitWidth + 8)
        height: 15
        radius: height / 2
        color: Theme.accent
        visible: root.badge > 0

        Text {
            id: count

            anchors.centerIn: parent
            text: root.badge > 99 ? "99+" : root.badge
            color: Theme.onAccent
            font.pixelSize: 10
            font.weight: Font.DemiBold
        }
    }

    FocusRing {
        visible: root.focused
    }

    MouseArea {
        id: mouse

        anchors.fill: parent
        hoverEnabled: true
        cursorShape: Qt.PointingHandCursor
        onClicked: root.clicked()
    }
}
