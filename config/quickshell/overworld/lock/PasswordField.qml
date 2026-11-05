import QtQuick
import qs.icons
import qs.theme

FocusScope {
    id: root

    property bool busy

    signal submitted(string password)

    function submit(): void {
        if (input.text !== "" && !busy)
            submitted(input.text);
    }

    function reject(): void {
        input.text = "";
        shake.restart();
    }

    implicitWidth: Theme.lockFieldWidth
    implicitHeight: 40

    transform: Translate {
        id: offset
    }

    SequentialAnimation {
        id: shake

        loops: 2

        NumberAnimation { target: offset; property: "x"; to: -8; duration: 40 }
        NumberAnimation { target: offset; property: "x"; to: 8; duration: 80 }
        NumberAnimation { target: offset; property: "x"; to: 0; duration: 40 }
    }

    Rectangle {
        anchors.fill: parent
        radius: Theme.itemRadius
        color: Qt.alpha(Theme.background, 0.75)
        border.width: input.activeFocus ? 2 : 1
        border.color: input.activeFocus ? Theme.accentLight : Theme.edge
    }

    HoverHandler {
        cursorShape: Qt.IBeamCursor
    }

    TextInput {
        id: input

        anchors.left: parent.left
        anchors.leftMargin: 14
        anchors.right: go.left
        anchors.rightMargin: 8
        anchors.verticalCenter: parent.verticalCenter
        focus: true
        clip: true
        readOnly: root.busy
        echoMode: TextInput.Password
        passwordCharacter: "●"
        color: Theme.text
        font.pixelSize: Theme.fontSize + 2
        font.letterSpacing: 2
        onAccepted: root.submit()
        onActiveFocusChanged: {
            if (!activeFocus && root.focus)
                Qt.callLater(() => input.forceActiveFocus());
        }
        Keys.onEscapePressed: text = ""

        Text {
            anchors.verticalCenter: parent.verticalCenter
            text: root.busy ? "Checking…" : "Password"
            color: Theme.subtle
            font.pixelSize: Theme.fontSize - 1
            font.letterSpacing: 0
            visible: input.text === "" || root.busy
        }
    }

    Rectangle {
        id: go

        anchors.right: parent.right
        anchors.rightMargin: 5
        anchors.verticalCenter: parent.verticalCenter
        width: 30
        height: 30
        radius: Theme.tileRadius
        color: goMouse.containsMouse ? Theme.accentLight : Theme.accent
        opacity: root.busy ? 0.5 : 1

        Glyph {
            anchors.centerIn: parent
            path: Glyphs.enter
            size: 14
            weight: 1.6
            color: "white"
        }

        MouseArea {
            id: goMouse

            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: root.submit()
        }
    }
}
