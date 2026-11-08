import QtQuick
import qs.icons
import qs.theme

Rectangle {
    id: root

    property alias text: input.text
    property bool canGoBack

    signal up
    signal down
    signal back
    signal inspect
    signal accepted
    signal escaped

    function clear(): void {
        input.clear();
    }

    function takeFocus(): void {
        input.forceActiveFocus();
    }

    implicitHeight: Theme.searchHeight
    radius: height / 2
    color: Theme.accent

    HoverHandler {
        cursorShape: Qt.IBeamCursor
    }

    Rectangle {
        anchors.fill: parent
        anchors.bottomMargin: 2
        radius: root.radius
        color: Theme.raised
        border.width: 1
        border.color: Theme.border
    }

    SearchIcon {
        id: icon

        anchors.left: parent.left
        anchors.leftMargin: 14
        anchors.verticalCenter: parent.verticalCenter
        color: Theme.subtle
    }

    TextInput {
        id: input

        anchors.left: icon.right
        anchors.leftMargin: 10
        anchors.right: parent.right
        anchors.rightMargin: 14
        anchors.verticalCenter: parent.verticalCenter
        color: Theme.text
        selectionColor: Theme.accentDeep
        selectedTextColor: Theme.text
        font.pixelSize: Theme.fontContent - 1
        clip: true
        onAccepted: root.accepted()
        Keys.onUpPressed: root.up()
        Keys.onDownPressed: root.down()
        Keys.onEscapePressed: root.escaped()
        Keys.onRightPressed: event => {
            if (cursorPosition === length)
                root.inspect();
            else
                event.accepted = false;
        }
        Keys.onLeftPressed: event => {
            if (root.canGoBack)
                root.back();
            else
                event.accepted = false;
        }
        Component.onCompleted: forceActiveFocus()
    }

    Text {
        anchors.left: input.left
        anchors.verticalCenter: input.verticalCenter
        text: "Search apps"
        color: Theme.subtle
        font: input.font
        visible: !input.text
    }
}
