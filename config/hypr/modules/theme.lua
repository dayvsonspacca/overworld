-- The colors of the theme Overworld wears, read from theme.json; the shell reloads Hyprland when the beacon signals a new one.
local config = os.getenv("XDG_CONFIG_HOME") or (os.getenv("HOME") .. "/.config")
local path = config .. "/overworld/theme.json"

local file = io.open(path, "r")
local json = file and file:read("a") or ""
if file then
    file:close()
end

local theme = {}

function theme.color(role)
    local hex = json:match('"' .. role .. '"%s*:%s*"#(%x+)"')
    if not hex then
        return nil
    end

    if #hex == 8 then
        return "rgba(" .. hex:sub(3) .. hex:sub(1, 2) .. ")"
    end

    return "rgba(" .. hex .. "ff)"
end

return theme
