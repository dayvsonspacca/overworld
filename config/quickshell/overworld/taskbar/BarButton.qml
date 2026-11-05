import QtQuick
import qs.theme

Item {
    id: root

    default property alias content: body.data
    property bool active
    readonly property alias hovered: mouse.containsMouse

    signal clicked
    signal middleClicked
    signal contextRequested

    width: Theme.tileSize
    height: Theme.tileSize

    Rectangle {
        anchors.fill: parent
        radius: Theme.radiusSmall
        color: root.active
            ? (root.hovered ? Theme.activeHoverTint : Theme.activeTint)
            : (root.hovered ? Theme.hoverTint : "transparent")

        Behavior on color {
            ColorAnimation {
                duration: 100
            }
        }
    }

    Item {
        id: body

        anchors.fill: parent
    }

    MouseArea {
        id: mouse

        anchors.fill: parent
        hoverEnabled: true
        cursorShape: Qt.PointingHandCursor
        acceptedButtons: Qt.LeftButton | Qt.MiddleButton | Qt.RightButton
        onClicked: event => {
            if (event.button === Qt.RightButton)
                root.contextRequested();
            else if (event.button === Qt.MiddleButton)
                root.middleClicked();
            else
                root.clicked();
        }
    }
}
