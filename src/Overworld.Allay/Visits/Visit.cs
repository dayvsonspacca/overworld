using Overworld.Allay.Conversations;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Visits;

public sealed record Visit(string Agent, string Color, Setup Setup, Conversation Conversation);
