import Quickshell
import Quickshell.Wayland
import QtQuick
import qs.icons
import qs.theme

PopupWindow {
    id: root

    property Item pending
    property Item target
    readonly property var group: target?.group ?? null
    readonly property var windows: group?.windows ?? []

    function show(button: Item): void {
        hideTimer.stop();
        pending = button;
        if (target)
            target = button;
        else
            showTimer.restart();
    }

    function leave(): void {
        showTimer.stop();
        hideTimer.restart();
    }

    function hide(): void {
        showTimer.stop();
        hideTimer.stop();
        target = null;
    }

    visible: group !== null
    color: "transparent"
    implicitWidth: card.implicitWidth
    implicitHeight: card.implicitHeight
    anchor.item: target
    anchor.rect.x: 0
    anchor.rect.y: -Theme.previewGap
    anchor.rect.w: target?.width ?? 0
    anchor.rect.h: target?.height ?? 0
    anchor.edges: Edges.Top
    anchor.gravity: Edges.Top

    Timer {
        id: showTimer

        interval: 400
        onTriggered: root.target = root.pending
    }

    Timer {
        id: hideTimer

        interval: 200
        onTriggered: root.target = null
    }

    Rectangle {
        id: card

        readonly property Item content: root.windows.length > 0 ? thumbs : name

        anchors.fill: parent
        implicitWidth: content.implicitWidth + 16
        implicitHeight: content.implicitHeight + 16
        radius: Theme.radiusPanel
        color: Theme.background
        border.width: 1
        border.color: Theme.edge

        HoverHandler {
            onHoveredChanged: {
                if (hovered)
                    hideTimer.stop();
                else
                    root.leave();
            }
        }

        Text {
            id: name

            x: 8
            y: 8
            padding: 4
            visible: root.windows.length === 0
            text: root.group ? Tasks.name(root.group) : ""
            color: Theme.text
            font.pixelSize: Theme.fontCallout
        }

        Row {
            id: thumbs

            x: 8
            y: 8
            spacing: 8
            visible: root.windows.length > 0

            Repeater {
                model: root.windows

                Rectangle {
                    id: preview

                    required property Toplevel modelData

                    width: Theme.previewWidth - 16
                    height: tile.implicitHeight + 12
                    radius: Theme.radiusControl
                    color: tileHover.hovered ? Theme.activeTint : "transparent"

                    Behavior on color {
                        ColorAnimation {
                            duration: 100
                        }
                    }

                    HoverHandler {
                        id: tileHover
                    }

                    MouseArea {
                        anchors.fill: parent
                        cursorShape: Qt.PointingHandCursor
                        onClicked: {
                            preview.modelData.activate();
                            root.hide();
                        }
                    }

                    Column {
                        id: tile

                        x: 6
                        y: 6
                        width: parent.width - 12
                        spacing: 8

                        Item {
                            width: parent.width
                            height: 20

                            Image {
                                id: icon

                                anchors.left: parent.left
                                anchors.leftMargin: 4
                                anchors.verticalCenter: parent.verticalCenter
                                width: 16
                                height: 16
                                sourceSize: Qt.size(width, height)
                                source: root.group ? Tasks.icon(root.group) : ""
                            }

                            Text {
                                anchors.left: icon.right
                                anchors.leftMargin: 8
                                anchors.right: close.left
                                anchors.rightMargin: 8
                                anchors.verticalCenter: parent.verticalCenter
                                text: preview.modelData.title
                                color: Theme.text
                                font.pixelSize: Theme.fontCallout
                                elide: Text.ElideRight
                            }

                            Rectangle {
                                id: close

                                anchors.right: parent.right
                                anchors.verticalCenter: parent.verticalCenter
                                width: 20
                                height: 20
                                radius: Theme.radiusSmall
                                color: closeMouse.containsMouse ? Theme.hoverTint : "transparent"

                                CloseIcon {
                                    anchors.centerIn: parent
                                    color: Theme.text
                                }

                                MouseArea {
                                    id: closeMouse

                                    anchors.fill: parent
                                    hoverEnabled: true
                                    cursorShape: Qt.PointingHandCursor
                                    onClicked: {
                                        preview.modelData.close();
                                        if (root.windows.length === 1)
                                            root.hide();
                                    }
                                }
                            }
                        }

                        ScreencopyView {
                            readonly property real aspect: hasContent ? sourceSize.width / sourceSize.height : 16 / 9

                            anchors.horizontalCenter: parent.horizontalCenter
                            width: Math.min(parent.width, Theme.previewThumbHeight * aspect)
                            height: width / aspect
                            captureSource: preview.modelData
                            live: true
                        }
                    }
                }
            }
        }
    }
}
