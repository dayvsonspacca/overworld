import Quickshell
import Quickshell.Wayland
import QtQuick

Loader {
    id: root

    required property ShellScreen screen

    active: false

    Connections {
        target: Locker

        function onLocking() {
            settle.restart();
        }
    }

    Timer {
        id: settle

        interval: 50
        onTriggered: root.active = true
    }

    sourceComponent: ScreencopyView {
        id: view

        function save(): void {
            const started = grabToImage(result => {
                Locker.captured(root.screen, result);
                root.active = false;
            });
            if (!started) {
                Locker.captured(root.screen, null);
                root.active = false;
            }
        }

        width: sourceSize.width
        height: sourceSize.height
        visible: false
        captureSource: root.screen
        live: false
        onHasContentChanged: {
            if (hasContent)
                Qt.callLater(view.save);
        }
    }
}
