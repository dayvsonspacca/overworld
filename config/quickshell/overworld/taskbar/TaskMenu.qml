import Quickshell
import Quickshell.Hyprland
import QtQuick
import qs.apps
import qs.icons
import qs.popups
import qs.theme

PopupWindow {
    id: root

    property Item target
    readonly property var group: target?.group ?? null
    readonly property var options: {
        const app = group;
        if (!app)
            return [];

        const count = app.windows.length;
        const options = [];
        if (app.entry) {
            const pinned = Pins.bar.has(app.id);
            options.push({ label: "New window", glyph: Glyphs.plus, run: () => app.entry.execute() });
            options.push({ label: pinned ? "Unpin from taskbar" : "Pin to taskbar", glyph: pinned ? Glyphs.unpin : Glyphs.pin, run: () => Pins.bar.toggle(app.id) });
        }
        if (count > 0)
            options.push({ label: count === 1 ? "Close window" : `Close ${count} windows`, glyph: Glyphs.close, run: () => app.windows.forEach(w => w.close()), apart: options.length > 0 });
        return options;
    }

    function open(button: Item): void {
        target = button;
    }

    function close(): void {
        target = null;
    }

    function run(option: var): void {
        close();
        option.run();
    }

    visible: group !== null
    color: "transparent"
    implicitWidth: 220
    implicitHeight: card.implicitHeight
    anchor.item: target
    anchor.rect.x: 0
    anchor.rect.y: -Theme.previewGap
    anchor.rect.w: target?.width ?? 0
    anchor.rect.h: target?.height ?? 0
    anchor.edges: Edges.Top
    anchor.gravity: Edges.Top

    HyprlandFocusGrab {
        active: root.visible
        windows: [root]
        onCleared: root.close()
    }

    Rectangle {
        id: card

        anchors.fill: parent
        implicitHeight: content.implicitHeight + 12
        radius: Theme.panelRadius
        color: Theme.background
        border.width: 1
        border.color: Theme.edge

        Column {
            id: content

            anchors.top: parent.top
            anchors.left: parent.left
            anchors.right: parent.right
            anchors.margins: 6
            spacing: 2

            Item {
                width: parent.width
                height: 38

                Image {
                    id: icon

                    anchors.left: parent.left
                    anchors.leftMargin: 10
                    anchors.verticalCenter: parent.verticalCenter
                    width: 18
                    height: 18
                    sourceSize: Qt.size(width, height)
                    source: root.group ? Tasks.icon(root.group) : ""
                }

                Text {
                    anchors.left: icon.right
                    anchors.leftMargin: 10
                    anchors.right: parent.right
                    anchors.rightMargin: 10
                    anchors.verticalCenter: parent.verticalCenter
                    text: root.group ? Tasks.name(root.group) : ""
                    color: Theme.text
                    font.pixelSize: Theme.fontSize - 1
                    font.weight: Font.DemiBold
                    elide: Text.ElideRight
                }
            }

            Rectangle {
                width: parent.width
                height: 1
                color: Theme.edge
            }

            Repeater {
                model: root.options

                MenuOption {
                    required property var modelData

                    width: content.width
                    label: modelData.label
                    glyph: modelData.glyph
                    apart: modelData.apart === true
                    onClicked: root.run(modelData)
                }
            }
        }
    }
}
