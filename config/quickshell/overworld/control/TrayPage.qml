import Quickshell.Services.SystemTray
import QtQuick
import qs.theme

Item {
    id: root

    property var stack: []
    readonly property var level: stack[stack.length - 1] ?? null
    readonly property bool alive: !level || SystemTray.items.values.includes(level.item)

    signal done

    function typing(): bool {
        for (let i = Window.activeFocusItem; i; i = i.parent) {
            if (i === root)
                return true;
        }
        return false;
    }

    function open(item: var, handle: var, title: string): void {
        const typed = typing();
        stack = [...stack, { item, handle, title }];
        if (typed)
            Qt.callLater(menu.enter);
    }

    function back(): void {
        const typed = typing();
        const from = level?.item;
        stack = stack.slice(0, -1);
        if (!typed)
            return;
        Qt.callLater(() => {
            if (level)
                menu.enter();
            else
                focusRow(from);
        });
    }

    function focusRow(item: var): void {
        for (let i = 0; i < rows.count; i++) {
            const row = rows.itemAt(i);
            if (row?.item === item)
                row.forceActiveFocus();
        }
    }

    onAliveChanged: {
        if (!alive)
            stack = [];
    }

    implicitHeight: level ? menu.implicitHeight : list.implicitHeight

    Column {
        id: list

        width: parent.width
        spacing: 8
        visible: !root.level

        PageTitle {
            text: "Background apps"
            bottomPadding: 6
        }

        Repeater {
            id: rows

            model: SystemTray.items

            TrayRow {
                required property var modelData

                width: parent.width
                item: modelData
                onInspect: root.open(modelData, modelData.menu, title)
            }
        }

        Text {
            text: "No apps in the background"
            color: Theme.subtle
            font.pixelSize: Theme.fontSmall
            visible: SystemTray.items.values.length === 0
        }
    }

    TrayMenu {
        id: menu

        width: parent.width
        visible: root.level !== null
        level: root.level
        onBack: root.back()
        onOpened: (entry, title) => root.open(root.level.item, entry, title)
        onTriggered: root.done()
    }
}
