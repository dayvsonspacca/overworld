import QtQuick
import qs.theme

Column {
    id: root

    required property date month
    required property date today
    readonly property int first: Qt.locale().firstDayOfWeek
    readonly property var cells: {
        const year = month.getFullYear();
        const index = month.getMonth();
        const offset = (new Date(year, index, 1).getDay() - first + 7) % 7;
        const days = new Date(year, index + 1, 0).getDate();
        const count = Math.ceil((offset + days) / 7) * 7;
        return Array.from({ length: count }, (_, i) => new Date(year, index, 1 - offset + i));
    }

    spacing: 2

    Row {
        Repeater {
            model: 7

            Text {
                required property int index

                width: root.width / 7
                height: 24
                horizontalAlignment: Text.AlignHCenter
                verticalAlignment: Text.AlignVCenter
                text: Qt.locale().dayName((root.first + index) % 7, Locale.NarrowFormat)
                color: Theme.subtle
                font.pixelSize: Theme.fontCallout
            }
        }
    }

    Grid {
        columns: 7

        Repeater {
            model: root.cells

            Item {
                id: cell

                required property date modelData
                readonly property bool isToday: modelData.toDateString() === root.today.toDateString()
                readonly property bool inMonth: modelData.getMonth() === root.month.getMonth()

                width: root.width / 7
                height: 32

                Rectangle {
                    anchors.centerIn: parent
                    width: 30
                    height: 30
                    radius: 15
                    color: cell.isToday ? Theme.accent : "transparent"
                }

                Text {
                    anchors.centerIn: parent
                    text: cell.modelData.getDate()
                    color: cell.isToday ? "white" : cell.inMonth ? Theme.text : Qt.alpha(Theme.text, 0.25)
                    font.pixelSize: Theme.fontCallout
                    font.weight: cell.isToday ? Font.DemiBold : Font.Normal
                }
            }
        }
    }
}
