using Overworld.Allay.ClaudeCode.Protocol;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;

namespace Overworld.Allay.ClaudeCode.Tests.Protocol;

public sealed class TranscriptTest
{
    [Fact]
    public void HearsWhichModelBegan()
    {
        Assert.Equal(
            [new Notice(NoticeKind.Model, "Opus 5.5")],
            new Transcript().Hear("""{"type":"system","subtype":"init","session_id":"s-1","model":"claude-opus-5-5"}"""));
    }

    [Fact]
    public void RemembersTheSession()
    {
        var transcript = new Transcript();

        transcript.Hear("""{"type":"system","subtype":"init","session_id":"s-1","model":"claude-opus-5-5"}""");
        transcript.Hear("""{"type":"rate_limit_event"}""");

        Assert.Equal("s-1", transcript.SessionId);
    }

    [Fact]
    public void HearsTheContextBeingSummarized()
    {
        Assert.Equal(
            [new Notice(NoticeKind.Context, "The conversation was summarized to fit the context.")],
            new Transcript().Hear("""{"type":"system","subtype":"compact_boundary"}"""));
    }

    [Theory]
    [InlineData("""{"type":"system","subtype":"init","session_id":"s-1"}""")]
    [InlineData("""{"type":"system","subtype":"status","status":"requesting"}""")]
    [InlineData("""{"type":"stream_event"}""")]
    [InlineData("""{"type":"stream_event","event":{"type":"message_start"}}""")]
    [InlineData("""{"type":"stream_event","event":{"type":"content_block_delta"}}""")]
    [InlineData("""{"type":"stream_event","event":{"type":"content_block_delta","delta":{"type":"text_delta","text":""}}}""")]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    [InlineData("""{"type":"user"}""")]
    [InlineData("""{"type":"user","message":{"content":"plain prompt"}}""")]
    [InlineData("""{"type":"assistant","message":"odd"}""")]
    [InlineData("""{"type":"control_request","request":{"subtype":"can_use_tool","tool_name":"Bash"}}""")]
    [InlineData("""{"type":"control_request","request_id":"r-1","request":{"subtype":"interrupt"}}""")]
    public void HearsNothingInWhatSaysNothing(string line)
    {
        Assert.Empty(new Transcript().Hear(line));
    }

    [Fact]
    public void HearsSpeechAndThoughts()
    {
        var transcript = new Transcript();

        Assert.Equal([new Speech("Olá")], transcript.Hear("""{"type":"stream_event","event":{"type":"content_block_delta","delta":{"type":"text_delta","text":"Olá"}}}"""));
        Assert.Equal([new Thought("hm")], transcript.Hear("""{"type":"stream_event","event":{"type":"content_block_delta","delta":{"type":"thinking_delta","thinking":"hm"}}}"""));
    }

    [Theory]
    [InlineData("end_turn", "finished")]
    [InlineData("max_tokens", "cutShort")]
    public void HearsHowAReplyEnded(string reason, string outcome)
    {
        var heard = Assert.Single(new Transcript().Hear($$"""{"type":"result","subtype":"success","is_error":false,"stop_reason":"{{reason}}"}"""));

        Assert.Equal(outcome, heard switch { Finished => "finished", CutShort => "cutShort", _ => "other" });
    }

    [Fact]
    public void HearsARefusalWithItsCategory()
    {
        Assert.Equal(
            [new Refused("cyber")],
            new Transcript().Hear("""{"type":"result","is_error":false,"stop_reason":"refusal","stop_details":{"category":"cyber"}}"""));
    }

    [Theory]
    [InlineData("""{"type":"result","subtype":"success","is_error":true,"result":"Not logged in"}""", "Not logged in")]
    [InlineData("""{"type":"result","subtype":"error_during_execution","is_error":true}""", "error_during_execution")]
    [InlineData("""{"type":"result","is_error":true}""", "Claude Code failed without saying why.")]
    public void HearsAFailureWithWhatItSaid(string line, string reason)
    {
        Assert.Equal([new Failed(reason)], new Transcript().Hear(line));
    }

