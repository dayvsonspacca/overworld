pragma Singleton

import Quickshell
import Quickshell.Services.UPower
import QtQuick

Singleton {
    readonly property var device: UPower.displayDevice
    readonly property bool available: device.ready && device.isLaptopBattery
    readonly property real level: device.percentage
    readonly property bool charging: device.state === UPowerDeviceState.Charging || device.state === UPowerDeviceState.PendingCharge
    readonly property bool full: device.state === UPowerDeviceState.FullyCharged
    readonly property bool low: level < 0.15 && !charging
    readonly property string percent: `${Math.round(level * 100)}%`
    readonly property string remaining: {
        if (full)
            return "Fully charged";
        const seconds = charging ? device.timeToFull : device.timeToEmpty;
        if (seconds <= 0)
            return charging ? "Charging" : "";
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor(seconds % 3600 / 60);
        const time = hours > 0 ? `${hours} h ${minutes} min` : `${minutes} min`;
        return charging ? `${time} to full` : `${time} left`;
    }

    readonly property int profile: PowerProfiles.profile
    readonly property var profiles: [
        { profile: PowerProfile.PowerSaver, label: "Power saver" },
        { profile: PowerProfile.Balanced, label: "Balanced" },
        ...(PowerProfiles.hasPerformanceProfile ? [{ profile: PowerProfile.Performance, label: "Performance" }] : [])
    ]

    function use(profile: int): void {
        PowerProfiles.profile = profile;
    }
}
