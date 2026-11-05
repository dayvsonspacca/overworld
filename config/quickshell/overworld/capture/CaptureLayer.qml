import Quickshell
import Quickshell.Io
import QtQuick

Scope {
    IpcHandler {
        target: "capture"

        function open(): void {
            Capture.open();
        }

        function screen(): void {
            Capture.shoot();
        }
    }

    LazyLoader {
        active: Capture.active

        Variants {
            model: Quickshell.screens

            CaptureOverlay {}
        }
    }
}
