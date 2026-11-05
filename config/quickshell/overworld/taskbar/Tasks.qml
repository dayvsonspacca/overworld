pragma Singleton

import Quickshell
import Quickshell.Wayland
import QtQuick
import qs.apps

Singleton {
    readonly property var groups: {
        const groups = Pins.bar.entries.map(entry => ({ id: entry.id, entry, windows: [] }));
        for (const window of ToplevelManager.toplevels.values) {
            const entry = Apps.entry(window.appId);
            const id = entry?.id ?? window.appId;
            let group = groups.find(g => g.id === id);
            if (!group) {
                group = { id, entry, windows: [] };
                groups.push(group);
            }
            group.windows.push(window);
        }
        return groups;
    }

    function name(group: var): string {
        return group.entry?.name ?? group.id;
    }

    function icon(group: var): string {
        return Apps.iconSource(group.entry?.icon ?? group.id);
    }

    function focus(group: var): void {
        const windows = group.windows;
        if (windows.length === 0) {
            group.entry?.execute();
            return;
        }
        const current = windows.findIndex(w => w.activated);
        windows[(current + 1) % windows.length].activate();
    }
}
