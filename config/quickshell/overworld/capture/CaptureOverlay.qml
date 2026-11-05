import Quickshell
import Quickshell.Hyprland
import Quickshell.Wayland
import QtQuick
import qs.theme

PanelWindow {
    id: root

    required property ShellScreen modelData
    readonly property bool frozen: view.hasContent
    readonly property bool focusedScreen: Hyprland.focusedMonitor?.name === modelData.name
    readonly property real ratio: frozen ? view.sourceSize.width / width : 1
    readonly property rect whole: Qt.rect(0, 0, width, height)
    property rect hole
    property point origin

    function windowAt(x: real, y: real): rect {
        const workspace = Hyprland.monitorFor(modelData)?.activeWorkspace?.id;
        const candidates = Hyprland.toplevels.values
            .map(t => t.lastIpcObject)
            .filter(w => w?.at && w.workspace?.id === workspace && !w.hidden)
            .sort((a, b) => a.focusHistoryID - b.focusHistoryID);
        for (const w of candidates) {
            const area = Qt.rect(w.at[0] - modelData.x, w.at[1] - modelData.y, w.size[0], w.size[1]);
            if (x >= area.x && y >= area.y && x <= area.x + area.width && y <= area.y + area.height)
                return area;
        }
        return Qt.rect(0, 0, 0, 0);
    }

    function track(x: real, y: real): void {
        if (Capture.mode === "window")
            hole = windowAt(x, y);
        else if (Capture.mode === "screen")
            hole = whole;
    }

    function shoot(area: rect): void {
        const bounded = Qt.rect(Math.max(0, area.x), Math.max(0, area.y), Math.min(area.width, width - Math.max(0, area.x)), Math.min(area.height, height - Math.max(0, area.y)));
        const size = Qt.size(Math.round(bounded.width * ratio), Math.round(bounded.height * ratio));
        crop.sourceRect = bounded;
        crop.width = bounded.width;
        crop.height = bounded.height;
        crop.textureSize = size;
        crop.grabToImage(result => Capture.store(result), size);
    }

    onFrozenChanged: {
        if (frozen && Capture.instant && focusedScreen)
            shoot(whole);
    }

    Component.onCompleted: Hyprland.refreshToplevels()

    screen: modelData
    color: "transparent"
    exclusionMode: ExclusionMode.Ignore
    WlrLayershell.layer: WlrLayer.Overlay
    WlrLayershell.namespace: "overworld:capture"
    WlrLayershell.keyboardFocus: WlrKeyboardFocus.Exclusive

    anchors {
        top: true
        bottom: true
        left: true
        right: true
    }

    ScreencopyView {
        id: view

        anchors.fill: parent
        captureSource: root.modelData
        live: false
        visible: root.frozen && !Capture.instant
    }

    ShaderEffectSource {
        id: crop

        visible: false
        sourceItem: view
    }

    Item {
        anchors.fill: parent
        visible: root.frozen && !Capture.instant
        focus: true
        Keys.onEscapePressed: Capture.cancel()

        readonly property bool empty: root.hole.width <= 0 || root.hole.height <= 0
        readonly property color shade: Qt.alpha(Theme.background, 0.55)

        Rectangle {
            width: parent.width
            height: parent.empty ? parent.height : root.hole.y
            color: parent.shade
        }

        Rectangle {
            y: root.hole.y + root.hole.height
            width: parent.width
            height: parent.height - y
            color: parent.shade
            visible: !parent.empty
        }

        Rectangle {
            y: root.hole.y
            width: root.hole.x
            height: root.hole.height
            color: parent.shade
            visible: !parent.empty
        }

        Rectangle {
            x: root.hole.x + root.hole.width
            y: root.hole.y
            width: parent.width - x
            height: root.hole.height
            color: parent.shade
            visible: !parent.empty
        }

        Rectangle {
            x: root.hole.x - 1
            y: root.hole.y - 1
            width: root.hole.width + 2
            height: root.hole.height + 2
            color: "transparent"
            border.width: 2
            border.color: Theme.accentLight
            visible: !parent.empty

            Repeater {
                model: 4

                Rectangle {
                    required property int index

                    x: (index % 2) * parent.width - 5
                    y: Math.floor(index / 2) * parent.height - 5
                    width: 10
                    height: 10
                    radius: 5
                    color: Theme.accentLight
                    border.width: 2
                    border.color: Theme.background
                }
            }
        }

        Rectangle {
            readonly property bool below: root.hole.y + root.hole.height + 36 < root.height

            x: Math.min(root.hole.x, root.width - width - 8)
            y: below ? root.hole.y + root.hole.height + 10 : root.hole.y - height - 10
            width: dimensions.implicitWidth + 16
            height: 22
            radius: Theme.tileRadius
            color: Qt.alpha(Theme.background, 0.9)
            border.width: 1
            border.color: Theme.edge
            visible: !parent.empty && Capture.mode === "region"

            Text {
                id: dimensions

                anchors.centerIn: parent
                text: `${Math.round(root.hole.width * root.ratio)} × ${Math.round(root.hole.height * root.ratio)}`
                color: Theme.text
                font.pixelSize: Theme.fontSmall
                font.features: { "tnum": 1 }
            }
        }

        MouseArea {
            anchors.fill: parent
            hoverEnabled: true
            acceptedButtons: Qt.LeftButton | Qt.RightButton
            cursorShape: Capture.mode === "region" ? Qt.CrossCursor : Qt.PointingHandCursor
            onPressed: event => {
                if (event.button === Qt.RightButton) {
                    Capture.cancel();
                } else if (Capture.mode === "region") {
                    root.origin = Qt.point(event.x, event.y);
                    root.hole = Qt.rect(event.x, event.y, 0, 0);
                }
            }
            onPositionChanged: event => {
                if (Capture.mode !== "region")
                    root.track(event.x, event.y);
                else if (pressed)
                    root.hole = Qt.rect(Math.min(root.origin.x, event.x), Math.min(root.origin.y, event.y), Math.abs(event.x - root.origin.x), Math.abs(event.y - root.origin.y));
            }
            onReleased: event => {
                if (event.button !== Qt.LeftButton)
                    return;
                if (root.hole.width >= 4 && root.hole.height >= 4)
                    root.shoot(root.hole);
                else
                    root.hole = Qt.rect(0, 0, 0, 0);
            }
            onExited: {
                if (Capture.mode !== "region")
                    root.hole = Qt.rect(0, 0, 0, 0);
            }
        }

        CaptureBar {
            anchors.horizontalCenter: parent.horizontalCenter
            anchors.top: parent.top
            anchors.topMargin: 16
            onPicked: mode => {
                Capture.mode = mode;
                root.hole = Qt.rect(0, 0, 0, 0);
            }
            onClosed: Capture.cancel()
        }
    }
}
