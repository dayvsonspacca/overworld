import QtQuick
import qs.icons
import qs.theme

Rectangle {
    id: root

    readonly property var modes: [
        { id: "region", label: "Region", glyph: Glyphs.region },
        { id: "window", label: "Window", glyph: Glyphs.window },
        { id: "screen", label: "Screen", glyph: Glyphs.screen }
    ]

    signal picked(string mode)
    signal closed

    component Tool: Rectangle {
        id: tool

        property string label
        required property string glyph
        property bool selected

        signal clicked

        width: label === "" ? 34 : content.implicitWidth + 20
        height: 34
        radius: Theme.itemRadius
        color: selected ? Theme.accent : mouse.containsMouse ? Theme.hoverTint : "transparent"

        Row {
            id: content

            anchors.centerIn: parent
            spacing: 8

            Glyph {
                anchors.verticalCenter: parent.verticalCenter
                path: tool.glyph
                color: tool.selected ? "white" : Theme.text
            }

            Text {
                anchors.verticalCenter: parent.verticalCenter
                text: tool.label
                visible: tool.label !== ""
                color: tool.selected ? "white" : Theme.text
                font.pixelSize: Theme.fontSmall + 1
            }
        }

        MouseArea {
            id: mouse

            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: tool.clicked()
        }
    }

    implicitWidth: row.implicitWidth + 10
    implicitHeight: row.implicitHeight + 10
    radius: Theme.panelRadius
    color: Qt.alpha(Theme.background, 0.92)
    border.width: 1
    border.color: Theme.edge

    Row {
        id: row

        anchors.centerIn: parent
        spacing: 4

        Repeater {
            model: root.modes

            Tool {
                required property var modelData

                label: modelData.label
                glyph: modelData.glyph
                selected: Capture.mode === modelData.id
                onClicked: root.picked(modelData.id)
            }
        }

        Rectangle {
            anchors.verticalCenter: parent.verticalCenter
            width: 1
            height: 24
            color: Theme.edge
        }

        Tool {
            glyph: Glyphs.close
            onClicked: root.closed()
        }
    }
}
