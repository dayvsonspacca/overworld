# Where apps are installed; set OVERWORLD_APPS to keep them somewhere else on your machine.
apps := env("OVERWORLD_APPS", env("HOME") / ".local/share/overworld/apps")

build:
    dotnet build

test:
    dotnet test

# Publishes Dye into the apps folder, adds it to the app launcher and links overworld:dye into ~/.local/bin.
# The launcher shows apps/Dye/icon.png when this machine has one, and the shared dye.svg otherwise.
# The new build replaces the old folder whole, so a Dye that is open keeps running until you reopen it.
install-dye:
    rm -rf dist/dye
    dotnet publish apps/Dye/Overworld.Apps.Dye.Desktop -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true -o dist/dye
    icon=$(ls apps/Dye/icon.png 2>/dev/null || echo apps/Dye/dye.svg); cp "$icon" "dist/dye/icon.${icon##*.}"; sed -e 's#@APPS@#{{apps}}#g' -e "s#@ICON@#icon.${icon##*.}#g" apps/Dye/dye.desktop > dist/dye/dye.desktop
    mkdir -p "{{apps}}"
    rm -rf "{{apps}}/dye.new"
    cp -r dist/dye "{{apps}}/dye.new"
    rm -rf "{{apps}}/dye"
    mv "{{apps}}/dye.new" "{{apps}}/dye"
    mkdir -p ~/.local/share/applications ~/.local/bin
    mv "{{apps}}/dye/dye.desktop" ~/.local/share/applications/dye.desktop
    ln -sf "{{apps}}/dye/dye" ~/.local/bin/overworld:dye
