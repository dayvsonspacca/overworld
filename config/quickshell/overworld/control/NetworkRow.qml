import Quickshell.Networking as Networking
import QtQuick
import qs.icons
import qs.system
import qs.theme

Rectangle {
    id: root

    required property var network
    property bool open
    property string error
    readonly property bool connected: network.connected
    readonly property bool busy: network.stateChanging
    readonly property bool asking: Network.secured(network) && (!network.known || error !== "")

    signal toggled
    signal closed

    function activate(): void {
        if (connected || asking) {
            toggled();
        } else {
            error = "";
            network.connect();
        }
    }

    function close(): void {
        closed();
        header.forceActiveFocus();
    }

    function submit(password: string): void {
        error = "";
        network.connectWithPsk(password);
    }

    implicitHeight: column.implicitHeight
    radius: Theme.radiusPanel
    color: open ? Theme.raised : "transparent"
    onOpenChanged: {
        if (open && asking && !connected)
            Qt.callLater(() => password.forceActiveFocus());
    }

    Connections {
        target: root.network

        function onConnectionFailed(reason: int): void {
            root.error = reason === Networking.ConnectionFailReason.NoSecrets || reason === Networking.ConnectionFailReason.WifiAuthTimeout
                ? "Wrong password"
                : reason === Networking.ConnectionFailReason.WifiNetworkLost ? "The network is out of reach" : "Couldn't connect";
            if (!root.open)
                root.toggled();
        }

        function onConnectedChanged(): void {
            if (root.network.connected)
                root.error = "";
            if (root.open)
                root.close();
        }
    }

    Column {
        id: column

        width: parent.width

        Rectangle {
            id: header

            width: parent.width
            height: 38
            activeFocusOnTab: true
            Keys.onReturnPressed: root.activate()
            Keys.onSpacePressed: root.activate()
            radius: Theme.radiusControl
            color: root.open ? "transparent" : root.connected ? Theme.activeTint : mouse.containsMouse ? Theme.hoverTint : "transparent"

            Glyph {
                id: icon

                anchors.left: parent.left
                anchors.leftMargin: 10
                anchors.verticalCenter: parent.verticalCenter
                path: Network.strength(root.network)
                color: Theme.text
            }

            Text {
                anchors.left: icon.right
                anchors.leftMargin: 10
                anchors.right: detail.left
                anchors.rightMargin: 8
                anchors.verticalCenter: parent.verticalCenter
                text: root.network.name
                color: Theme.text
                font.pixelSize: Theme.fontCallout + 1
                elide: Text.ElideRight
            }

            Row {
                id: detail

                anchors.right: parent.right
                anchors.rightMargin: 10
                anchors.verticalCenter: parent.verticalCenter
                spacing: 8

                Text {
                    anchors.verticalCenter: parent.verticalCenter
                    text: root.busy ? "Connecting…" : root.network.known && !root.connected ? "Saved" : ""
                    visible: text !== ""
                    color: Theme.subtle
                    font.pixelSize: Theme.fontCallout - 1
                }

                Glyph {
                    anchors.verticalCenter: parent.verticalCenter
                    path: Glyphs.lock
                    size: 14
                    color: Theme.subtle
                    visible: Network.secured(root.network)
                }

                Glyph {
                    anchors.verticalCenter: parent.verticalCenter
                    path: Glyphs.check
                    color: Theme.accentLight
                    visible: root.connected
                }
            }

            FocusRing {}

            MouseArea {
                id: mouse

                anchors.fill: parent
                hoverEnabled: true
                cursorShape: Qt.PointingHandCursor
                onClicked: root.activate()
            }
        }

        Column {
            width: parent.width
            visible: root.open
            padding: 10
            topPadding: 0
            spacing: 8

            PasswordInput {
                id: password

                width: parent.width - 20
                visible: root.asking && !root.connected
                busy: root.busy
                onSubmitted: text => root.submit(text)
                onCancelled: root.close()
            }

            Text {
                text: root.error
                visible: root.error !== ""
                color: Theme.danger
                font.pixelSize: Theme.fontCallout - 1
            }

            Item {
                width: parent.width - 20
                height: actions.height

                Row {
                    id: actions

                    anchors.right: parent.right
                    spacing: 8

                    TextButton {
                        text: root.connected ? "Forget" : "Cancel"
                        onClicked: {
                            if (root.connected)
                                root.network.forget();
                            root.close();
                        }
                    }

                    TextButton {
                        text: root.connected ? "Disconnect" : "Connect"
                        primary: true
                        onClicked: root.connected ? root.network.disconnect() : password.submit()
                    }
                }
            }
        }
    }
}
