import QtQuick
import QtQuick.Shapes
import qs.theme

Shape {
    id: root

    property color color: Theme.text

    width: 16
    height: 16
    preferredRendererType: Shape.CurveRenderer

    ShapePath {
        strokeColor: root.color
        strokeWidth: 1.5
        fillColor: "transparent"

        PathAngleArc {
            centerX: 7
            centerY: 7
            radiusX: 5
            radiusY: 5
            startAngle: 0
            sweepAngle: 360
        }
    }

    ShapePath {
        strokeColor: root.color
        strokeWidth: 1.5
        fillColor: "transparent"
        capStyle: ShapePath.RoundCap
        startX: 11
        startY: 11

        PathLine { x: 14.5; y: 14.5 }
    }
}
