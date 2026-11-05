import QtQuick
import QtQuick.Shapes

Shape {
    id: root

    property color color: "white"

    width: 10
    height: 10
    preferredRendererType: Shape.CurveRenderer

    ShapePath {
        strokeColor: root.color
        strokeWidth: 1.2
        fillColor: "transparent"
        capStyle: ShapePath.RoundCap
        startX: 1
        startY: 1

        PathLine { x: 9; y: 9 }
        PathMove { x: 9; y: 1 }
        PathLine { x: 1; y: 9 }
    }
}
