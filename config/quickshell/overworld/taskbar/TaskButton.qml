import QtQuick
import qs.theme

BarButton {
    id: root

    required property var group
    readonly property int count: group.windows.length
    readonly property int pills: Math.min(count, 3)

    active: group.windows.some(w => w.activated)
    onClicked: Tasks.focus(group)
    onMiddleClicked: group.entry?.execute()

    Image {
        anchors.centerIn: parent
        width: Theme.iconSize
        height: Theme.iconSize
        sourceSize: Qt.size(width, height)
        source: Tasks.icon(root.group)
    }

    Row {
        anchors.bottom: parent.bottom
        anchors.bottomMargin: 2
        anchors.horizontalCenter: parent.horizontalCenter
        spacing: 2

        Repeater {
            model: root.pills

            Rectangle {
                required property int index
                readonly property bool lead: root.active && index === 0

                width: root.pills === 1
                    ? (root.active ? Theme.pillActiveWidth : Theme.pillWidth)
                    : (lead ? Theme.pillActiveWidth - 4 : Theme.pillDotWidth)
                height: Theme.pillHeight
                radius: height / 2
                color: (root.pills === 1 ? root.active : lead) ? Theme.accentLight : Theme.subtle

                Behavior on width {
                    NumberAnimation {
                        duration: 150
                        easing.type: Easing.OutCubic
                    }
                }

                Behavior on color {
                    ColorAnimation {
                        duration: 150
                    }
                }
            }
        }
    }
}
