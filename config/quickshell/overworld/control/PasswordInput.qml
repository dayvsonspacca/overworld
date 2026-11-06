import QtQuick
import qs.theme

FocusScope {
    id: root

    property bool busy
    property bool shown

    signal submitted(string password)
    signal cancelled

    function submit(): void {
        if (input.text !== "" && !busy)
            submitted(input.text);
    }

    implicitHeight: 32

    Rectangle {
        anchors.fill: parent
        radius: Theme.radiusControl
        color: Theme.surface
        border.width: input.activeFocus ? 2 : 1
        border.color: input.activeFocus ? Theme.accentLight : Theme.edge
    }

    HoverHandler {
        cursorShape: Qt.IBeamCursor
    }

    TextInput {
        id: input

        anchors.left: parent.left
        anchors.leftMargin: 10
        anchors.right: toggle.left
        anchors.rightMargin: 8
        anchors.verticalCenter: parent.verticalCenter
        focus: true
        clip: true
        readOnly: root.busy
        echoMode: root.shown ? TextInput.Normal : TextInput.Password
        passwordCharacter: "●"
        color: Theme.text
        font.pixelSize: Theme.fontContent
        onAccepted: root.submit()
        Keys.onEscapePressed: root.cancelled()

        Text {
            anchors.verticalCenter: parent.verticalCenter
            text: root.busy ? "Connecting…" : "Password"
            color: Theme.subtle
            font.pixelSize: Theme.fontCallout + 1
            visible: input.text === "" || root.busy
        }
    }

    Text {
        id: toggle

        anchors.right: parent.right
        anchors.rightMargin: 10
        anchors.verticalCenter: parent.verticalCenter
        text: root.shown ? "Hide" : "Show"
        color: Theme.subtle
        font.pixelSize: Theme.fontCallout - 1

        MouseArea {
            anchors.fill: parent
            anchors.margins: -6
            cursorShape: Qt.PointingHandCursor
            onClicked: root.shown = !root.shown
        }
    }
}
