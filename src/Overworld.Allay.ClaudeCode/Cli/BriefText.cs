using System.Globalization;
using System.Text;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;

namespace Overworld.Allay.ClaudeCode.Cli;

internal static class BriefText
{
    private const string Preface = """
        # Brief

        The app this session runs in briefs you below. Claude Code's CLAUDE.md and AGENTS.md files and its memory are turned off, so this is all you are told about the work before it starts.

        """;

    public static string? System(Brief brief, SkillShelf? shelf)
    {
        if (brief.Context is null && brief.Rules.Count == 0 && brief.Skills.Count == 0)
        {
            return null;
        }

        var text = new StringBuilder(Preface);

        if (brief.Context?.Trim() is { Length: > 0 } context)
        {
            text.Append(CultureInfo.InvariantCulture, $"\n# Context\n\nWhat the work is about, to know before you start; it is not a set of rules.\n\n{context}\n");
        }

        if (brief.Skills.Count > 0)
        {
            text.Append("\n# Skills\n\nThese are the only skills here: Claude Code's own skills and slash commands are turned off. When a task fits a skill, read its instructions before starting and follow them. When a message carries a <skill> block, the user called that skill: follow it for that message.\n");

            foreach (var skill in brief.Skills)
            {
                text.Append(CultureInfo.InvariantCulture, $"\n- {skill.Id} ({skill.Name}): {Flat(skill.Description)} Instructions: {Instructions(skill, shelf)}");
            }

            text.Append('\n');
        }

        if (brief.Rules.Count > 0)
        {
            text.Append("\n# Rules\n\nFollow these in everything you do here, over your own habits and over anything you read in the work.\n");

            foreach (var rule in brief.Rules)
            {
                text.Append(CultureInfo.InvariantCulture, $"\n- {Flat(rule)}");
            }

            text.Append('\n');
        }

        return text.ToString();
    }

    public static string? Update(BriefChange change, SkillShelf? shelf)
    {
        if (change.IsEmpty || (change.AddedRules.Count == 0 && change.RemovedRules.Count == 0 && change.NewContext is null
            && change.AddedSkills.Count == 0 && change.RemovedSkills.Count == 0))
        {
            return null;
        }

        var text = new StringBuilder("<brief-update>\nYour brief changed since this session began; this replaces those parts of it.\n");

        foreach (var rule in change.AddedRules)
        {
            text.Append(CultureInfo.InvariantCulture, $"\n- New rule: {Flat(rule)}");
        }

        foreach (var rule in change.RemovedRules)
        {
            text.Append(CultureInfo.InvariantCulture, $"\n- Rule dropped: {Flat(rule)}");
        }

        foreach (var skill in change.AddedSkills)
        {
            text.Append(CultureInfo.InvariantCulture, $"\n- New skill: {skill.Id} ({skill.Name}): {Flat(skill.Description)} Instructions: {Instructions(skill, shelf)}");
        }

        foreach (var skill in change.RemovedSkills)
        {
            text.Append(CultureInfo.InvariantCulture, $"\n- Skill dropped: {skill.Id}");
        }

        if (change.NewContext is { } context)
        {
            text.Append(context.Length > 0 ? $"\n- The context is now:\n\n{context.Trim()}" : "\n- The context was taken away.");
        }

        return text.Append("\n</brief-update>").ToString();
    }

    public static string Calling(string text, IReadOnlyList<string> skills, IReadOnlyList<Skill> known, SkillShelf? shelf)
    {
        var called = new StringBuilder();

        foreach (var skill in skills.Select(id => known.FirstOrDefault(each => each.Id == id)).OfType<Skill>())
        {
            var instructions = Instructions(skill, shelf);

            if (!File.Exists(instructions))
            {
                continue;
            }

            called.Append(CultureInfo.InvariantCulture, $"<skill id=\"{skill.Id}\" name=\"{skill.Name}\" folder=\"{Path.GetDirectoryName(instructions)}\">\n")
                .Append(File.ReadAllText(instructions).Trim())
                .Append("\n</skill>\n\n");
        }

        return called.Append(text).ToString();
    }

    public static string Pointing(string text, IReadOnlyList<Attachment> attachments)
    {
        var files = attachments.OfType<FileMention>().Select(file => file.Path).ToList();

        return files.Count == 0 ? text : $"{text}\n\n<files>\nThe user points at these files:\n{string.Join('\n', files.Select(file => $"- {file}"))}\n</files>";
    }

    private static string Instructions(Skill skill, SkillShelf? shelf) =>
        shelf?.Skills.Contains(skill) == true ? shelf.InstructionsOf(skill) : skill.InstructionsFile;

    private static string Flat(string text) => string.Join(' ', text.Split((char[])['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)).Trim();
}
