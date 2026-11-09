pragma Singleton

import Quickshell
import Quickshell.Io
import Quickshell.Services.Pam
import QtQuick

Singleton {
    id: root

    readonly property int transition: 400
    readonly property string user: Quickshell.env("USER") ?? ""
    readonly property string runtime: Quickshell.env("XDG_RUNTIME_DIR") ?? ""
    readonly property bool busy: pam.active || leaving.running
    property bool locked
    property bool unlocking
    property list<string> waiting
    property var frames: ({})
    property string error
    property string pending

    signal locking

    function snapshot(screen: ShellScreen): url {
        return frames[screen.name]?.url ?? "";
    }

    function lock(): void {
        if (locked || waiting.length > 0)
            return;

        error = "";
        frames = {};
        waiting = Quickshell.screens.map(s => s.name);
        locking();
        patience.restart();
    }

    function captured(screen: ShellScreen, frame: var): void {
        if (!waiting.includes(screen.name))
            return;

        if (frame)
            frames = Object.assign({}, frames, { [screen.name]: frame });
        waiting = waiting.filter(name => name !== screen.name);
        if (waiting.length === 0)
            finish();
    }

    function finish(): void {
        patience.stop();
        waiting = [];
        locked = true;
    }

    function unlock(password: string): void {
        if (busy || password === "")
            return;

        error = "";
        pending = password;
        pam.start();
    }

    onLockedChanged: memory.setText(locked ? "1" : "0")

    Timer {
        id: patience

        interval: 1500
        onTriggered: root.finish()
    }

    Timer {
        id: leaving

        interval: root.transition
        onTriggered: {
            root.locked = false;
            root.unlocking = false;
        }
    }

    PamContext {
        id: pam

        config: "login"
        onResponseRequiredChanged: {
            if (!responseRequired)
                return;
            respond(root.pending);
            root.pending = "";
        }
        onCompleted: result => {
            if (result === PamResult.Success) {
                root.unlocking = true;
                leaving.start();
            } else if (result === PamResult.MaxTries) {
                root.error = "Too many attempts. Wait a moment.";
            } else {
                root.error = "Wrong password. Try again.";
            }
        }
        onError: root.error = "Couldn't check the password."
    }

    FileView {
        id: memory

        path: `${root.runtime}/overworld-locked`
        blockLoading: true
        printErrors: false
        onLoaded: {
            if (text().trim() === "1")
                root.locked = true;
        }
    }
}
