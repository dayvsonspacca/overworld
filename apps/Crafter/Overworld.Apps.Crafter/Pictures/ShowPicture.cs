using System.ComponentModel;

namespace Overworld.Apps.Crafter.Pictures;

public sealed record ShowPicture(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The picture's file, PNG, JPEG, GIF or WebP; a relative path is read from the project's folder.")]
    string File,
    [property: Description("A line under the picture saying what it shows.")]
    string? Caption = null);
