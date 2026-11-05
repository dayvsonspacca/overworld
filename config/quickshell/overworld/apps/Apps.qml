pragma Singleton

import Quickshell
import QtQuick

Singleton {
    readonly property string fallbackIcon: "application-x-executable"
    readonly property bool loaded: DesktopEntries.applications.values.length > 0
    readonly property list<DesktopEntry> all: [...DesktopEntries.applications.values]
        .filter(e => !e.noDisplay)
        .sort((a, b) => a.name.localeCompare(b.name))

    readonly property var categories: ({
        Development: "Development",
        Network: "Internet",
        AudioVideo: "Multimedia",
        Audio: "Multimedia",
        Video: "Multimedia",
        Office: "Office",
        Graphics: "Graphics",
        Game: "Games",
        Education: "Education",
        Science: "Science",
        Settings: "Settings",
        System: "System",
        Utility: "Utilities"
    })

    function entry(appId: string): DesktopEntry {
        return loaded ? DesktopEntries.heuristicLookup(appId) : null;
    }

    function byId(id: string): DesktopEntry {
        return loaded ? DesktopEntries.byId(id) : null;
    }

    function icon(appId: string): string {
        return iconSource(entry(appId)?.icon ?? appId);
    }

    function iconSource(name: string): string {
        return Quickshell.iconPath(name, true) || Quickshell.iconPath(fallbackIcon);
    }

    function category(e: DesktopEntry): string {
        const known = e.categories.find(c => c in categories);
        return known ? categories[known] : "";
    }

    function search(query: string): list<DesktopEntry> {
        const q = query.trim().toLowerCase();
        if (!q)
            return all;

        const rank = e => {
            const name = e.name.toLowerCase();
            if (name.startsWith(q))
                return 0;
            if (name.split(/\s+/).some(w => w.startsWith(q)))
                return 1;
            if (name.includes(q))
                return 2;
            return [e.genericName, ...e.keywords].some(s => s.toLowerCase().includes(q)) ? 3 : -1;
        };

        return all
            .map(e => ({ e, r: rank(e) }))
            .filter(m => m.r >= 0)
            .sort((a, b) => a.r - b.r || a.e.name.localeCompare(b.e.name))
            .map(m => m.e);
    }
}
