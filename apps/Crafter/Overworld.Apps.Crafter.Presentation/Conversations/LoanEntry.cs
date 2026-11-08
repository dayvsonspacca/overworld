using Overworld.Bedrock.Lending;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class LoanEntry(ILoan loan) : Entry
{
    public ILoan Loan { get; } = loan;
}
