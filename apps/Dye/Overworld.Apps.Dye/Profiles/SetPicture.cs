using System.ComponentModel;

namespace Overworld.Apps.Dye.Profiles;

public sealed record SetPicture(
    [property: Description("Path to a PNG, JPEG or WebP picture; it is copied, so the original can be moved or deleted.")]
    string File);
