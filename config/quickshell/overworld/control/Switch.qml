import QtQuick
import qs.theme

Rectangle {
    id: root

    property bool checked

    signal toggled

    implicitWidth: 36
    implicitHeight: 20
    radius: height / 2
    color: checked ? Theme.accent : Theme.track
    activeFocusOnTab: true
    Keys.onReturnPressed: toggled()
    Keys.onSpacePressed: toggled()

    Behavior on color {
        ColorAnimation {
            duration: 120
        }
    }

    Rectangle {
        x: root.checked ? root.width - width - 3 : 3
        anchors.verticalCenter: parent.verticalCenter
        width: 14
        height: 14
        radius: 7
        color: Theme.text

        Behavior on x {
            NumberAnimation {
                duration: 120
                easing.type: Easing.OutCubic
            }
        }
    }

    FocusRing {
        radius: root.radius + gap
    }

    MouseArea {
        anchors.fill: parent
        cursorShape: Qt.PointingHandCursor
        onClicked: root.toggled()
    }
}
