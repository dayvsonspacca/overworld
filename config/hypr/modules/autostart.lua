hl.on("hyprland.start", function()
    hl.exec_cmd("ibus-daemon -drx")
    hl.exec_cmd("overworld")
end)
