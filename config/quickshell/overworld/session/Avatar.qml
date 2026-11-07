import Quickshell
import QtQuick
import QtQuick.Effects
import qs.daemon
import qs.theme

Rectangle {
    id: root

    required property string user
    property int size: 32
    readonly property string face: `file://${Quickshell.env("HOME")}/.face`

    width: size
    height: size
    radius: size / 2
    gradient: Gradient {
        GradientStop { position: 0; color: Theme.accentLight }
        GradientStop { position: 1; color: Theme.accentDeep }
    }

    Text {
        anchors.centerIn: parent
        visible: picture.status !== Image.Ready
        text: root.user.charAt(0).toUpperCase()
        color: Theme.background
        font.pixelSize: Math.round(root.size * 0.44)
        font.weight: Font.DemiBold
    }

    Image {
        id: picture

        anchors.fill: parent
        source: root.face
        sourceSize: Qt.size(root.size * 2, root.size * 2)
        fillMode: Image.PreserveAspectCrop
        cache: false
        asynchronous: true
        visible: false
    }

    Rectangle {
        id: mask

        anchors.fill: parent
        radius: width / 2
        visible: false
        layer.enabled: true
    }

    MultiEffect {
        anchors.fill: parent
        visible: picture.status === Image.Ready
        source: picture
        maskEnabled: true
        maskSource: mask
    }

    // Dye keeps the picture at the same path when it changes, so it is read again whenever the beacon says the profile changed.
    Connections {
        target: Beacon

        function onLit(name: string): void {
            if (name !== "profile")
                return;

            picture.source = "";
            picture.source = root.face;
        }
    }
}
