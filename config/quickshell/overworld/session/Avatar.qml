import QtQuick
import qs.theme

Rectangle {
    id: root

    required property string user
    property int size: 32

    width: size
    height: size
    radius: size / 2
    gradient: Gradient {
        GradientStop { position: 0; color: Theme.accentLight }
        GradientStop { position: 1; color: Theme.accentDeep }
    }

    Text {
        anchors.centerIn: parent
        text: root.user.charAt(0).toUpperCase()
        color: Theme.background
        font.pixelSize: Math.round(root.size * 0.44)
        font.weight: Font.DemiBold
    }
}
