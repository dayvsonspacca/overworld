import QtQuick

Item {
    Facets {
        anchors.fill: parent
        visible: image.status !== Image.Ready
    }

    Image {
        id: image

        anchors.fill: parent
        source: Picture.source
        asynchronous: true
        cache: false
        fillMode: Image.PreserveAspectCrop
    }
}
