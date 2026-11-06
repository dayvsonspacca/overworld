pragma Singleton

import Quickshell
import Quickshell.Io
import QtQuick

Singleton {
    id: root

    readonly property string logo: Qt.resolvedUrl("../brand/grass.png").toString().replace("file://", "")
    readonly property string share: 'wl-copy --type image/png < "$1"; action=$(notify-send -a Overworld -i "$2" -h "string:image-path:$1" -h boolean:x-overworld-preview:true -A open=Open -A folder="Show in folder" "Screenshot copied" "Saved to $3"); case "$action" in open) xdg-open "$1" ;; folder) xdg-open "$(dirname "$1")" ;; esac'
    property string pictures: `${Quickshell.env("HOME")}/Pictures`
    readonly property string folder: `${pictures}/Screenshots`
    readonly property string place: `${pictures.split("/").pop()}/Screenshots`
    property bool active
    property bool instant
    property string mode: "region"

    function open(): void {
        start(false);
    }

    function shoot(): void {
        start(true);
    }

    function start(now: bool): void {
        if (active)
            return;

        instant = now;
        mode = now ? "screen" : "region";
        prepare.running = true;
        active = true;
    }

    function cancel(): void {
        active = false;
    }

    function store(result: var): void {
        const path = `${folder}/Screenshot ${Qt.formatDateTime(new Date(), "yyyy-MM-dd HH-mm-ss")}.png`;
        const saved = result.saveToFile(path);
        active = false;

        if (saved)
            Quickshell.execDetached(["sh", "-c", share, "sh", path, logo, place]);
        else
            Quickshell.execDetached(["notify-send", "-a", "Overworld", "-i", logo, "Screenshot failed", `Couldn't save to ${place}`]);
    }

    Process {
        running: true
        command: ["xdg-user-dir", "PICTURES"]

        stdout: StdioCollector {
            onStreamFinished: {
                const dir = text.trim();
                if (dir !== "")
                    root.pictures = dir;
            }
        }
    }

    Process {
        id: prepare

        command: ["mkdir", "-p", root.folder]
    }
}
