import Quickshell
import QtQuick
import qs.icons
import qs.theme

Column {
    id: root

    property date month: new Date(clock.date.getFullYear(), clock.date.getMonth(), 1)

    function shift(delta: int): void {
        month = new Date(month.getFullYear(), month.getMonth() + delta, 1);
    }

    component Arrow: Rectangle {
        id: arrow

        required property string glyph

        signal clicked

        width: 28
        height: 28
        radius: Theme.tileRadius
        activeFocusOnTab: true
        Keys.onReturnPressed: clicked()
        Keys.onSpacePressed: clicked()
        color: mouse.containsMouse ? Theme.hoverTint : "transparent"

        Glyph {
            anchors.centerIn: parent
            path: arrow.glyph
            size: 12
            color: Theme.subtle
        }

        FocusRing {
            radius: arrow.radius + gap
        }

        MouseArea {
            id: mouse

            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: arrow.clicked()
        }
    }

    spacing: 14

    SystemClock {
        id: clock

        precision: SystemClock.Minutes
    }

    Column {
        Text {
            text: clock.date.toLocaleTimeString(Qt.locale(), Locale.ShortFormat)
            color: Theme.text
            font.pixelSize: 40
            font.weight: Font.Light
        }

        Text {
            topPadding: 6
            text: clock.date.toLocaleDateString(Qt.locale(), Locale.LongFormat)
            color: Theme.subtle
            font.pixelSize: Theme.fontSmall + 1
        }
    }

    Item {
        width: parent.width
        height: 28

        PageTitle {
            anchors.verticalCenter: parent.verticalCenter
            text: root.month.toLocaleDateString(Qt.locale(), "MMMM yyyy")
        }

        Row {
            anchors.right: parent.right
            spacing: 2

            Arrow {
                glyph: Glyphs.left
                onClicked: root.shift(-1)
            }

            Arrow {
                glyph: Glyphs.right
                onClicked: root.shift(1)
            }
        }
    }

    Rectangle {
        width: parent.width
        height: calendar.implicitHeight + 24
        radius: Theme.itemRadius
        color: Qt.alpha(Theme.surface, 0.75)
        border.width: 1
        border.color: Theme.edge

        CalendarMonth {
            id: calendar

            anchors.top: parent.top
            anchors.left: parent.left
            anchors.right: parent.right
            anchors.margins: 12
            month: root.month
            today: clock.date
        }
    }
}
