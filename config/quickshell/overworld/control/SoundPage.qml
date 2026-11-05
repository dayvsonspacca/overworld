import Quickshell.Services.Pipewire
import QtQuick
import qs.icons
import qs.system
import qs.theme

Column {
    spacing: 14

    PageTitle {
        text: "Sound"
    }

    Loader {
        width: parent.width
        active: Media.player !== null

        sourceComponent: MediaCard {
            player: Media.player
        }
    }

    VolumeRow {
        width: parent.width
        node: Audio.sink
    }

    Column {
        width: parent.width
        spacing: 6

        SectionLabel {
            text: "Output"
        }

        Repeater {
            model: Audio.sinks

            OptionRow {
                required property PwNode modelData

                width: parent.width
                glyph: Glyphs.volume
                label: modelData.description || modelData.name
                selected: modelData === Audio.sink
                onClicked: Audio.use(modelData)
            }
        }
    }

    Column {
        width: parent.width
        spacing: 14

        SectionLabel {
            text: "Apps"
        }

        Repeater {
            model: Audio.streams

            AppVolumeRow {
                required property PwNode modelData

                width: parent.width
                node: modelData
            }
        }

        Text {
            text: "No apps playing sound"
            color: Theme.subtle
            font.pixelSize: Theme.fontSmall
            visible: Audio.streams.length === 0
        }
    }
}
