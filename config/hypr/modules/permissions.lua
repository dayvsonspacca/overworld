hl.config({
    ecosystem = {
        enforce_permissions = true,
    },
})

hl.permission(".*/quickshell", "screencopy", "allow")
hl.permission(".*/grim", "screencopy", "allow")
