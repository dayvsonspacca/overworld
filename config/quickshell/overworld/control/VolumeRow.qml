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

    implicitHeight: 24
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
        width: 20
        height: 20

        Glyph {
            anchors.centerIn: parent
            path: root.muted ? Glyphs.muted : root.volume < 0.34 ? Glyphs.volumeLow : Glyphs.volume
            color: Theme.text
        }

        MouseArea {
            anchors.fill: parent
            cursorShape: Qt.PointingHandCursor
            onClicked: Audio.toggleMute(root.node)
        }
    }

    VolumeSlider {
        anchors.left: lead.right
        anchors.leftMargin: 12
        anchors.right: percent.left
        anchors.rightMargin: 12
        anchors.verticalCenter: parent.verticalCenter
        value: root.muted ? 0 : root.volume
        onMoved: value => Audio.setVolume(root.node, value)
    }

    Text {
        id: percent

        anchors.right: parent.right
        anchors.verticalCenter: parent.verticalCenter
        width: 36
        horizontalAlignment: Text.AlignRight
        text: `${Math.round(root.volume * 100)}%`
        color: Theme.text
        font.pixelSize: Theme.fontCallout
    }
}
