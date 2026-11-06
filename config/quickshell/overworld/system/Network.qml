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
    readonly property var wifiNetworks: (wifi?.networks.values ?? [])
        .filter(n => n.name !== "")
        .sort((a, b) => (b.connected - a.connected) || (b.known - a.known) || (b.signalStrength - a.signalStrength))
    readonly property bool wiredConnected: wired?.connected ?? false
    readonly property bool wifiConnected: wifi?.connected ?? false
    readonly property bool wifiEnabled: Networking.wifiEnabled
    readonly property bool wifiAvailable: wifi !== null && Networking.wifiHardwareEnabled
    readonly property string glyph: wiredConnected ? Glyphs.wired : wifiConnected ? Glyphs.wifi : Glyphs.offline

    function setWifi(enabled: bool): void {
        Networking.wifiEnabled = enabled;
    }

    function strength(network: var): string {
        const signal = network?.signalStrength ?? 0;
        return signal > 0.66 ? Glyphs.wifi : signal > 0.33 ? Glyphs.wifiMid : Glyphs.wifiLow;
    }

    function secured(network: var): bool {
        return network.security !== WifiSecurityType.Open && network.security !== WifiSecurityType.Owe;
    }
}
