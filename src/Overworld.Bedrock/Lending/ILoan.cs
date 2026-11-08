using System.ComponentModel;

namespace Overworld.Bedrock.Lending;

public interface ILoan : INotifyPropertyChanged
{
    string Key { get; }

    string Source { get; }

    string Title { get; }

    bool IsLive { get; set; }
}
