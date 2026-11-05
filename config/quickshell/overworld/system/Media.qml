pragma Singleton

import Quickshell
import Quickshell.Services.Mpris
import QtQuick

Singleton {
    readonly property var players: Mpris.players.values
    readonly property MprisPlayer player: players.find(p => p.isPlaying) ?? players[0] ?? null
}
