local theme = require("modules.theme")

local light, accent, border = theme.color("accentLight"), theme.color("accent"), theme.color("border")

hl.config({
    general = {
        gaps_in          = 5,
        gaps_out         = 10,
        border_size      = 2,
        resize_on_border = true,
        allow_tearing    = false,
        layout           = "dwindle",

        col = light and accent and border and {
            active_border   = { colors = { light, accent }, angle = 45 },
            inactive_border = border,
        } or nil,
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
