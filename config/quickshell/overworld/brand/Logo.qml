import QtQuick

Image {
    property int size: 32

    width: size
    height: size
    source: Qt.resolvedUrl("amethyst.png")
    smooth: false
    mipmap: false
}
