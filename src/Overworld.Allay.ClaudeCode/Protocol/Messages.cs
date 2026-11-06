using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Overworld.Allay.Conversations;

namespace Overworld.Allay.ClaudeCode.Protocol;

internal static class Messages
{
    public const string Introduction = """{"type":"control_request","request_id":"overworld","request":{"subtype":"initialize"}}""";

    private const string Refusal = "The user denied this. Don't try it again; take another way or ask them how to proceed.";

    private const string Silence = "The user chose not to answer. Carry on with your best judgment or ask in plain text.";

    public static string Asking(string text, IReadOnlyList<Picture> pictures, string? id = null) =>
        Json(writer =>
        {
            writer.WriteString("type", "user");

            if (id is not null)
            {
                writer.WriteString("uuid", id);
            }

            writer.WriteStartObject("message");
            writer.WriteString("role", "user");

            if (pictures.Count == 0)
            {
                writer.WriteString("content", text);
            }
            else
            {
                writer.WriteStartArray("content");

                foreach (var picture in pictures)
                {
                    writer.WriteStartObject();
                    writer.WriteString("type", "image");
                    writer.WriteStartObject("source");
                    writer.WriteString("type", "base64");
                    writer.WriteString("media_type", picture.MediaType);
                    writer.WriteBase64String("data", picture.Data);
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }

                writer.WriteStartObject();
                writer.WriteString("type", "text");
                writer.WriteString("text", text);
                writer.WriteEndObject();
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        });

    public static string Interrupting(string request) =>
        Json(writer =>
        {
            writer.WriteString("type", "control_request");
            writer.WriteString("request_id", request);
            writer.WriteStartObject("request");
            writer.WriteString("subtype", "interrupt");
            writer.WriteEndObject();
        });

    public static string Stopping(string request, string job) =>
        Json(writer =>
        {
            writer.WriteString("type", "control_request");
            writer.WriteString("request_id", request);
            writer.WriteStartObject("request");
            writer.WriteString("subtype", "stop_task");
            writer.WriteString("task_id", job);
            writer.WriteEndObject();
        });

    public static string Answering(string request, Decision decision, string input, string? suggestions) =>
        Json(writer =>
        {
            writer.WriteString("type", "control_response");
            writer.WriteStartObject("response");
            writer.WriteString("subtype", "success");
            writer.WriteString("request_id", request);
            writer.WriteStartObject("response");

            switch (decision)
            {
                case Deny deny:
                    writer.WriteString("behavior", "deny");
                    writer.WriteString("message", Refused(deny.Reason, asked: false));
                    break;
                case Skip skip:
                    writer.WriteString("behavior", "deny");
                    writer.WriteString("message", Refused(skip.Reason, asked: true));
                    break;
                case Answers answers:
                    writer.WriteString("behavior", "allow");
                    writer.WritePropertyName("updatedInput");
                    Answered(writer, input, answers);
                    break;
                default:
                    writer.WriteString("behavior", "allow");
                    writer.WritePropertyName("updatedInput");
                    writer.WriteRawValue(input);

                    if (decision is AllowAlways && suggestions is not null)
                    {
                        writer.WritePropertyName("updatedPermissions");
                        writer.WriteRawValue(suggestions);
                    }

                    break;
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        });

    private static string Refused(string? reason, bool asked) =>
        reason?.Trim() is { Length: > 0 } said
            ? $"The user {(asked ? "chose not to answer" : "denied this")} and said: {said}"
            : asked ? Silence : Refusal;

    private static void Answered(Utf8JsonWriter writer, string input, Answers answers)
    {
        using var document = JsonDocument.Parse(input);
        writer.WriteStartObject();

        foreach (var property in document.RootElement.EnumerateObject().Where(property => property.Name != "answers"))
        {
            property.WriteTo(writer);
        }

        writer.WriteStartObject("answers");

        foreach (var answer in answers.Given)
        {
            writer.WriteString(
                answer.Question.Text,
                string.Join(", ", [.. answer.Picked.Select(option => option.Label), .. answer.Written is { } written ? [written] : Array.Empty<string>()]));
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static string Json(Action<Utf8JsonWriter> write)
    {
        using var buffer = new MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            write(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
