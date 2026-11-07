import Quickshell
import Quickshell.Hyprland
import Quickshell.Wayland
import QtQuick
import qs.icons
import qs.system
import qs.theme

PanelWindow {
    id: root

    property bool suppressed
    // Pipewire and the backlight report their first values once the shell starts; those aren't changes.
    property bool settled
    property string kind
    property bool shown
    readonly property bool mic: kind === "mic"
    readonly property real level: kind === "brightness" ? Brightness.level : Audio.volume
    readonly property string glyph: {
        if (kind === "brightness")
            return Glyphs.sun;
        if (mic)
            return Audio.micMuted ? Glyphs.micOff : Glyphs.mic;
        return Audio.glyph;
    }

    function show(what: string): void {
        if (suppressed || !settled)
            return;
        kind = what;
        shown = true;
        timeout.restart();
    }

    screen: Quickshell.screens.find(s => s.name === Hyprland.focusedMonitor?.name) ?? null
    visible: pill.opacity > 0
    implicitWidth: 300
    implicitHeight: 48
    color: "transparent"
    exclusiveZone: 0
    mask: Region {}
    WlrLayershell.namespace: "overworld:osd"
    WlrLayershell.layer: WlrLayer.Overlay
    WlrLayershell.keyboardFocus: WlrKeyboardFocus.None

    anchors.bottom: true

    margins.bottom: Theme.gap * 2

    Connections {
        target: Audio.sink?.audio ?? null

        function onVolumeChanged(): void {
            root.show("volume");
        }

        function onMutedChanged(): void {
            root.show("volume");
        }
    }

    Connections {
        target: Audio.source?.audio ?? null

        function onMutedChanged(): void {
            root.show("mic");
        }
    }

    Connections {
        target: Brightness

        function onAdjusted(): void {
            root.show("brightness");
        }
    }

    Timer {
        interval: 2000
        running: true
        onTriggered: root.settled = true
    }

    Timer {
        id: timeout

        interval: 1500
        onTriggered: root.shown = false
    }

    Rectangle {
        id: pill

        width: parent.width
        height: parent.height
        y: root.shown ? 0 : 8
        opacity: root.shown ? 1 : 0
        radius: height / 2
        color: Theme.raised
        border.width: 1
        border.color: Theme.border

        Behavior on opacity {
            NumberAnimation { duration: 150 }
        }

        Behavior on y {
            NumberAnimation { duration: 150; easing.type: Easing.OutCubic }
        }

        Glyph {
            id: icon

            anchors.left: parent.left
            anchors.leftMargin: 18
            anchors.verticalCenter: parent.verticalCenter
            path: root.glyph
            size: 18
            color: root.mic ? (Audio.micMuted ? Theme.danger : Theme.text) : root.kind === "volume" && Audio.muted ? Theme.subtle : Theme.text
        }

        Text {
            anchors.left: icon.right
            anchors.leftMargin: 14
            anchors.verticalCenter: parent.verticalCenter
            text: Audio.micMuted ? "Microphone off" : "Microphone on"
            color: Theme.text
            font.pixelSize: Theme.fontCallout + 1
            visible: root.mic
        }

        Rectangle {
            anchors.left: icon.right
            anchors.leftMargin: 14
            anchors.right: percent.left
            anchors.rightMargin: 14
            anchors.verticalCenter: parent.verticalCenter
            height: 4
            radius: 2
            color: Theme.track
            visible: !root.mic

            Rectangle {
                width: parent.width * root.level
                height: parent.height
                radius: 2
                color: root.kind === "volume" && Audio.muted ? Theme.subtle : Theme.accentLight

                Behavior on width {
                    NumberAnimation { duration: 80 }
                }
            }
        }

        Text {
            id: percent

            anchors.right: parent.right
            anchors.rightMargin: 20
            anchors.verticalCenter: parent.verticalCenter
            width: 34
            horizontalAlignment: Text.AlignRight
            text: `${Math.round(root.level * 100)}%`
            color: root.kind === "volume" && Audio.muted ? Theme.subtle : Theme.text
            font.pixelSize: Theme.fontCallout
            font.features: { "tnum": 1 }
            visible: !root.mic
        }
    }
}
