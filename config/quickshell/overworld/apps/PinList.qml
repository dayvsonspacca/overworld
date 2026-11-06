import Quickshell
import QtQuick
import qs.daemon

Scope {
    id: root

    required property string place
    property list<string> ids
    readonly property list<DesktopEntry> entries: ids.map(id => Apps.byId(id)).filter(e => e)

    function has(id: string): bool {
        return ids.includes(id);
    }

    function toggle(id: string): void {
        Daemon.call(has(id) ? "pins-remove" : "pins-add", { place, app: id }, result => root.ids = result);
    }

    function load(): void {
        if (Daemon.connected)
            Daemon.call("pins-list", { place }, result => root.ids = result);
    }

    Component.onCompleted: load()

    Connections {
        target: Daemon

        function onConnectedChanged() {
            root.load();
        }
    }

    Connections {
        target: Beacon

        function onLit(name: string): void {
            if (name === "pins")
                root.load();
        }
    }
}
