import QtQuick
import qs.theme

Item {
    id: root

    property real value
    readonly property real shown: drag.pressed ? drag.live : value

    signal moved(real value)

    implicitHeight: 20

    Rectangle {
        anchors.verticalCenter: parent.verticalCenter
        width: parent.width
        height: 4
        radius: 2
        color: Theme.track

        Rectangle {
            width: root.shown * parent.width
            height: parent.height
            radius: 2
            color: Theme.accentLight
        }
    }

    Rectangle {
        x: root.shown * root.width - width / 2
        anchors.verticalCenter: parent.verticalCenter
        width: 16
        height: 16
        radius: 8
        color: Theme.accentLight
        border.width: 3
        border.color: Theme.raised
    }

    MouseArea {
        id: drag

        property real live

        function at(x: real): real {
            return Math.max(0, Math.min(1, x / root.width));
        }

        anchors.fill: parent
        cursorShape: Qt.PointingHandCursor
        onPressed: event => {
            live = at(event.x);
            root.moved(live);
        }
        onPositionChanged: event => {
            if (!pressed)
                return;
            live = at(event.x);
            root.moved(live);
        }
        onWheel: event => root.moved(Math.max(0, Math.min(1, root.value + (event.angleDelta.y > 0 ? 0.05 : -0.05))))
    }
}
