require("modules.monitors")
require("modules.env")
require("modules.input")
require("modules.general")
require("modules.animations")
require("modules.binds")
require("modules.rules")
require("modules.autostart")

local config = os.getenv("XDG_CONFIG_HOME") or (os.getenv("HOME") .. "/.config")
local overrides = loadfile(config .. "/hypr/hyprland.local.lua")
if overrides then
    overrides()
end
