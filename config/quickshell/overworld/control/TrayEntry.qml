import Quickshell
import QtQuick
import qs.icons
import qs.theme

Item {
    id: root

    required property QsMenuEntry entry
    readonly property string label: entry.text.replace(/_(_?)/g, "$1")
    readonly property bool checkable: entry.buttonType !== QsMenuButtonType.None

    signal activated

    implicitHeight: entry.isSeparator ? 9 : 36
    activeFocusOnTab: !entry.isSeparator && entry.enabled
    Keys.onReturnPressed: activated()
    Keys.onSpacePressed: activated()
    Keys.onRightPressed: event => {
        if (entry.hasChildren)
            activated();
        else
            event.accepted = false;
    }

    Rectangle {
        anchors.verticalCenter: parent.verticalCenter
        width: parent.width
        height: 1
        color: Theme.divider
        visible: root.entry.isSeparator
    }

    Rectangle {
        anchors.fill: parent
        radius: Theme.radiusControl
        color: mouse.containsMouse ? Theme.hoverTint : "transparent"
        visible: !root.entry.isSeparator

        Item {
            id: lead

            anchors.left: parent.left
            anchors.leftMargin: 10
            anchors.verticalCenter: parent.verticalCenter
            width: 16
            height: 16

            Glyph {
                anchors.centerIn: parent
                path: Glyphs.check
                size: 14
                color: Theme.accentLight
                visible: root.checkable && root.entry.checkState === Qt.Checked
            }

            Image {
                anchors.fill: parent
                sourceSize: Qt.size(width, height)
                source: root.entry.icon
                visible: !root.checkable && root.entry.icon !== ""
            }
        }

        Text {
            anchors.left: lead.right
            anchors.leftMargin: 12
            anchors.right: arrow.left
            anchors.rightMargin: 8
            anchors.verticalCenter: parent.verticalCenter
            text: root.label
            color: root.entry.enabled ? Theme.text : Theme.subtle
            font.pixelSize: Theme.fontCallout + 1
            elide: Text.ElideRight
        }

        Glyph {
            id: arrow

            anchors.right: parent.right
            anchors.rightMargin: 10
            anchors.verticalCenter: parent.verticalCenter
            path: Glyphs.right
            size: 12
            color: Theme.subtle
            visible: root.entry.hasChildren
        }

        MouseArea {
            id: mouse

            anchors.fill: parent
            enabled: root.entry.enabled
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: root.activated()
        }
    }

    FocusRing {}
}
