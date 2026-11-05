using System.Reflection;
using System.Text.RegularExpressions;
using Overworld.Bedrock.Results;

namespace Overworld.Bedrock.Operations;

public abstract partial class Operation
{
    private protected static readonly SemaphoreSlim Gate = new(1, 1);

    private protected Operation(string name, string description, Type inputType)
    {
        Name = name;
        Description = description;
        InputType = inputType;
    }

    public string Name { get; }

    public string Description { get; }

    public Type InputType { get; }

    public abstract Task<Result<object?>> ExecuteAsync(object input, CancellationToken cancellationToken);

    public static Operation<TInput, TOutput> From<TInput, TOutput>(IOperation<TInput, TOutput> operation)
        where TInput : notnull
    {
        var type = operation.GetType();

        var attribute = type.GetCustomAttribute<OperationAttribute>()
            ?? throw new ArgumentException($"{type.Name} has no [Operation] attribute.", nameof(operation));

        if (!KebabCase().IsMatch(attribute.Name))
        {
            throw new ArgumentException(
                $"{type.Name} is named '{attribute.Name}', but operation names are kebab-case, like 'add-color'.",
                nameof(operation));
        }

        return new Operation<TInput, TOutput>(attribute.Name, attribute.Description, operation);
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(-[a-z0-9]+)*$")]
    private static partial Regex KebabCase();
}
