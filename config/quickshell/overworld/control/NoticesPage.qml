import Quickshell.Services.Notifications
import QtQuick
import qs.notifications
import qs.theme

Column {
    id: root

    readonly property var groups: {
        const map = new Map();
        for (const n of Notifications.all) {
            const app = n.appName || "System";
            if (!map.has(app))
                map.set(app, []);
            map.get(app).push(n);
        }
        return [...map].map(([app, items]) => ({ app, items }));
    }

    spacing: 10

    Item {
        width: parent.width
        height: 24

        PageTitle {
            anchors.verticalCenter: parent.verticalCenter
            text: "Notifications"
        }

        Text {
            id: clear

            anchors.right: parent.right
            anchors.verticalCenter: parent.verticalCenter
            text: "Clear all"
            color: Theme.accentLight
            font.pixelSize: Theme.fontCallout
            visible: Notifications.count > 0
            activeFocusOnTab: visible
            Keys.onReturnPressed: Notifications.clear()
            Keys.onSpacePressed: Notifications.clear()

            FocusRing {
                radius: Theme.radiusSmall + gap
            }

            MouseArea {
                anchors.fill: parent
                cursorShape: Qt.PointingHandCursor
                onClicked: Notifications.clear()
            }
        }
    }

    Item {
        width: parent.width
        height: 24

        Text {
            anchors.verticalCenter: parent.verticalCenter
            text: "Do not disturb"
            color: Theme.text
            font.pixelSize: Theme.fontCallout
        }

        Switch {
            anchors.right: parent.right
            anchors.verticalCenter: parent.verticalCenter
            checked: Notifications.doNotDisturb
            onToggled: Notifications.doNotDisturb = !Notifications.doNotDisturb
        }
    }

    Repeater {
        model: root.groups

        Column {
            id: group

            required property var modelData

            width: root.width
            spacing: 8

            Row {
                spacing: 8
                topPadding: 4

                Image {
                    anchors.verticalCenter: parent.verticalCenter
                    width: 14
                    height: 14
                    sourceSize: Qt.size(width, height)
                    source: Notifications.icon(group.modelData.items[0])
                }

                SectionLabel {
                    anchors.verticalCenter: parent.verticalCenter
                    text: `${group.modelData.app} · ${group.modelData.items.length}`
                }
            }

            Repeater {
                model: group.modelData.items

                NoticeCard {
                    required property Notification modelData

                    width: group.width
                    notification: modelData
                }
            }
        }
    }

    Text {
        topPadding: 24
        width: parent.width
        horizontalAlignment: Text.AlignHCenter
        text: "No notifications"
        color: Theme.subtle
        font.pixelSize: Theme.fontCallout
        visible: Notifications.count === 0
    }
}
