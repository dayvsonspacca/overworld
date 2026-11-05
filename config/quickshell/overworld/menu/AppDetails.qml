import Quickshell
import QtQuick
import qs.apps
import qs.theme

Rectangle {
    id: root

    required property DesktopEntry entry
    property bool focused
    property int selected
    readonly property var actions: [
        { label: "Open", run: () => entry.execute(), closes: true },
        ...entry.actions.map(a => ({ label: a.name, run: () => a.execute(), closes: true })),
        { label: Pins.menu.has(entry.id) ? "Unpin from Start" : "Pin to Start", run: () => Pins.menu.toggle(entry.id), closes: false },
        { label: Pins.bar.has(entry.id) ? "Unpin from taskbar" : "Pin to taskbar", run: () => Pins.bar.toggle(entry.id), closes: false }
    ]

    signal done

    function move(delta: int): void {
        selected = Math.max(0, Math.min(actions.length - 1, selected + delta));
    }

    function trigger(): void {
        run(actions[selected]);
    }

    function run(action: var): void {
        action.run();
        if (action.closes)
            done();
    }

    component ActionRow: Item {
        id: action

        property string label
        property bool highlighted

        signal clicked

        implicitHeight: 38

        Rectangle {
            anchors.fill: parent
            radius: Theme.radiusControl
            color: action.highlighted || mouse.containsMouse ? Theme.surface : "transparent"
        }

        Text {
            anchors.left: parent.left
            anchors.leftMargin: 12
            anchors.verticalCenter: parent.verticalCenter
            text: action.label
            color: Theme.text
            font.pixelSize: Theme.fontContent - 1
        }

        MouseArea {
            id: mouse

            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: action.clicked()
        }
    }

    onEntryChanged: selected = 0
    onFocusedChanged: selected = 0

    color: Theme.raised
    radius: Theme.radiusPanel
    border.width: 1
    border.color: focused ? Theme.accent : Theme.divider

    Column {
        anchors.fill: parent
        anchors.margins: 20
        anchors.topMargin: 28

        Image {
            anchors.horizontalCenter: parent.horizontalCenter
            width: Theme.detailIconSize
            height: Theme.detailIconSize
            sourceSize: Qt.size(width, height)
            source: Apps.iconSource(root.entry.icon)
        }

        Text {
            width: parent.width
            topPadding: 14
            horizontalAlignment: Text.AlignHCenter
            text: root.entry.name
            color: Theme.text
            font.pixelSize: Theme.fontTitle2
            font.weight: Font.DemiBold
            elide: Text.ElideRight
        }

        Text {
            width: parent.width
            topPadding: 4
            bottomPadding: 18
            horizontalAlignment: Text.AlignHCenter
            text: ["App", Apps.category(root.entry)].filter(s => s).join(" · ")
            color: Theme.subtle
            font.pixelSize: Theme.fontCallout
        }

        Rectangle {
            width: parent.width
            height: 1
            color: Theme.divider
        }

        Item {
            width: 1
            height: 8
        }

        Repeater {
            model: root.actions

            ActionRow {
                required property var modelData
                required property int index

                width: parent.width
                label: modelData.label
                highlighted: index === root.selected
                onClicked: root.run(modelData)
            }
        }
    }
}
