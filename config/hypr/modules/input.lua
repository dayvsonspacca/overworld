hl.config({
    input = {
        kb_layout  = "br",
        kb_model   = "abnt2",
        kb_variant = "",
        kb_options = "",

        numlock_by_default = true,
        follow_mouse       = 1,
        sensitivity        = 0,

        touchpad = {
            natural_scroll = true,
        },
    },
})

hl.gesture({
    fingers   = 3,
    direction = "horizontal",
    action    = "workspace",
})
