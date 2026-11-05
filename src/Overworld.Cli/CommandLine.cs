using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Overworld.Bedrock.Results;

namespace Overworld.Cli;

public static class CommandLine
{
    public static Result<JsonObject> ReadOptions(Type inputType, IReadOnlyList<string> args)
    {
        var properties = inputType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties.Where(property => !IsSupported(property.PropertyType)))
        {
            throw new NotSupportedException(
                $"{inputType.Name}.{property.Name} is a {property.PropertyType.Name}, "
                + "but the CLI reads only strings and lists of strings for now.");
        }

        var options = properties.ToDictionary(OptionName);
        var json = new JsonObject();

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];

            if (!options.TryGetValue(arg, out var property))
            {
                return arg.StartsWith("--", StringComparison.Ordinal)
                    ? new Error("cli.unknown_option", $"There is no option {arg}.")
                    : new Error("cli.unexpected_argument", $"Unexpected '{arg}'. Values come after an option, like --name Sea.");
            }

            var key = JsonNamingPolicy.CamelCase.ConvertName(property.Name);

            if (json.ContainsKey(key))
            {
                return new Error("cli.repeated_option", $"{arg} is given more than once.");
            }

            if (i + 1 == args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                return new Error("cli.missing_value", $"{arg} needs a value.");
            }

            json[key] = Value(property.PropertyType, args[++i]);
        }

        var missing = properties.FirstOrDefault(property =>
            IsRequired(property) && !json.ContainsKey(JsonNamingPolicy.CamelCase.ConvertName(property.Name)));

        return missing is null
            ? json
            : new Error("cli.missing_option", $"{OptionName(missing)} is required.");
    }

    public static string OptionName(PropertyInfo property) =>
        "--" + JsonNamingPolicy.KebabCaseLower.ConvertName(property.Name);

    public static string Placeholder(PropertyInfo property) =>
        IsListOfStrings(property.PropertyType) ? "<item,item,...>" : "<text>";

    public static bool IsRequired(PropertyInfo property) =>
        property.DeclaringType!.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Any(parameter =>
                string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase)
                && !parameter.HasDefaultValue);

    private static bool IsSupported(Type type) => type == typeof(string) || IsListOfStrings(type);

    private static bool IsListOfStrings(Type type) =>
        type != typeof(string) && typeof(IEnumerable<string>).IsAssignableFrom(type);

    private static JsonNode Value(Type type, string text) =>
        IsListOfStrings(type)
            ? new JsonArray([.. text.Split(',', StringSplitOptions.TrimEntries).Select(item => JsonValue.Create(item))])
            : JsonValue.Create(text);
}
