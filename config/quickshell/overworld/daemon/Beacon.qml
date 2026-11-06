pragma Singleton

import Quickshell
import Quickshell.Io
import QtQuick

// The signals the daemon lights, like pins or theme; whoever cares rereads what the signal names when it is lit.
Singleton {
    id: root

    signal lit(string name)

    Socket {
        path: `${Daemon.runtime}/overworld-beacon.sock`
        connected: true

        parser: SplitParser {
            onRead: line => root.lit(JSON.parse(line).signal)
        }
    }
}
