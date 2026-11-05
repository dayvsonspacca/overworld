hl.config({
    general = {
        gaps_in          = 5,
        gaps_out         = 10,
        border_size      = 2,
        resize_on_border = true,
        allow_tearing    = false,
        layout           = "dwindle",

        col = {
            active_border   = { colors = { "rgba(c9a5f2ff)", "rgba(8e62d1ff)" }, angle = 45 },
            inactive_border = "rgba(4a4952ff)",
        },
    },

    decoration = {
        rounding       = 8,
        rounding_power = 2,

        shadow = {
            enabled      = true,
            range        = 12,
            render_power = 3,
            color        = "rgba(000000cc)",
        },

        blur = {
            enabled = true,
            size    = 3,
            passes  = 1,
        },
    },

    dwindle = {
        preserve_split = true,
    },

    misc = {
        force_default_wallpaper = 0,
        disable_hyprland_logo   = true,
        allow_session_lock_restore = true,
    },
})
