using System.Globalization;
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
                + "but the CLI reads only text, whole numbers, true or false, choices, lists of text and JSON objects for now.");
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

            var value = Value(property.PropertyType, args[++i]);

            if (value is null)
            {
                return new Error("cli.invalid_value", $"{arg} takes {Placeholder(property)}, not '{args[i]}'.");
            }

            json[key] = value;
        }

        var missing = properties.FirstOrDefault(property =>
            IsRequired(property) && !json.ContainsKey(JsonNamingPolicy.CamelCase.ConvertName(property.Name)));

        return missing is null
            ? json
            : new Error("cli.missing_option", $"{OptionName(missing)} is required.");
    }

    public static string OptionName(PropertyInfo property) =>
        "--" + JsonNamingPolicy.KebabCaseLower.ConvertName(property.Name);

    public static string Placeholder(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        return type switch
        {
            _ when IsListOfStrings(type) => "<item,item,...>",
            _ when type == typeof(int) => "<number>",
            _ when type == typeof(bool) => "<true|false>",
            _ when IsJson(type) => "<json>",
            { IsEnum: true } => $"<{string.Join('|', Enum.GetNames(type).Select(name => name.ToLowerInvariant()))}>",
            _ => "<text>",
        };
    }

    public static bool IsRequired(PropertyInfo property) =>
        property.DeclaringType!.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Any(parameter =>
                string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase)
                && !parameter.HasDefaultValue);

    private static bool IsSupported(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying == typeof(string) || underlying == typeof(int) || underlying == typeof(bool) || underlying.IsEnum || IsListOfStrings(underlying) || IsJson(underlying);
    }

    private static bool IsJson(Type type) =>
        type != typeof(string) && !IsListOfStrings(type) && (type.IsClass || type.IsInterface);

    private static JsonNode? Json(string text)
    {
        try
        {
            var node = JsonNode.Parse(text);

            return node is JsonObject or JsonArray ? node : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsListOfStrings(Type type) =>
        type != typeof(string) && typeof(IEnumerable<string>).IsAssignableFrom(type);

    private static JsonNode? Value(Type type, string text)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying switch
        {
            _ when IsListOfStrings(underlying) => new JsonArray([.. text.Split(',', StringSplitOptions.TrimEntries).Select(item => JsonValue.Create(item))]),
            _ when underlying == typeof(int) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? JsonValue.Create(number) : null,
            _ when underlying == typeof(bool) => bool.TryParse(text, out var flag) ? JsonValue.Create(flag) : null,
            _ when IsJson(underlying) => Json(text),
            { IsEnum: true } => Enum.GetNames(underlying).FirstOrDefault(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase)) is { } name ? JsonValue.Create(name) : null,
            _ => JsonValue.Create(text),
        };
    }
}
