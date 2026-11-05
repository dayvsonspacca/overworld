pragma Singleton

import Quickshell
import QtQuick
import Qt.labs.folderlistmodel

Singleton {
    id: root

    readonly property string config: Quickshell.env("XDG_CONFIG_HOME") || `${Quickshell.env("HOME")}/.config`
    readonly property url source: files.count > 0 ? files.get(0, "fileUrl") : ""

    FolderListModel {
        id: files

        folder: `file://${root.config}/overworld`
        nameFilters: ["wallpaper", "wallpaper.*"]
        showDirs: false
    }
}
