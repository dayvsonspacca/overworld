import Quickshell.Services.Mpris
import QtQuick
import qs.icons
import qs.theme

Rectangle {
    id: root

    required property MprisPlayer player

    function time(seconds: real): string {
        const s = Math.max(0, Math.floor(seconds));
        return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, "0")}`;
    }

    component MediaButton: Rectangle {
        id: button

        required property string glyph
        property bool main

        signal clicked

        width: 32
        height: 32
        radius: 16
        activeFocusOnTab: visible
        Keys.onReturnPressed: clicked()
        Keys.onSpacePressed: clicked()
        color: main ? Theme.text : mouse.containsMouse ? Theme.hoverTint : "transparent"

        Glyph {
            anchors.centerIn: parent
            path: button.glyph
            size: 14
            filled: !button.main
            weight: button.main ? 2.4 : 1.4
            color: button.main ? Theme.background : Theme.text
        }

        FocusRing {
            radius: button.radius + gap
        }

        MouseArea {
            id: mouse

            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: button.clicked()
        }
    }

    implicitHeight: content.implicitHeight + 24
    radius: Theme.radiusControl
    color: Qt.alpha(Theme.surface, 0.75)
    border.width: 1
    border.color: Theme.edge

    Timer {
        running: root.player.isPlaying
        interval: 1000
        repeat: true
        onTriggered: root.player.positionChanged()
    }

    Column {
        id: content

        anchors.top: parent.top
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.margins: 12
        spacing: 6

        Row {
            width: parent.width
            spacing: 12

            Rectangle {
                width: 56
                height: 56
                radius: Theme.radiusControl
                gradient: Gradient {
                    GradientStop { position: 0; color: Theme.accentLight }
                    GradientStop { position: 1; color: Theme.accentDeep }
                }

                Glyph {
                    anchors.centerIn: parent
                    path: Glyphs.note
                    size: 22
                    color: Theme.background
                    visible: art.status !== Image.Ready
                }

                Image {
                    id: art

                    anchors.fill: parent
                    source: root.player.trackArtUrl
                    fillMode: Image.PreserveAspectCrop
                    visible: status === Image.Ready
                }
            }

            Column {
                anchors.verticalCenter: parent.verticalCenter
                width: parent.width - 68
                spacing: 3

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
                    text: [root.player.trackArtist, root.player.identity].filter(s => s).join(" · ")
                    color: Theme.subtle
                    font.pixelSize: Theme.fontCallout
                    elide: Text.ElideRight
                }

                Item {
                    width: parent.width
                    height: 22
                    visible: root.player.lengthSupported && root.player.length > 0

                    Rectangle {
                        anchors.bottom: times.top
                        anchors.bottomMargin: 4
                        width: parent.width
                        height: 3
                        radius: 2
                        color: Theme.track

                        Rectangle {
                            width: parent.width * Math.min(1, root.player.position / Math.max(1, root.player.length))
                            height: parent.height
                            radius: 2
                            color: Theme.accentLight
                        }
                    }

                    Item {
                        id: times

                        anchors.bottom: parent.bottom
                        width: parent.width
                        height: 12

                        Text {
                            text: root.time(root.player.position)
                            color: Theme.subtle
                            font.pixelSize: Theme.fontCallout - 1
                        }

                        Text {
                            anchors.right: parent.right
                            text: root.time(root.player.length)
                            color: Theme.subtle
                            font.pixelSize: Theme.fontCallout - 1
                        }
                    }
                }
            }
        }

        Row {
            anchors.horizontalCenter: parent.horizontalCenter
            spacing: 6

            MediaButton {
                glyph: Glyphs.previous
                visible: root.player.canGoPrevious
                onClicked: root.player.previous()
            }

            MediaButton {
                glyph: root.player.isPlaying ? Glyphs.pause : Glyphs.play
                main: true
                onClicked: root.player.togglePlaying()
            }

            MediaButton {
                glyph: Glyphs.next
                visible: root.player.canGoNext
                onClicked: root.player.next()
            }
        }
    }
}
