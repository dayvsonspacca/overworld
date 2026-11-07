import QtQuick
import qs.icons
import qs.notifications
import qs.system
import qs.theme

Rectangle {
    id: root

    property string current
    readonly property var pages: [
        { id: "general", glyph: Network.glyph },
        { id: "sound", glyph: Audio.glyph },
        { id: "notices", glyph: Glyphs.bell, badge: Notifications.count },
        { id: "agenda", glyph: Glyphs.calendar },
        { id: "tray", glyph: Glyphs.tray }
    ]

    signal selected(string page)

    function shift(delta: int): void {
        const index = pages.findIndex(p => p.id === current);
        const next = Math.max(0, Math.min(pages.length - 1, index + delta));
        selected(pages[next].id);
    }

    width: Theme.railWidth
    color: Qt.alpha(Theme.text, 0.02)

    Column {
        anchors.top: parent.top
        anchors.topMargin: 12
        anchors.horizontalCenter: parent.horizontalCenter
        spacing: 4

        Repeater {
            model: root.pages

            RailButton {
                required property var modelData

                glyph: modelData.glyph
                selected: modelData.id === root.current
                focused: selected && root.activeFocus
                badge: modelData.badge ?? 0
                onClicked: root.selected(modelData.id)
            }
        }
    }
}
