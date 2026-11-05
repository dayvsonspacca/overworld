import Quickshell
import QtQuick
import QtQuick.Effects
import qs.icons
import qs.session
import qs.system
import qs.theme
import qs.wallpaper

Item {
    id: root

    required property bool active
    required property string user
    property bool shown: true
    property int duration: 400
    property url snapshot
    property bool busy
    property string error
    property real progress
    readonly property bool frozen: picture.status === Image.Ready
    readonly property real slide: (1 - progress) * 30

    signal submitted(string password)

    function play(): void {
        fade.to = shown ? 1 : 0;
        fade.restart();
    }

    onShownChanged: play()
    Component.onCompleted: play()

    onErrorChanged: {
        if (error !== "")
            field.reject();
    }

    NumberAnimation {
        id: fade

        target: root
        property: "progress"
        duration: root.duration
        easing.type: Easing.OutCubic
    }

    SystemClock {
        id: clock

        precision: SystemClock.Minutes
    }

    Backdrop {
        anchors.fill: parent
        visible: !root.frozen
    }

    Image {
        id: picture

        anchors.fill: parent
        source: root.snapshot
        cache: false
        visible: false
    }

    MultiEffect {
        id: soft

        anchors.fill: parent
        source: picture
        visible: false
        layer.enabled: true
        autoPaddingEnabled: false
        blurEnabled: true
        blurMax: 64
        blur: root.progress
    }

    MultiEffect {
        anchors.fill: parent
        source: soft
        visible: root.frozen
        scale: 1 + 0.04 * root.progress
        autoPaddingEnabled: false
        blurEnabled: true
        blurMax: 59
        blur: root.progress
        brightness: -0.35 * root.progress
    }

    Rectangle {
        anchors.fill: parent
        opacity: root.frozen ? root.progress : 1
        gradient: Gradient {
            GradientStop { position: 0.4; color: "transparent" }
            GradientStop { position: 1; color: Qt.alpha(Theme.background, 0.75) }
        }
    }

    Column {
        anchors.left: parent.left
        anchors.bottom: parent.bottom
        anchors.margins: Theme.lockMargin
        spacing: 12
        opacity: root.progress

        transform: Translate {
            y: root.slide
        }

        Text {
            text: clock.date.toLocaleTimeString(Qt.locale(), Locale.ShortFormat)
            color: Theme.text
            font.pixelSize: Theme.lockClock
            font.weight: Font.Light
            font.letterSpacing: -2
        }

        Text {
            text: clock.date.toLocaleDateString(Qt.locale(), Dates.dayFormat)
            color: Qt.alpha(Theme.text, 0.7)
            font.pixelSize: Theme.fontTitle2 + 3
        }
    }

    Row {
        anchors.left: parent.left
        anchors.top: parent.top
        anchors.margins: Theme.lockMargin
        anchors.topMargin: Theme.lockMargin - 12
        spacing: 14
        visible: root.active
        opacity: root.progress

        Glyph {
            path: Network.glyph
            color: Qt.alpha(Theme.text, 0.7)
        }

        Glyph {
            path: Audio.glyph
            color: Qt.alpha(Theme.text, 0.7)
        }
    }

    Loader {
        anchors.right: parent.right
        anchors.top: parent.top
        anchors.margins: Theme.lockMargin - 16
        active: root.active && Media.player !== null
        opacity: root.progress

        sourceComponent: LockMedia {
            player: Media.player
        }
    }

    Column {
        anchors.right: parent.right
        anchors.bottom: parent.bottom
        anchors.margins: Theme.lockMargin
        spacing: 12
        visible: root.active
        opacity: root.progress

        transform: Translate {
            x: root.slide
        }

        Row {
            anchors.right: parent.right
            spacing: 12

            Column {
                anchors.verticalCenter: parent.verticalCenter
                spacing: 2

                Text {
                    anchors.right: parent.right
                    text: root.user
                    color: Theme.text
                    font.pixelSize: Theme.fontContent + 2
                    font.weight: Font.DemiBold
                }

                Row {
                    anchors.right: parent.right
                    spacing: 6

                    Glyph {
                        anchors.verticalCenter: parent.verticalCenter
                        path: Glyphs.lock
                        size: 12
                        color: Theme.subtle
                    }

                    Text {
                        text: "locked"
                        color: Theme.subtle
                        font.pixelSize: Theme.fontCallout
                    }
                }
            }

            Avatar {
                user: root.user
                size: 48
            }
        }

        PasswordField {
            id: field

            focus: root.active
            busy: root.busy
            onSubmitted: password => root.submitted(password)
        }

        Text {
            anchors.right: parent.right
            text: root.error
            color: Theme.danger
            font.pixelSize: Theme.fontCallout
            opacity: root.error !== "" ? 1 : 0
        }
    }
}
