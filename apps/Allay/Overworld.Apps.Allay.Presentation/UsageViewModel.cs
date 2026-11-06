using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Metering;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Allay.Presentation;

public sealed partial class UsageViewModel(string agent, Operation<MeasureAgent, Usage> measure, TimeProvider clock) : ObservableObject
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public ObservableCollection<LimitRow> Limits { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBare))]
    public partial SpendingRow? Spending { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBare))]
    public partial string? Problem { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBare))]
    public partial bool IsMeasuring { get; private set; }

    [ObservableProperty]
    public partial string? Checked { get; private set; }

    public bool IsBare => !IsMeasuring && Problem is null && Limits.Count == 0 && Spending is null;

    [RelayCommand]
    public async Task MeasureAsync(CancellationToken cancellationToken)
    {
        IsMeasuring = true;

        try
        {
            var measured = await measure.ExecuteAsync(new MeasureAgent(agent), cancellationToken);
            var now = Local(clock.GetUtcNow());
            Limits.Clear();

            if (!measured.IsSuccess)
            {
                (Problem, Spending, Checked) = (measured.Error.Message, null, null);

                return;
            }

            foreach (var limit in measured.Value.Limits)
            {
                Limits.Add(new LimitRow(limit.Name, limit.Percent, limit.Resets is { } resets ? $"Resets {When(Local(resets), now)}" : null, limit.Close));
            }

            (Problem, Spending, Checked) = (null, Row(measured.Value.Spending), $"Checked at {Time(now)}");
        }
        finally
        {
            IsMeasuring = false;
        }
    }

    private DateTimeOffset Local(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, clock.LocalTimeZone);

    private static string When(DateTimeOffset moment, DateTimeOffset now) =>
        (moment.Date - now.Date).Days switch
        {
            <= 0 when moment - now < TimeSpan.FromHours(1) => $"in {Math.Max(1, (int)Math.Ceiling((moment - now).TotalMinutes))} min",
            <= 0 => $"today at {Time(moment)}",
            1 => $"tomorrow at {Time(moment)}",
            < 7 => $"{moment.ToString("ddd", Invariant)} at {Time(moment)}",
            _ => $"{moment.ToString("MMM d", Invariant)} at {Time(moment)}",
        };

    private static string Time(DateTimeOffset moment) => moment.ToString("h:mm tt", Invariant);

    private static SpendingRow? Row(Spending? spending) =>
        spending is null
            ? null
            : new SpendingRow(
                spending.Name,
                Money(spending.Spent, spending.Currency),
                spending.Cap is { } cap ? $"of {Money(cap, spending.Currency)}" : null,
                spending.Cap is decimal limit && limit > 0 ? (double)Math.Clamp(spending.Spent / limit * 100, 0, 100) : null);

    private static string Money(decimal amount, string currency) =>
        currency.ToUpperInvariant() switch
        {
            "USD" => $"${amount.ToString("0.00", Invariant)}",
            "EUR" => $"€{amount.ToString("0.00", Invariant)}",
            "GBP" => $"£{amount.ToString("0.00", Invariant)}",
            var code => $"{amount.ToString("0.00", Invariant)} {code}",
        };
}
