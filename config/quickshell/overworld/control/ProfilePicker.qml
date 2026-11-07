import QtQuick
import qs.system
import qs.theme

Rectangle {
    id: root

    implicitHeight: 30
    radius: Theme.radiusControl
    color: Theme.surface

    Row {
        anchors.fill: parent
        anchors.margins: 2

        Repeater {
            model: Battery.profiles

            Rectangle {
                id: option

                required property var modelData
                readonly property bool selected: modelData.profile === Battery.profile

                width: parent.width / Battery.profiles.length
                height: parent.height
                activeFocusOnTab: true
                Keys.onReturnPressed: Battery.use(modelData.profile)
                Keys.onSpacePressed: Battery.use(modelData.profile)
                radius: Theme.radiusSmall
                color: selected ? Theme.accent : mouse.containsMouse ? Theme.hoverTint : "transparent"

                Text {
                    anchors.centerIn: parent
                    text: option.modelData.label
                    color: option.selected ? Theme.onAccent : Theme.subtle
                    font.pixelSize: Theme.fontCallout
                }

                FocusRing {
                    radius: Theme.radiusSmall + gap
                }

                MouseArea {
                    id: mouse

                    anchors.fill: parent
                    hoverEnabled: true
                    cursorShape: Qt.PointingHandCursor
                    onClicked: Battery.use(option.modelData.profile)
                }
            }
        }
    }
}
