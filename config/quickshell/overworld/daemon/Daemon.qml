pragma Singleton

import Quickshell
import Quickshell.Io
import QtQuick

Singleton {
    id: root

    readonly property bool connected: socket.connected
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

        path: `${Quickshell.env("XDG_RUNTIME_DIR")}/overworld.sock`
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
