pragma Singleton

import Quickshell
import QtQuick

Singleton {
    readonly property color background: "#1C1B21"
    readonly property color raised: "#25242B"
    readonly property color surface: "#2E2D35"
    readonly property color border: "#4A4952"
    readonly property color accentDeep: "#5B3A9C"
    readonly property color accent: "#8E62D1"
    readonly property color accentLight: "#C9A5F2"
    readonly property color text: "#E4E2DC"
    readonly property color subtle: "#8A8888"
    readonly property color divider: Qt.alpha(border, 0.6)

    readonly property color bar: Qt.alpha(background, 0.9)
    readonly property color edge: Qt.alpha("white", 0.07)
    readonly property color hoverTint: Qt.alpha("white", 0.06)
    readonly property color activeTint: Qt.alpha("white", 0.09)
    readonly property color activeHoverTint: Qt.alpha("white", 0.12)

    readonly property int fontSize: 14
    readonly property int fontSmall: 12
    readonly property int fontTitle: 17

    readonly property int barHeight: 48
    readonly property int barPadding: 8
    readonly property int tileSize: 40
    readonly property int tileRadius: 4
    readonly property int tileSpacing: 4
    readonly property int iconSize: 24
    readonly property int pillWidth: 6
    readonly property int pillActiveWidth: 16
    readonly property int pillDotWidth: 4
    readonly property int pillHeight: 3

    readonly property int previewWidth: 240
    readonly property int previewThumbHeight: 140
    readonly property int previewGap: 8

    readonly property int toastWidth: 360
    readonly property int toastLimit: 3
    readonly property int toastTimeout: 5000

    readonly property int gap: 12
    readonly property int panelRadius: 8
    readonly property int itemRadius: 6

    readonly property int controlWidth: 420
    readonly property int controlHeight: 560
    readonly property int controlPadding: 16
    readonly property int railWidth: 52
    readonly property int quickTileHeight: 46

    readonly property int menuWidth: 780
    readonly property int menuHeight: 580
    readonly property int menuListWidth: 280
    readonly property int menuPadding: 24
    readonly property int searchHeight: 36
    readonly property int rowHeight: 42
    readonly property int rowIconSize: 28
    readonly property int tileHeight: 84
    readonly property int tileIconSize: 32
    readonly property int detailIconSize: 64
    readonly property int footerHeight: 60
    readonly property int lockMargin: 56
    readonly property int lockClock: 132
    readonly property int lockFieldWidth: 300
}