    [Fact]
    public void HearsTheActsItMakes()
    {
        var heard = new Transcript("/home/hero/overworld").Hear(
            """{"type":"assistant","message":{"content":[{"type":"text","text":"ok"},{"type":"tool_use","id":"t-1","name":"Edit","input":{"file_path":"/home/hero/overworld/a.cs","old_string":"x","new_string":"y"}},{"type":"tool_use","name":"NoId"},"junk"]}}""");

        Assert.Equal([new Act("t-1", ActKind.Change, "a.cs", new Changes(1, 1))], heard);
    }

    [Fact]
    public void HearsWhatTheActsReturned()
    {
        var heard = new Transcript().Hear(
            """{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"t-1","content":"done"},{"type":"tool_result","tool_use_id":"t-2","is_error":true,"content":[{"type":"text","text":"no"},{"type":"image"},"x"]},{"type":"tool_result","tool_use_id":"t-3"},{"type":"tool_result"}]}}""");

        Assert.Equal([new ActResult("t-1", true, "done"), new ActResult("t-2", false, "no"), new ActResult("t-3", true, string.Empty)], heard);
    }

    [Fact]
    public void HearsAPlanFromItsTodosAndHidesTheirResult()
    {
        var transcript = new Transcript();

        var planned = transcript.Hear(
            """{"type":"assistant","message":{"content":[{"type":"tool_use","id":"t-1","name":"TodoWrite","input":{"todos":[{"content":"Run the tests","status":"completed"},{"content":"Fix them","status":"in_progress"},{"content":"Commit","status":"pending"},{"content":""}]}}]}}""");
        var result = transcript.Hear("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"t-1","content":"Todos updated"}]}}""");

        Assert.Equal(
            [new Plan([new PlanStep("Run the tests", StepState.Done), new PlanStep("Fix them", StepState.Doing), new PlanStep("Commit", StepState.Pending)])],
            planned);
        Assert.Empty(result);
    }

    [Fact]
    public void LeavesTheQuestionToolToItsRequest()
    {
        var transcript = new Transcript();

        Assert.Empty(transcript.Hear("""{"type":"assistant","message":{"content":[{"type":"tool_use","id":"t-1","name":"AskUserQuestion","input":{"questions":[]}}]}}"""));
        Assert.Empty(transcript.Hear("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"t-1","content":"answered"}]}}"""));
    }

    [Fact]
    public void HearsARequestForPermissionAndKeepsItsInput()
    {
        var transcript = new Transcript("/home/hero/overworld");

        var heard = transcript.Hear(
            """{"type":"control_request","request_id":"r-1","request":{"subtype":"can_use_tool","tool_name":"Bash","input":{"command":"dotnet test"},"description":"Run the tests"}}""");

        Assert.Equal([new Permission("r-1", ActKind.Command, "dotnet test", "Run the tests", [])], heard);
        Assert.Equal("""{"command":"dotnet test"}""", transcript.InputOf("r-1"));
        Assert.Equal("{}", transcript.InputOf("r-9"));
    }

    [Theory]
    [InlineData("""[{"type":"addRules","rules":[{"toolName":"WebFetch"}],"behavior":"allow","destination":"session"}]""", "WebFetch@Conversation")]
    [InlineData("""[{"type":"addRules","rules":[{"toolName":"Bash","ruleContent":"ls /tmp/x"}],"behavior":"allow","destination":"localSettings"},{"type":"addDirectories","directories":["/tmp/out"],"destination":"projectSettings"}]""", "Bash(ls /tmp/x)@Folder, /tmp/out@Folder")]
    [InlineData("""[{"type":"setMode","mode":"acceptEdits","destination":"session"}]""", "acceptEdits mode@Conversation")]
    [InlineData("""[{"type":"addRules","rules":[{"toolName":"Read"}],"behavior":"allow","destination":"userSettings"}]""", "Read@Everywhere")]
    [InlineData("""[{"type":"addRules","rules":[{"toolName":"Bash"}],"behavior":"deny","destination":"session"}]""", "")]
    public void SaysWhatAllowingAlwaysWouldRemember(string suggestions, string remembers)
    {
        var heard = new Transcript().Hear(
            """{"type":"control_request","request_id":"r-4","request":{"subtype":"can_use_tool","tool_name":"Bash","permission_suggestions":""" + suggestions + "}}");

        var permission = Assert.IsType<Permission>(Assert.Single(heard));

        Assert.Equal(remembers, string.Join(", ", permission.Remembers.Select(allowance => $"{allowance.What}@{allowance.Reach}")));
    }

