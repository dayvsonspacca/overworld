import Quickshell
import QtQuick
import qs.icons
import qs.theme

Column {
    id: root

    property var level

    signal back
    signal opened(QsMenuEntry entry, string title)
    signal triggered

    function enter(): void {
        for (let i = 0; i < entries.count; i++) {
            const row = entries.itemAt(i);
            if (row?.activeFocusOnTab) {
                row.forceActiveFocus();
                return;
            }
        }
        exit.forceActiveFocus();
    }

    function run(entry: QsMenuEntry, title: string): void {
        if (entry.hasChildren) {
            opened(entry, title);
            return;
        }
        entry.triggered();
        triggered();
    }

    spacing: 2
    Keys.onLeftPressed: back()
    Keys.onEscapePressed: back()

    QsMenuOpener {
        id: opener

        menu: root.level?.handle ?? null
    }

    Row {
        spacing: 10
        bottomPadding: 8

        Rectangle {
            id: exit

            width: 28
            height: 28
            radius: Theme.radiusSmall
            activeFocusOnTab: true
            Keys.onReturnPressed: root.back()
            Keys.onSpacePressed: root.back()
            color: backMouse.containsMouse ? Theme.activeTint : Theme.hoverTint

            Glyph {
                anchors.centerIn: parent
                path: Glyphs.left
                size: 12
                color: Theme.text
            }

            FocusRing {
                radius: exit.radius + gap
            }

            MouseArea {
                id: backMouse

                anchors.fill: parent
                hoverEnabled: true
                cursorShape: Qt.PointingHandCursor
                onClicked: root.back()
            }
        }

        Image {
            anchors.verticalCenter: parent.verticalCenter
            width: 20
            height: 20
            sourceSize: Qt.size(width, height)
            source: root.level?.item.icon ?? ""
        }

        PageTitle {
            anchors.verticalCenter: parent.verticalCenter
            text: root.level?.title ?? ""
        }
    }

    Repeater {
        id: entries

        model: opener.children

        TrayEntry {
            required property QsMenuEntry modelData

            width: root.width
            entry: modelData
            onActivated: root.run(modelData, label)
        }
    }
}
