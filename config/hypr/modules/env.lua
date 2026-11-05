hl.env("XCURSOR_SIZE", "24")
hl.env("HYPRCURSOR_SIZE", "24")
hl.env("GTK_IM_MODULE", "ibus")

local bin = os.getenv("HOME") .. "/.local/bin"
local path = os.getenv("PATH") or "/usr/local/bin:/usr/bin"
if not (":" .. path .. ":"):find(":" .. bin .. ":", 1, true) then
    hl.env("PATH", bin .. ":" .. path)
end
