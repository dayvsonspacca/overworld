using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Remote;

public sealed record Envelope(
    string Op,
    string? Chat = null,
    int? Number = null,
    string? Agent = null,
    Brief? Brief = null,
    string? Session = null,
    IReadOnlyList<Turn>? Turns = null,
    bool? Keep = null,
    Prompt? Prompt = null,
    Setup? Setup = null,
    string? Request = null,
    Decision? Decision = null,
    string? Job = null,
    string? Stream = null,
    Moment? Moment = null,
    IReadOnlyList<Moment>? Pending = null,
    IReadOnlyList<Job>? Jobs = null,
    string? Output = null,
    string? Error = null,
    IReadOnlyList<string>? Opener = null);