    [Fact]
    public void HearsQuestionsForTheUser()
    {
        var transcript = new Transcript();

        var heard = transcript.Hear(
            """{"type":"control_request","request_id":"r-5","request":{"subtype":"can_use_tool","tool_name":"AskUserQuestion","input":{"questions":[{"question":"Which database?","header":"Storage","multiSelect":false,"options":[{"label":"SQLite","description":"A file"},{"label":"Postgres","description":"A server"}]},{"question":"Which extras?","header":"Extras","multiSelect":true,"options":[{"label":"Cache"}]}]}}}""");

        Assert.Equal(
            [
                new Questions(
                    "r-5",
                    [
                        new Question("Which database?", "Storage", false, [new Option("SQLite", "A file"), new Option("Postgres", "A server")]),
                        new Question("Which extras?", "Extras", true, [new Option("Cache", string.Empty)]),
                    ]),
            ],
            heard);
        Assert.Contains("Which database?", transcript.InputOf("r-5"));
    }

    [Fact]
    public void AsksPermissionWhenTheQuestionsMakeNoSense()
    {
        var heard = new Transcript().Hear("""{"type":"control_request","request_id":"r-6","request":{"subtype":"can_use_tool","tool_name":"AskUserQuestion","input":{"questions":"?"}}}""");

        Assert.Equal("r-6", Assert.IsType<Permission>(Assert.Single(heard)).Id);
    }

    [Fact]
    public void HearsARequestBeingWithdrawn()
    {
        Assert.Equal([new Withdrawn("r-1")], new Transcript().Hear("""{"type":"control_cancel_request","request_id":"r-1"}"""));
    }

    [Fact]
    public void TracksACommandLeftRunning()
    {
        var transcript = new Transcript();

        transcript.Hear("""{"type":"assistant","message":{"content":[{"type":"tool_use","id":"t-1","name":"Bash","input":{"command":"npm run dev","run_in_background":true}}]}}""");
        transcript.Hear("""{"type":"system","subtype":"task_started","task_id":"b-1","tool_use_id":"t-1","description":"Dev server","task_type":"local_bash"}""");

        Assert.True(transcript.JobsMoved);

        transcript.Hear("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"t-1","content":"Command running in background with ID: b-1. Output is being written to: /tmp/tasks/b-1.output. You will be notified."}]}}""");

        Assert.Equal([new Job("b-1", JobKind.Command, "Dev server", Prints: true)], transcript.Jobs);
        Assert.Equal("/tmp/tasks/b-1.output", transcript.OutputOf("b-1"));
    }

    [Fact]
    public void KnowsAMonitorByItsTool()
    {
        var transcript = new Transcript();

        transcript.Hear("""{"type":"assistant","message":{"content":[{"type":"tool_use","id":"t-1","name":"Monitor","input":{"command":"tail -f log"}}]}}""");
        transcript.Hear("""{"type":"system","subtype":"task_started","task_id":"b-1","tool_use_id":"t-1","description":"Watch the log","task_type":"local_bash"}""");

        Assert.Equal(JobKind.Monitor, Assert.Single(transcript.Jobs).Kind);
    }

    [Theory]
    [InlineData("""{"type":"system","subtype":"task_notification","task_id":"b-1","status":"completed"}""")]
    [InlineData("""{"type":"system","subtype":"task_updated","task_id":"b-1","patch":{"status":"killed"}}""")]
    [InlineData("""{"type":"system","subtype":"background_tasks_changed","tasks":[]}""")]
    public void ForgetsAJobOnceItSettles(string settled)
    {
        var transcript = new Transcript();
        transcript.Hear("""{"type":"system","subtype":"task_started","task_id":"b-1","description":"Tests","task_type":"local_agent"}""");

        transcript.Hear(settled);

        Assert.True(transcript.JobsMoved);
        Assert.Empty(transcript.Jobs);
    }

    [Fact]
    public void HearsWhichPromptStarted()
    {
        var transcript = new Transcript();

        transcript.Hear("""{"type":"command_lifecycle","command_uuid":"u-1","state":"queued"}""");
        Assert.Null(transcript.Started);

        transcript.Hear("""{"type":"command_lifecycle","command_uuid":"u-1","state":"started"}""");
        Assert.Equal("u-1", transcript.Started);
    }
}
