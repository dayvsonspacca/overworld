hl.window_rule({
    name  = "suppress-maximize-events",
    match = { class = ".*" },
    suppress_event = "maximize",
})

hl.window_rule({
    name  = "fix-xwayland-drags",
    match = {
        class      = "^$",
        title      = "^$",
        xwayland   = true,
        float      = true,
        fullscreen = false,
        pin        = false,
    },
    no_focus = true,
})

hl.window_rule({
    name  = "picture-in-picture",
    match = { title = "^(Picture-in-Picture|Picture in picture)$" },
    float             = true,
    pin               = true,
    keep_aspect_ratio = true,
    no_initial_focus  = true,
    size              = "480 270",
    move              = "(monitor_w-window_w-20) (monitor_h-window_h-20)",
})

hl.layer_rule({
    match = { namespace = "^overworld:(taskbar|menu|control)$" },
    blur  = true,
})
