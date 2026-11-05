local programs = require("modules.programs")

local mod = "SUPER"
local config = os.getenv("XDG_CONFIG_HOME") or (os.getenv("HOME") .. "/.config")
local shell = os.getenv("OVERWORLD_SHELL") or (config .. "/quickshell/overworld")
local ipc = "qs -p " .. shell .. " ipc call "

hl.bind(mod .. " + L",       hl.dsp.exec_cmd(ipc .. "lock lock"))

hl.bind(mod .. " + T", hl.dsp.exec_cmd(programs.terminal))
hl.bind(mod .. " + E", hl.dsp.exec_cmd(programs.fileManager))

hl.bind(mod .. " + W", hl.dsp.window.close())
hl.bind(mod .. " + F", hl.dsp.window.float({ action = "toggle" }))

for key, dir in pairs({ Left = "l", Right = "r", Up = "u", Down = "d" }) do
    hl.bind(mod .. " + " .. key,         hl.dsp.focus({ direction = dir }))
    hl.bind(mod .. " + SHIFT + " .. key, hl.dsp.window.move({ direction = dir }))
end

for i = 1, 10 do
    local key = tostring(i % 10)
    hl.bind(mod .. " + " .. key,         hl.dsp.focus({ workspace = tostring(i) }))
    hl.bind(mod .. " + SHIFT + " .. key, hl.dsp.window.move({ workspace = tostring(i) }))
end

hl.bind(mod .. " + mouse_down", hl.dsp.focus({ workspace = "e-1" }))
hl.bind(mod .. " + mouse_up",   hl.dsp.focus({ workspace = "e+1" }))
hl.bind(mod .. " + mouse:272",  hl.dsp.window.drag(),   { mouse = true })
hl.bind(mod .. " + mouse:273",  hl.dsp.window.resize(), { mouse = true })

hl.bind("XF86AudioPlay",        hl.dsp.exec_cmd("playerctl play-pause"), { locked = true })
hl.bind("XF86AudioPrev",        hl.dsp.exec_cmd("playerctl previous"),   { locked = true })
hl.bind("XF86AudioNext",        hl.dsp.exec_cmd("playerctl next"),       { locked = true })
hl.bind("XF86AudioRaiseVolume", hl.dsp.exec_cmd("wpctl set-volume -l 1 @DEFAULT_AUDIO_SINK@ 5%+"), { locked = true, repeating = true })
hl.bind("XF86AudioLowerVolume", hl.dsp.exec_cmd("wpctl set-volume @DEFAULT_AUDIO_SINK@ 5%-"),      { locked = true, repeating = true })
hl.bind("XF86AudioMute",        hl.dsp.exec_cmd("wpctl set-mute @DEFAULT_AUDIO_SINK@ toggle"),     { locked = true })
hl.bind("XF86AudioMicMute",     hl.dsp.exec_cmd("wpctl set-mute @DEFAULT_AUDIO_SOURCE@ toggle"),   { locked = true })
