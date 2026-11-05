pragma Singleton

import Quickshell
import Quickshell.Networking
import QtQuick
import qs.icons

Singleton {
    readonly property var devices: Networking.devices.values
    readonly property var wired: devices.find(d => d.type === DeviceType.Wired) ?? null
    readonly property var wifi: devices.find(d => d.type === DeviceType.Wifi) ?? null
    readonly property var wifiNetwork: wifi?.networks.values.find(n => n.connected) ?? null
    readonly property bool wiredConnected: wired?.connected ?? false
    readonly property bool wifiConnected: wifi?.connected ?? false
    readonly property bool wifiEnabled: Networking.wifiEnabled
    readonly property bool wifiAvailable: wifi !== null && Networking.wifiHardwareEnabled
    readonly property string glyph: wiredConnected ? Glyphs.wired : wifiConnected ? Glyphs.wifi : Glyphs.offline

    function setWifi(enabled: bool): void {
        Networking.wifiEnabled = enabled;
    }
}
