pragma Singleton

import Quickshell
import QtQuick
import qs.apps
import qs.daemon

// Notices Overworld's apps ring through the daemon, which keeps them until they are answered or withdrawn.
Singleton {
    id: root

    property var waiting: []
    property var popups: []
    property var seen: ({})
    property bool loaded
    readonly property int count: waiting.length

    function refresh(): void {
        Daemon.call("bell-list", {}, listed => {
            // Keeping the objects already known keeps the toasts and cards showing them, and what was picked in them.
            const known = new Map(root.waiting.map(n => [n.id, n]));
            const notices = listed.map(n => known.get(n.id) ?? n);
            const ids = new Set(notices.map(n => n.id));
            const fresh = root.loaded ? notices.filter(n => !root.seen[n.id]) : [];
            for (const n of notices)
                root.seen[n.id] = true;
            root.popups = [
                ...root.popups.map(p => ids.has(p.notice.id) || p.said !== "" ? p : { notice: p.notice, said: `Answered in ${root.app(p.notice)}` }),
                ...fresh.filter(n => !n.quiet && !Notifications.doNotDisturb).map(n => ({ notice: n, said: "" }))
            ];
            root.waiting = notices;
            root.loaded = true;
        });
    }

    function answer(notice: var, reply: var, said: string): void {
        Daemon.call("bell-answer", { id: notice.id, reply }, () => {});
        root.waiting = root.waiting.filter(n => n.id !== notice.id);
        root.popups = root.popups.map(p => p.notice.id === notice.id ? { notice: p.notice, said } : p);
    }

    function open(notice: var): void {
        Daemon.call("bell-open", { id: notice.id }, () => {});
        hide(notice);
    }

    function hide(notice: var): void {
        popups = popups.filter(p => p.notice.id !== notice.id);
    }

    function app(notice: var): string {
        return Apps.byId(notice.from)?.name ?? notice.from;
    }

    function icon(notice: var): string {
        return Apps.icon(notice.from);
    }

    function time(notice: var): string {
        return new Date(notice.rung).toLocaleTimeString(Qt.locale(), Locale.ShortFormat);
    }

    Connections {
        target: Beacon

        function onLit(name: string): void {
            if (name === "bell")
                root.refresh();
        }
    }

    Connections {
        target: Daemon

        function onConnectedChanged(): void {
            if (Daemon.connected)
                root.refresh();
        }
    }

    Component.onCompleted: refresh()
}
