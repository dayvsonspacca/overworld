hl.config({
    ecosystem = {
        enforce_permissions = true,
    },
})

hl.permission(".*/quickshell", "screencopy", "allow")
