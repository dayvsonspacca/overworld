pragma Singleton

import Quickshell
import Quickshell.Services.Notifications
import QtQuick
import qs.apps

Singleton {
    id: root

    property bool doNotDisturb
    property var popups: []
    property var arrivals: ({})
    readonly property var all: [...server.trackedNotifications.values].reverse()
    readonly property int count: server.trackedNotifications.values.length

    function hide(n: Notification): void {
        popups = popups.filter(p => p !== n);
        if (n.transient)
            n.dismiss();
    }

    function dismiss(n: Notification): void {
        popups = popups.filter(p => p !== n);
        n.dismiss();
    }

    function activate(n: Notification): void {
        n.actions.find(a => a.identifier === "default")?.invoke();
        dismiss(n);
    }

    function clear(): void {
        popups = [];
        for (const n of [...server.trackedNotifications.values])
            n.dismiss();
    }

    function time(n: Notification): string {
        return arrivals[n.id]?.toLocaleTimeString(Qt.locale(), Locale.ShortFormat) ?? "";
    }

    function app(n: Notification): DesktopEntry {
        const name = n.appName.toLowerCase();
        return Apps.byId(n.desktopEntry) ?? Apps.all.find(e => e.name.toLowerCase() === name) ?? Apps.entry(n.appName);
    }

    function icon(n: Notification): string {
        const name = n.appIcon || app(n)?.icon || "";
        if (name.startsWith("file://"))
            return name;
        if (name.startsWith("/"))
            return `file://${name}`;
        return Apps.iconSource(name || Apps.fallbackIcon);
    }

    NotificationServer {
        id: server

        keepOnReload: true
        persistenceSupported: true
        bodySupported: true
        actionsSupported: true
        imageSupported: true

        onNotification: n => {
            n.tracked = true;
            root.arrivals[n.id] = new Date();
            n.closed.connect(() => root.popups = root.popups.filter(p => p !== n));
            if (!root.doNotDisturb)
                root.popups = [...root.popups, n];
        }
    }
}
