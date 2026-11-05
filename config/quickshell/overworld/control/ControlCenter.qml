import Quickshell
import Quickshell.Hyprland
import Quickshell.Wayland
import QtQuick
import qs.session
import qs.theme

PanelWindow {
    id: root

    required property PanelWindow bar
    property string page: "general"
    readonly property var pages: ({
        general: general,
        sound: sound,
        notices: notices,
        agenda: agenda,
        tray: tray
    })
    readonly property real ringRoom: 4

    signal dismissed

    function inside(item: Item, ancestor: Item): bool {
        for (let i = item; i; i = i.parent) {
            if (i === ancestor)
                return true;
        }
        return false;
    }

    function enter(): void {
        const first = body.nextItemInFocusChain(true);
        if (first && inside(first, body))
            first.forceActiveFocus();
    }

    function focusables(): var {
        const items = [];
        let item = body.nextItemInFocusChain(true);
        while (item && inside(item, body) && !items.includes(item)) {
            if (item.visible)
                items.push(item);
            item = item.nextItemInFocusChain(true);
        }
        return items;
    }

    function bounds(item: Item): var {
        const point = item.mapToItem(panel, 0, 0);
        return { x: point.x, y: point.y, right: point.x + item.width, bottom: point.y + item.height };
    }

    function gap(start: real, end: real, otherStart: real, otherEnd: real): real {
        return Math.max(0, Math.max(start, otherStart) - Math.min(end, otherEnd));
    }

    function distance(from: var, to: var, direction: string): real {
        const horizontal = direction === "left" || direction === "right";
        const cross = horizontal ? gap(from.y, from.bottom, to.y, to.bottom) : gap(from.x, from.right, to.x, to.right);
        if (horizontal && cross > 0)
            return Infinity;

        const along = {
            left: from.x - to.right,
            right: to.x - from.right,
            up: from.y - to.bottom,
            down: to.y - from.bottom
        }[direction];
        if (along < -1)
            return Infinity;

        const offset = horizontal
            ? Math.abs((from.y + from.bottom) - (to.y + to.bottom)) / 2
            : Math.abs((from.x + from.right) - (to.x + to.right)) / 2;
        return Math.max(0, along) + 2 * cross + 0.01 * offset;
    }

    function move(direction: string): bool {
        const current = panel.focused;
        if (!current || !inside(current, body))
            return false;

        const from = bounds(current);
        let best = null;
        let score = Infinity;
        for (const item of focusables()) {
            if (item === current)
                continue;
            const candidate = distance(from, bounds(item), direction);
            if (candidate < score) {
                score = candidate;
                best = item;
            }
        }

        best?.forceActiveFocus();
        return best !== null;
    }

    function reveal(item: Item): void {
        if (!item || !inside(item, flick.contentItem))
            return;

        const y = item.mapToItem(flick.contentItem, 0, 0).y;
        const max = Math.max(0, flick.contentHeight + flick.topMargin + flick.bottomMargin - flick.height);
        if (y - ringRoom < flick.contentY)
            flick.contentY = Math.max(-flick.topMargin, y - ringRoom * 2);
        else if (y + item.height + ringRoom > flick.contentY + flick.height)
            flick.contentY = Math.min(max, y + item.height + ringRoom * 2 - flick.height);
    }

    onPageChanged: rail.forceActiveFocus()

    screen: bar.screen
    implicitWidth: Theme.controlWidth
    implicitHeight: Theme.controlHeight
    color: "transparent"
    exclusiveZone: 0
    WlrLayershell.namespace: "overworld:control"
    WlrLayershell.keyboardFocus: WlrKeyboardFocus.Exclusive

    anchors {
        right: true
        bottom: true
    }

    margins {
        right: Theme.gap
        bottom: Theme.gap
    }

    HyprlandFocusGrab {
        active: true
        windows: [root, root.bar]
        onCleared: root.dismissed()
    }

    Component {
        id: general

        GeneralPage {}
    }

    Component {
        id: sound

        SoundPage {}
    }

    Component {
        id: notices

        NoticesPage {}
    }

    Component {
        id: agenda

        AgendaPage {}
    }

    Component {
        id: tray

        TrayPage {
            onDone: root.dismissed()
        }
    }

    Rectangle {
        id: panel

        readonly property Item focused: Window.activeFocusItem

        anchors.fill: parent
        radius: Theme.panelRadius
        color: Theme.background
        border.width: 1
        border.color: Theme.border
        onFocusedChanged: root.reveal(focused)

        Rail {
            id: rail

            anchors.top: parent.top
            anchors.bottom: footer.top
            anchors.left: parent.left
            anchors.margins: 1
            anchors.bottomMargin: 0
            topLeftRadius: Theme.panelRadius - 1
            current: root.page
            focus: true
            onSelected: page => root.page = page
            Keys.onUpPressed: shift(-1)
            Keys.onDownPressed: shift(1)
            Keys.onRightPressed: root.enter()
            Keys.onEscapePressed: root.dismissed()
        }

        Rectangle {
            id: split

            anchors.top: parent.top
            anchors.bottom: footer.top
            anchors.left: rail.right
            anchors.topMargin: 1
            width: 1
            color: Theme.divider
        }

        FocusScope {
            id: body

            anchors.top: parent.top
            anchors.bottom: footer.top
            anchors.left: split.right
            anchors.right: parent.right
            anchors.margins: Theme.controlPadding - root.ringRoom
            Keys.onUpPressed: root.move("up")
            Keys.onDownPressed: root.move("down")
            Keys.onRightPressed: root.move("right")
            Keys.onLeftPressed: {
                if (!root.move("left"))
                    rail.forceActiveFocus();
            }
            Keys.onEscapePressed: rail.forceActiveFocus()

            Flickable {
                id: flick

                anchors.fill: parent
                clip: true
                boundsBehavior: Flickable.StopAtBounds
                topMargin: root.ringRoom
                bottomMargin: root.ringRoom
                leftMargin: root.ringRoom
                rightMargin: root.ringRoom
                contentWidth: width - leftMargin - rightMargin
                contentHeight: content.implicitHeight

                Loader {
                    id: content

                    width: flick.contentWidth
                    sourceComponent: root.pages[root.page]
                }
            }
        }

        UserFooter {
            id: footer

            anchors.bottom: parent.bottom
            anchors.left: parent.left
            anchors.right: parent.right
            anchors.margins: 1
            bottomLeftRadius: Theme.panelRadius - 1
            bottomRightRadius: Theme.panelRadius - 1
            onDone: root.dismissed()
        }
    }
}
