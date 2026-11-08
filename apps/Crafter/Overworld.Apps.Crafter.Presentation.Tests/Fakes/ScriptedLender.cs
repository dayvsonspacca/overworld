using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Bedrock.Lending;

namespace Overworld.Apps.Crafter.Presentation.Tests.Fakes;

public sealed partial class ScriptedLender : ILender
{
    public ILoan? Lend(JsonElement said) =>
        said.TryGetProperty("lent", out var key) && key.GetString() is { } lent
            ? new Loan(lent, said.TryGetProperty("title", out var title) ? title.GetString() ?? lent : lent)
            : null;

    public sealed partial class Loan(string key, string title) : ObservableObject, ILoan
    {
        public string Key => key;

        public string Source => "Scripted";

        public string Title => title;

        [ObservableProperty]
        public partial bool IsLive { get; set; }
    }
}
