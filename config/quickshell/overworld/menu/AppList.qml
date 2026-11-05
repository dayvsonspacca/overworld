import Quickshell
import QtQuick
import qs.apps
import qs.theme

ListView {
    id: root

    required property string query
    readonly property DesktopEntry current: currentItem?.entry ?? null

    signal launched(DesktopEntry entry)

    function alphabetical(entries: list<DesktopEntry>): var {
        let last = "";
        return entries.map(e => {
            const letter = initial(e.name);
            const header = letter !== last ? letter : "";
            last = letter;
            return { entry: e, header };
        });
    }

    function ranked(entries: list<DesktopEntry>): var {
        return entries.map((e, i) => ({ entry: e, header: i === 0 ? "Best match" : i === 1 ? "Apps" : "" }));
    }

    function initial(name: string): string {
        const c = name.normalize("NFD").replace(/[̀-ͯ]/g, "").charAt(0).toUpperCase();
        return /[A-Z]/.test(c) ? c : "#";
    }

    function reset(): void {
        currentIndex = query ? 0 : -1;
    }

    clip: true
    boundsBehavior: Flickable.StopAtBounds
    model: query ? ranked(Apps.search(query)) : alphabetical(Apps.all)
    onQueryChanged: reset()
    onCountChanged: reset()
    Component.onCompleted: reset()
    onCurrentIndexChanged: {
        if (currentIndex >= 0)
            positionViewAtIndex(currentIndex, ListView.Contain);
    }

    delegate: Column {
        id: item

        required property var modelData
        required property int index
        readonly property DesktopEntry entry: modelData.entry

        width: ListView.view.width

        Text {
            text: item.modelData.header
            visible: text !== ""
            topPadding: item.index === 0 ? 0 : 10
            bottomPadding: 4
            leftPadding: 10
            color: Theme.accentLight
            font.pixelSize: Theme.fontSmall
            font.weight: Font.DemiBold
        }

        AppRow {
            width: parent.width
            entry: item.entry
            detail: root.query && item.index === 0 ? "App" : ""
            selected: item.ListView.isCurrentItem
            onClicked: root.launched(item.entry)
        }
    }
}
