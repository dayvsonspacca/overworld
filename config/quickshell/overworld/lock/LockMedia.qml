import Quickshell.Services.Mpris
import QtQuick
import qs.icons
import qs.theme

Rectangle {
    id: root

    required property MprisPlayer player

    component Control: Rectangle {
        id: control

        required property string glyph
        property bool enabled: true

        signal clicked

        width: 28
        height: 28
        radius: 14
        color: mouse.containsMouse ? Theme.activeHoverTint : Theme.hoverTint
        opacity: enabled ? 1 : 0.4

        Glyph {
            anchors.centerIn: parent
            path: control.glyph
            size: 12
            filled: true
            color: Theme.text
        }

        MouseArea {
            id: mouse

            anchors.fill: parent
            enabled: control.enabled
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: control.clicked()
        }
    }

    implicitWidth: 300
    implicitHeight: 64
    radius: Theme.radiusPanel
    color: Qt.alpha(Theme.background, 0.72)
    border.width: 1
    border.color: Theme.edge

    Rectangle {
        id: cover

        anchors.left: parent.left
        anchors.leftMargin: 10
        anchors.verticalCenter: parent.verticalCenter
        width: 44
        height: 44
        radius: Theme.radiusControl
        clip: true
        gradient: Gradient {
            GradientStop { position: 0; color: Theme.accentLight }
            GradientStop { position: 1; color: Theme.accentDeep }
        }

        Image {
            anchors.fill: parent
            source: root.player.trackArtUrl
            fillMode: Image.PreserveAspectCrop
            sourceSize: Qt.size(88, 88)
        }
    }

    Column {
        anchors.left: cover.right
        anchors.leftMargin: 12
        anchors.right: controls.left
        anchors.rightMargin: 10
        anchors.verticalCenter: parent.verticalCenter
        spacing: 2

        Text {
            width: parent.width
            text: root.player.trackTitle || root.player.identity
            color: Theme.text
            font.pixelSize: Theme.fontCallout + 1
            font.weight: Font.DemiBold
            elide: Text.ElideRight
        }

        Text {
            width: parent.width
            text: root.player.trackArtist
            color: Theme.subtle
            font.pixelSize: Theme.fontCallout
            elide: Text.ElideRight
            visible: text !== ""
        }
    }

    Row {
        id: controls

        anchors.right: parent.right
        anchors.rightMargin: 10
        anchors.verticalCenter: parent.verticalCenter
        spacing: 6

        Control {
            glyph: Glyphs.previous
            enabled: root.player.canGoPrevious
            onClicked: root.player.previous()
        }

        Control {
            glyph: root.player.isPlaying ? Glyphs.pause : Glyphs.play
            enabled: root.player.canTogglePlaying
            onClicked: root.player.togglePlaying()
        }

        Control {
            glyph: Glyphs.next
            enabled: root.player.canGoNext
            onClicked: root.player.next()
        }
    }
}
