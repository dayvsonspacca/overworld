import Quickshell
import Quickshell.Hyprland
import Quickshell.Wayland
import QtQuick
import qs.session
import qs.theme

PanelWindow {
    id: root

    required property PanelWindow bar
    property bool inspecting
    readonly property bool showsDetails: list.current !== null && (search.text !== "" || inspecting)

    signal dismissed

    function launch(entry: DesktopEntry): void {
        entry.execute();
        dismissed();
    }

    screen: bar.screen
    implicitWidth: Theme.menuWidth
    implicitHeight: Theme.menuHeight
    color: "transparent"
    exclusiveZone: 0
    WlrLayershell.namespace: "overworld:menu"
    WlrLayershell.keyboardFocus: WlrKeyboardFocus.Exclusive

    anchors {
        left: true
        bottom: true
    }

    margins {
        left: Theme.gap
        bottom: Theme.gap
    }

    HyprlandFocusGrab {
        active: true
        windows: [root, root.bar]
        onCleared: root.dismissed()
    }

    Rectangle {
        anchors.fill: parent
        radius: Theme.panelRadius
        color: Theme.background
        border.width: 1
        border.color: Theme.border

        Item {
            id: side

            anchors.top: parent.top
            anchors.bottom: parent.bottom
            anchors.left: parent.left
            width: Theme.menuListWidth

            AppList {
                id: list

                anchors.fill: parent
                anchors.margins: 8
                anchors.topMargin: 16
                query: search.text
                onLaunched: entry => root.launch(entry)
            }
        }

        Rectangle {
            id: split

            anchors.top: parent.top
            anchors.bottom: parent.bottom
            anchors.left: side.right
            anchors.topMargin: 1
            anchors.bottomMargin: 1
            width: 1
            color: Theme.divider
        }

        Item {
            anchors.top: parent.top
            anchors.bottom: parent.bottom
            anchors.left: split.right
            anchors.right: parent.right

            SearchBox {
                id: search

                anchors.top: parent.top
                anchors.left: parent.left
                anchors.right: parent.right
                anchors.margins: Theme.menuPadding
                anchors.topMargin: 20
                canGoBack: root.inspecting
                onTextChanged: root.inspecting = false
                onUp: {
                    if (root.inspecting)
                        details.item.move(-1);
                    else
                        list.decrementCurrentIndex();
                }
                onDown: {
                    if (root.inspecting)
                        details.item.move(1);
                    else
                        list.incrementCurrentIndex();
                }
                onInspect: {
                    if (list.current)
                        root.inspecting = true;
                }
                onBack: root.inspecting = false
                onAccepted: {
                    if (root.inspecting)
                        details.item.trigger();
                    else if (list.current)
                        root.launch(list.current);
                }
                onEscaped: {
                    if (root.inspecting)
                        root.inspecting = false;
                    else if (text)
                        clear();
                    else
                        root.dismissed();
                }
            }

            Item {
                id: content

                anchors.top: search.bottom
                anchors.bottom: footer.top
                anchors.left: parent.left
                anchors.right: parent.right
                anchors.margins: Theme.menuPadding
                anchors.topMargin: 20
                anchors.bottomMargin: 16

                PinnedGrid {
                    anchors.fill: parent
                    visible: !root.showsDetails && !search.text
                    onLaunched: entry => root.launch(entry)
                }

                Loader {
                    id: details

                    anchors.fill: parent
                    active: root.showsDetails

                    sourceComponent: AppDetails {
                        entry: list.current
                        focused: root.inspecting
                        onDone: root.dismissed()
                    }
                }

                Text {
                    anchors.centerIn: parent
                    text: "No apps found"
                    color: Theme.subtle
                    font.pixelSize: Theme.fontSize - 1
                    visible: search.text !== "" && list.count === 0
                }
            }

            UserFooter {
                id: footer

                anchors.bottom: parent.bottom
                anchors.left: parent.left
                anchors.right: parent.right
                anchors.margins: 1
                anchors.leftMargin: 0
                bottomRightRadius: Theme.panelRadius - 1
                onDone: root.dismissed()
            }
        }
    }
}
