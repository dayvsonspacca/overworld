import QtQuick
import QtQuick.Shapes

Item {
    id: root

    required property string path
    property color color: "white"
    property real size: 16
    property real weight: 1.4
    property bool filled

    width: size
    height: size

    Shape {
        width: 16
        height: 16
        scale: root.size / 16
        transformOrigin: Item.TopLeft
        preferredRendererType: Shape.CurveRenderer

        ShapePath {
            strokeColor: root.color
            strokeWidth: root.weight
            fillColor: root.filled ? root.color : "transparent"
            capStyle: ShapePath.RoundCap
            joinStyle: ShapePath.RoundJoin

            PathSvg {
                path: root.path
            }
        }
    }
}
