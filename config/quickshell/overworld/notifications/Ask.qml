import QtQuick
import qs.theme

// What a Bell notice asks, answered in place: questions with choices and words of your own, or buttons.
Column {
    id: root

    required property var notice
    readonly property var ask: notice.ask
    readonly property bool questions: ask?.type === "questions"
    readonly property var asked: questions ? ask.asked : []
    property int step: 0
    property var picks: asked.map(() => [])
    property var written: asked.map(() => "")
    readonly property var question: asked[step] ?? null
    readonly property bool last: step >= asked.length - 1
    readonly property bool ready: question !== null && (picks[step].length > 0 || written[step].trim() !== "")

    function pick(label: string): void {
        const now = picks[step];
        const next = question.several
            ? (now.includes(label) ? now.filter(l => l !== label) : [...now, label])
            : [label];
        picks = picks.map((p, i) => i === step ? next : p);
    }

    function proceed(): void {
        if (!ready)
            return;
        if (!last) {
            step++;
            field.text = written[step];
            return;
        }
        Bell.answer(notice, {
            type: "answered",
            given: asked.map((_, i) => ({ labels: picks[i], written: written[i].trim() || null }))
        }, "✓ Answered");
    }

    spacing: 8

    component Action: Rectangle {
        id: action

        required property string text
        property string tone: "plain"
        property bool ghost
        property bool active: true

        signal clicked

        width: ghost ? label.implicitWidth + 16 : 0
        height: 30
        radius: Theme.radiusSmall
        opacity: active ? 1 : 0.5
        activeFocusOnTab: active
        Keys.onReturnPressed: if (active) clicked()
        Keys.onSpacePressed: if (active) clicked()
        color: tone === "primary"
            ? (mouse.containsMouse && active ? Theme.accentLight : Theme.accent)
            : ghost
                ? (mouse.containsMouse ? Theme.hoverTint : "transparent")
                : (mouse.containsMouse ? Theme.activeTint : Theme.hoverTint)

        Text {
            id: label

            anchors.centerIn: parent
            width: parent.width - 16
            horizontalAlignment: Text.AlignHCenter
            text: action.text
            color: action.tone === "primary" ? Theme.onAccent : action.tone === "danger" ? Theme.danger : action.ghost ? Theme.subtle : Theme.text
            font.pixelSize: Theme.fontCallout
            font.weight: action.tone === "primary" ? Font.DemiBold : Font.Normal
            elide: Text.ElideRight
        }

        MouseArea {
            id: mouse

            anchors.fill: parent
            hoverEnabled: true
            cursorShape: action.active ? Qt.PointingHandCursor : Qt.ArrowCursor
            onClicked: if (action.active) action.clicked()
        }
    }

    Item {
        width: parent.width
        height: chip.height
        visible: root.questions

        Rectangle {
            id: chip

            width: header.implicitWidth + 12
            height: header.implicitHeight + 2
            radius: height / 2
            color: Qt.alpha(Theme.accent, 0.18)
            visible: header.text !== ""

            Text {
                id: header

                anchors.centerIn: parent
                text: root.question?.header ?? ""
                color: Theme.accentLight
                font.pixelSize: Theme.fontCallout - 1
            }
        }

        Text {
            anchors.right: parent.right
            anchors.verticalCenter: parent.verticalCenter
            text: `${root.step + 1} of ${root.asked.length}`
            color: Theme.subtle
            font.pixelSize: Theme.fontCallout - 1
            visible: root.asked.length > 1
        }
    }

    Text {
        width: parent.width
        text: root.question?.text ?? ""
        visible: root.questions
        color: Theme.text
        font.pixelSize: Theme.fontCallout
        wrapMode: Text.Wrap
        lineHeight: 1.15
    }

    Column {
        width: parent.width
        spacing: 4
        visible: root.questions

        Repeater {
            model: root.question?.choices ?? []

            Rectangle {
                id: choice

                required property var modelData
                readonly property bool on: root.picks[root.step]?.includes(modelData.label) ?? false

                width: parent.width
                height: words.implicitHeight + 14
                radius: Theme.radiusControl
                color: on ? Qt.alpha(Theme.accent, 0.14) : choiceMouse.containsMouse ? Theme.activeTint : Theme.hoverTint
                border.width: 1
                border.color: on ? Theme.accent : "transparent"
                activeFocusOnTab: true
                Keys.onSpacePressed: root.pick(modelData.label)
                Keys.onReturnPressed: root.pick(modelData.label)

                Rectangle {
                    id: mark

                    x: 8
                    y: 8
                    width: 14
                    height: 14
                    radius: root.question?.several ? 3 : 7
                    color: choice.on ? Theme.accent : "transparent"
                    border.width: choice.on ? 0 : 1.5
                    border.color: Theme.subtle

                    Rectangle {
                        anchors.centerIn: parent
                        width: 5
                        height: 5
                        radius: root.question?.several ? 1 : 2.5
                        color: Theme.onAccent
                        visible: choice.on
                    }
                }

                Column {
                    id: words

                    anchors.left: mark.right
                    anchors.leftMargin: 8
                    anchors.right: parent.right
                    anchors.rightMargin: 8
                    y: 7
                    spacing: 2

                    Text {
                        width: parent.width
                        text: choice.modelData.label
                        color: Theme.text
                        font.pixelSize: Theme.fontCallout
                        wrapMode: Text.Wrap
                    }

                    Text {
                        width: parent.width
                        text: choice.modelData.detail ?? ""
                        visible: text !== ""
                        color: Theme.subtle
                        font.pixelSize: Theme.fontCallout - 1
                        wrapMode: Text.Wrap
                    }
                }

                MouseArea {
                    id: choiceMouse

                    anchors.fill: parent
                    hoverEnabled: true
                    cursorShape: Qt.PointingHandCursor
                    onClicked: root.pick(choice.modelData.label)
                }
            }
        }
    }

    Rectangle {
        width: parent.width
        height: 30
        radius: Theme.radiusControl
        color: Theme.surface
        border.width: 1
        border.color: field.activeFocus ? Theme.accent : Theme.edge
        visible: root.questions

        HoverHandler {
            cursorShape: Qt.IBeamCursor
        }

        TextInput {
            id: field

            anchors.left: parent.left
            anchors.right: parent.right
            anchors.margins: 8
            anchors.verticalCenter: parent.verticalCenter
            clip: true
            color: Theme.text
            selectionColor: Theme.accent
            selectedTextColor: Theme.onAccent
            font.pixelSize: Theme.fontCallout
            onTextEdited: root.written = root.written.map((w, i) => i === root.step ? text : w)
            onAccepted: root.proceed()

            Text {
                anchors.verticalCenter: parent.verticalCenter
                text: "Or answer in your own words…"
                color: Theme.subtle
                font.pixelSize: Theme.fontCallout
                visible: field.text === ""
            }
        }
    }

    Row {
        id: answers

        width: parent.width
        spacing: 8
        visible: root.questions

        Action {
            id: open

            text: "Open"
            ghost: true
            onClicked: Bell.open(root.notice)
        }

        Action {
            width: (answers.width - open.width - 2 * answers.spacing) / 2
            text: "Skip"
            onClicked: Bell.answer(root.notice, { type: "skipped" }, "Skipped")
        }

        Action {
            width: (answers.width - open.width - 2 * answers.spacing) / 2
            text: root.last ? "Answer" : "Next"
            tone: "primary"
            active: root.ready
            onClicked: root.proceed()
        }
    }

    Text {
        width: parent.width
        leftPadding: 10
        topPadding: 7
        bottomPadding: 7
        text: root.ask?.subject ?? ""
        visible: root.ask?.type === "buttons" && text !== ""
        color: Theme.text
        font.family: Theme.fontMono
        font.pixelSize: Theme.fontCallout
        wrapMode: Text.WrapAnywhere

        Rectangle {
            z: -1
            anchors.fill: parent
            radius: Theme.radiusControl
            color: Theme.surface

            Rectangle {
                width: 2
                height: parent.height
                color: Theme.accent
            }
        }
    }

    Row {
        id: buttons

        readonly property var offered: root.ask?.type === "buttons" ? root.ask.offered : []

        width: parent.width
        spacing: 8
        visible: offered.length > 0

        Repeater {
            model: buttons.offered

            Action {
                required property var modelData

                width: (buttons.width - buttons.spacing * (buttons.offered.length - 1)) / buttons.offered.length
                text: modelData.label
                tone: modelData.tone.toLowerCase()
                onClicked: Bell.answer(root.notice, { type: "pressed", key: modelData.key }, `✓ ${modelData.label}`)
            }
        }
    }
}
