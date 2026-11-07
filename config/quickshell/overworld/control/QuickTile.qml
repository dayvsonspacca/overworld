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
    property bool more

    signal clicked
    signal expanded

    implicitHeight: Theme.quickTileHeight
    activeFocusOnTab: interactive
    Keys.onReturnPressed: clicked()
    Keys.onSpacePressed: clicked()
    radius: Theme.radiusPanel
    color: on ? Theme.accent : mouse.containsMouse ? Theme.activeTint : Theme.hoverTint

    Glyph {
        id: icon

        anchors.left: parent.left
        anchors.leftMargin: 12
        anchors.verticalCenter: parent.verticalCenter
        path: root.glyph
        size: 18
        color: root.on ? Theme.onAccent : Theme.subtle
    }

    Column {
        anchors.left: icon.right
        anchors.leftMargin: 10
        anchors.right: root.more ? chevron.left : parent.right
        anchors.rightMargin: 8
        anchors.verticalCenter: parent.verticalCenter

        Text {
            width: parent.width
            text: root.title
            color: root.on ? Theme.onAccent : Theme.text
            font.pixelSize: Theme.fontCallout + 1
            elide: Text.ElideRight
        }

        Text {
            width: parent.width
            text: root.detail
            visible: root.detail !== ""
            color: root.on ? Qt.alpha(Theme.onAccent, 0.75) : Theme.subtle
            font.pixelSize: Theme.fontCallout - 1
            elide: Text.ElideRight
        }
    }

    FocusRing {
        radius: Theme.radiusPanel + gap
    }

    MouseArea {
        id: mouse

        anchors.fill: parent
        hoverEnabled: true
        enabled: root.interactive
        cursorShape: Qt.PointingHandCursor
        onClicked: root.clicked()
    }

    Rectangle {
        id: chevron

        anchors.top: parent.top
        anchors.bottom: parent.bottom
        anchors.right: parent.right
        width: 34
        visible: root.more
        activeFocusOnTab: visible
        Keys.onReturnPressed: root.expanded()
        Keys.onSpacePressed: root.expanded()
        topRightRadius: Theme.radiusPanel
        bottomRightRadius: Theme.radiusPanel
        color: chevronMouse.containsMouse ? Theme.hoverTint : "transparent"

        Rectangle {
            anchors.left: parent.left
            anchors.verticalCenter: parent.verticalCenter
            width: 1
            height: parent.height - 16
            color: root.on ? Qt.alpha(Theme.onAccent, 0.25) : Theme.divider
        }

        Glyph {
            anchors.centerIn: parent
            path: Glyphs.right
            size: 14
            color: root.on ? Theme.onAccent : Theme.subtle
        }

        FocusRing {
            radius: Theme.radiusPanel + gap
        }

        MouseArea {
            id: chevronMouse

            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: root.expanded()
        }
    }
}
