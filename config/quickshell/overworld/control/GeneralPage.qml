import QtQuick
import qs.icons
import qs.system
import qs.theme

Column {
    id: root

    spacing: 14

    PageTitle {
        text: "General"
    }

    Grid {
        id: tiles

        readonly property real tileWidth: (width - spacing) / 2

        width: parent.width
        columns: 2
        spacing: 8

        QuickTile {
            width: tiles.tileWidth
            visible: Network.wired !== null
            glyph: Glyphs.wired
            title: "Wired"
            detail: Network.wiredConnected ? "Connected" : "Disconnected"
            on: Network.wiredConnected
            interactive: false
        }

        QuickTile {
            width: tiles.tileWidth
            visible: Network.wifi !== null
            glyph: Network.wifiEnabled ? Glyphs.wifi : Glyphs.offline
            title: "Wi‑Fi"
            detail: !Network.wifiEnabled ? "Off" : Network.wifiConnected ? (Network.wifiNetwork?.name ?? "Connected") : "Disconnected"
            on: Network.wifiEnabled
            interactive: Network.wifiAvailable
            onClicked: Network.setWifi(!Network.wifiEnabled)
        }

        QuickTile {
            width: tiles.tileWidth
            visible: NightLight.available
            glyph: Glyphs.sun
            title: "Night light"
            detail: NightLight.enabled ? "On" : "Off"
            on: NightLight.enabled
            onClicked: NightLight.enabled = !NightLight.enabled
        }
    }

    Column {
        width: parent.width
        spacing: 4

        VolumeRow {
            width: parent.width
            node: Audio.sink
        }

        Text {
            leftPadding: 32
            text: Audio.sink?.description ?? "No audio output"
            color: Theme.subtle
            font.pixelSize: Theme.fontCallout - 1
        }
    }

    Loader {
        width: parent.width
        active: Media.player !== null

        sourceComponent: MediaCard {
            player: Media.player
        }
    }
}
