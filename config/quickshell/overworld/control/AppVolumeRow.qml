import Quickshell.Services.Pipewire
import QtQuick
import qs.icons
import qs.system
import qs.theme

Item {
    id: root

    required property PwNode node
    readonly property real volume: node?.audio?.volume ?? 0
    readonly property bool muted: node?.audio?.muted ?? false
    readonly property string icon: Audio.icon(node)

    implicitHeight: 40
    activeFocusOnTab: true
    Keys.onLeftPressed: Audio.setVolume(node, volume - 0.05)
    Keys.onRightPressed: Audio.setVolume(node, volume + 0.05)
    Keys.onReturnPressed: Audio.toggleMute(node)
    Keys.onSpacePressed: Audio.toggleMute(node)

    FocusRing {
        gap: 5
    }

    Item {
        id: lead

        anchors.left: parent.left
        anchors.verticalCenter: parent.verticalCenter
        width: 22
        height: 22

        Image {
            anchors.fill: parent
            visible: root.icon !== ""
            source: root.icon
            sourceSize: Qt.size(width, height)
        }

        Rectangle {
            anchors.fill: parent
            visible: root.icon === ""
            radius: 5
            gradient: Gradient {
                GradientStop { position: 0; color: Theme.border }
                GradientStop { position: 1; color: Theme.surface }
            }

            Glyph {
                anchors.centerIn: parent
                path: Glyphs.volume
                size: 13
                color: Theme.accentLight
            }
        }

        MouseArea {
            anchors.fill: parent
            cursorShape: Qt.PointingHandCursor
            onClicked: Audio.toggleMute(root.node)
        }
    }

    Text {
        id: name

        anchors.top: parent.top
        anchors.left: lead.right
        anchors.leftMargin: 12
        anchors.right: percent.left
        anchors.rightMargin: 8
        text: Audio.label(root.node)
        color: root.muted ? Theme.subtle : Theme.text
        font.pixelSize: Theme.fontSmall + 1
        elide: Text.ElideRight
    }

    Text {
        id: percent

        anchors.right: parent.right
        anchors.verticalCenter: name.verticalCenter
        width: 36
        horizontalAlignment: Text.AlignRight
        text: `${Math.round(root.volume * 100)}%`
        color: Theme.text
        font.pixelSize: Theme.fontSmall
    }

    VolumeSlider {
        anchors.left: name.left
        anchors.right: parent.right
        anchors.bottom: parent.bottom
        value: root.muted ? 0 : root.volume
        onMoved: value => Audio.setVolume(root.node, value)
    }
}
