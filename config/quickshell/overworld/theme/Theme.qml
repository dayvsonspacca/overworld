pragma Singleton

import Quickshell
import Quickshell.Io
import QtQuick

// Colors, type sizes and radii come from the theme Seed installs; the measures below are the shell's own.
Singleton {
    id: root

    readonly property string path: {
        const xdg = Quickshell.env("XDG_CONFIG_HOME") ?? "";
        return `${xdg.startsWith("/") ? xdg : `${Quickshell.env("HOME")}/.config`}/overworld/theme.json`;
    }
    readonly property var theme: JSON.parse(file.text() || '{"colors":{},"font":{"sizes":{}},"radii":{}}')

    readonly property color background: theme.colors.background
    readonly property color raised: theme.colors.raised
    readonly property color surface: theme.colors.surface
    readonly property color divider: theme.colors.divider
    readonly property color border: theme.colors.border
    readonly property color text: theme.colors.text
    readonly property color subtle: theme.colors.subtle
    readonly property color accentDeep: theme.colors.accentDeep
    readonly property color accent: theme.colors.accent
    readonly property color accentLight: theme.colors.accentLight
    readonly property color danger: theme.colors.danger
    readonly property color edge: theme.colors.edge
    readonly property color hoverTint: theme.colors.hoverTint
    readonly property color activeTint: theme.colors.activeTint
    readonly property color activeHoverTint: theme.colors.activeHoverTint
    readonly property color bar: Qt.alpha(background, 0.9)

    readonly property int fontCallout: theme.font.sizes.callout
    readonly property int fontContent: theme.font.sizes.content
    readonly property int fontTitle2: theme.font.sizes.title2

    readonly property int radiusSmall: theme.radii.small
    readonly property int radiusControl: theme.radii.control
    readonly property int radiusPanel: theme.radii.panel

    readonly property int barHeight: 48
    readonly property int barPadding: 8
    readonly property int tileSize: 40
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

    FileView {
        id: file

        path: root.path
        blockLoading: true
        onLoadFailed: console.error(`overworld: no theme at ${root.path}; run just seed to install one`)
    }
}
