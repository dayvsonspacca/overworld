import QtQuick
import qs.theme

Rectangle {
    id: root

    required property string text
    property bool primary

    signal clicked

    implicitWidth: label.implicitWidth + 28
    implicitHeight: 28
    activeFocusOnTab: true
    Keys.onReturnPressed: clicked()
    Keys.onSpacePressed: clicked()
    radius: Theme.radiusControl
    color: primary
        ? (mouse.containsMouse ? Theme.accentLight : Theme.accent)
        : (mouse.containsMouse ? Theme.activeTint : Theme.hoverTint)

    Text {
        id: label

        anchors.centerIn: parent
        text: root.text
        color: root.primary ? "white" : Theme.text
        font.pixelSize: Theme.fontCallout
    }

    FocusRing {}

    MouseArea {
        id: mouse

        anchors.fill: parent
        hoverEnabled: true
        cursorShape: Qt.PointingHandCursor
        onClicked: root.clicked()
    }
}
