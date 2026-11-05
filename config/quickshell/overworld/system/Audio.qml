pragma Singleton

import Quickshell
import Quickshell.Services.Pipewire
import QtQuick
import qs.apps
import qs.icons

Singleton {
    id: root

    readonly property PwNode sink: Pipewire.defaultAudioSink
    readonly property real volume: sink?.audio?.volume ?? 0
    readonly property bool muted: sink?.audio?.muted ?? false
    readonly property list<PwNode> sinks: Pipewire.nodes.values.filter(n => n.audio && n.isSink && !n.isStream)
    readonly property list<PwNode> streams: Pipewire.nodes.values.filter(n => n.audio && n.isSink && n.isStream)
    readonly property string glyph: muted ? Glyphs.muted : volume < 0.34 ? Glyphs.volumeLow : Glyphs.volume

    function setVolume(node: PwNode, value: real): void {
        if (!node?.audio)
            return;

        node.audio.muted = false;
        node.audio.volume = Math.max(0, Math.min(1, value));
    }

    function toggleMute(node: PwNode): void {
        if (node?.audio)
            node.audio.muted = !node.audio.muted;
    }

    function use(node: PwNode): void {
        Pipewire.preferredDefaultAudioSink = node;
    }

    function entry(node: PwNode): DesktopEntry {
        return Apps.entry(node.properties["application.process.binary"] ?? node.name) ?? Apps.entry(node.name);
    }

    function label(node: PwNode): string {
        return entry(node)?.name || node.properties["application.name"] || node.name;
    }

    function icon(node: PwNode): string {
        const name = node.properties["application.icon-name"] ?? entry(node)?.icon ?? node.properties["application.process.binary"] ?? node.name;
        return Quickshell.iconPath(name, true);
    }

    PwObjectTracker {
        objects: [root.sink, ...root.sinks, ...root.streams]
    }
}
