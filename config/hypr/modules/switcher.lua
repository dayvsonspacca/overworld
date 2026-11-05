local held = { "Alt_L", "Alt_R" }
local order = {}
local index = 1
local poll

local function holding()
    for _, key in ipairs(held) do
        if hl.is_key_down(key) then
            return true
        end
    end
    return false
end

local function candidates()
    local windows = {}
    for _, window in ipairs(hl.get_windows()) do
        if window.mapped and not window.hidden and window.workspace and not window.workspace.special then
            table.insert(windows, window)
        end
    end
    table.sort(windows, function(a, b) return a.focus_history_id < b.focus_history_id end)
    return windows
end

local function show(position)
    index = (position - 1) % #order + 1
    pcall(hl.dispatch, hl.dsp.focus({ window = order[index] }))
end

local function finish()
    poll:set_enabled(false)
    order = {}
end

local function step(delta)
    if #order == 0 then
        order = candidates()
        if #order < 2 then
            order = {}
            return
        end
        index = 1
        poll:set_enabled(true)
    end
    show(index + delta)
end

poll = hl.timer(function()
    if not holding() then
        finish()
    end
end, { timeout = 30, type = "repeat" })
poll:set_enabled(false)

hl.bind("ALT + Tab",         function() step(1) end)
hl.bind("ALT + SHIFT + Tab", function() step(-1) end)
hl.bind("ALT + Escape",      function()
    if #order > 0 then
        show(1)
        finish()
    end
end)
