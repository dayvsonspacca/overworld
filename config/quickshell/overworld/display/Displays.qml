pragma Singleton

import Quickshell
import QtQuick

Singleton {
    readonly property ShellScreen primary: Quickshell.screens.reduce((best, s) => !best || area(s) > area(best) ? s : best, null)

    function area(s: ShellScreen): int {
        return s.width * s.height;
    }
}
