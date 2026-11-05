import Quickshell
import QtQuick
import qs.system
import qs.theme

Column {
    SystemClock {
        id: clock

        precision: SystemClock.Seconds
    }

    Text {
        anchors.right: parent.right
        text: clock.date.toLocaleTimeString(Qt.locale(), Dates.timeFormat)
        color: Theme.text
        font.pixelSize: Theme.fontSmall
        font.features: { "tnum": 1 }
    }

    Text {
        anchors.right: parent.right
        text: clock.date.toLocaleDateString(Qt.locale(), Locale.ShortFormat)
        color: Theme.text
        font.pixelSize: Theme.fontSmall
        font.features: { "tnum": 1 }
    }
}
