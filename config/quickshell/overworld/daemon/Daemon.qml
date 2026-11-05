pragma Singleton

import Quickshell
import Quickshell.Io
import QtQuick

Singleton {
    id: root

    readonly property bool connected: socket.connected
    // Same place as SocketPath in Overworld.Redstone: the runtime dir when it is a full path, the temp dir otherwise.
    readonly property string runtime: {
        const xdg = Quickshell.env("XDG_RUNTIME_DIR") ?? "";
        return xdg.startsWith("/") ? xdg : (Quickshell.env("TMPDIR") || "/tmp");
    }
    property var callbacks: []

    function call(operation: string, input: var, done: var): void {
        if (!socket.connected)
            return;

        callbacks.push(done);
        socket.write(`${JSON.stringify({ operation, input })}\n`);
        socket.flush();
    }

    Socket {
        id: socket

        path: `${root.runtime}/overworld.sock`
        connected: true
        onConnectedChanged: root.callbacks = []

        parser: SplitParser {
            onRead: line => {
                const done = root.callbacks.shift();
                const response = JSON.parse(line);
                if (response.error)
                    console.warn(`overworld daemon: ${response.error.code}: ${response.error.message}`);
                else
                    done(response.result);
            }
        }
    }
}
