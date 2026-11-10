using System.ComponentModel;
using System.Diagnostics;
using Overworld.Allay.ClaudeCode;
using Overworld.Allay.Hiring;
using Overworld.Allay.Remote;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Allay.Bells;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Pictures;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Presentation;
using Overworld.Apps.Crafter.Presentation.Gate;
using Overworld.Apps.Crafter.Presentation.Workbench;
using Overworld.Apps.Crafter.Desktop.Summoning;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Sessions;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Apps.Crafter.Todos;
using Overworld.Apps.Dye.Palettes;
using Overworld.Apps.Dye.Presentation.Lending;
using Overworld.Apps.Dye.Themes;
using Overworld.Apps.Painting.Artworks;
using Overworld.Apps.Painting.Presentation.Lending;
using Overworld.Beacon;
using Overworld.Bedrock.Apps;
using Overworld.Bell;
using Overworld.Bedrock.Operations;
using Overworld.Chest;
using Overworld.Hosting;
using Overworld.Mcp;
using Overworld.Redstone;
using Overworld.Web;
using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Chats;
using Overworld.Apps.Crafter.Presentation.Conversations;
using Overworld.Apps.Crafter.Presentation.Composing;
using Overworld.Apps.Crafter.Presentation.Projects;
using System.Text.Json.Nodes;
using Overworld.Allay.Presentation.Conversations;
using Overworld.Allay.Presentation.Chats;
using Overworld.Allay.Visits;

var app = new AppIdentity("crafter");
var data = DataDirectory.For(app.Name);
var allay = DataDirectory.For("allay");
var shelf = new Shelf(data);
var settings = new SettingsStore();
var todos = new TodoList(shelf);
var axioms = new AxiomBook(shelf, settings);
var summaries = new SummaryStore();
var preferences = new PreferencesStore(data, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
var library = new SkillLibrary(preferences, new Mirror(Path.Combine(data, "library")));
var skills = new ProjectSkills(shelf, library);
var pictures = new PictureStore(Path.Combine(data, "pictures"));
var chats = new ChatStore(Path.Combine(data, "chats"), pictures);
var artworks = new ArtworkStore(DataDirectory.For("painting"));
var painting = new PaintingLender(Operation.From(new ShowArtworkOp(artworks)));
var palettes = new PaletteStore(DataDirectory.For("dye"));
var config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "overworld");
var themes = new ThemeStore(Path.Combine(config, "themes"), Path.Combine(config, "theme.json"));
var lit = new BeaconClient(new RedstoneClient(SocketPath.Resolve()));
var paint = Operation.From(new PaintArtworkOp(artworks, lit, TimeProvider.System));
var restore = Operation.From(new RestoreArtworkOp(artworks, lit, TimeProvider.System));
var staff = new StaffFile(allay);
using var tools = new McpHttpHost(app.Name);
var crew = new Crew([new ClaudeCode(Path.Combine(allay, "claude-code", "skills"))], staff);
var editProject = Operation.From(new EditProjectOp(shelf, settings));
var listTodos = Operation.From(new ListTodosOp(todos));
var addTodo = Operation.From(new AddTodoOp(todos));
var completeTodo = Operation.From(new CompleteTodoOp(todos));
var removeTodo = Operation.From(new RemoveTodoOp(todos));
var giveSkill = Operation.From(new GiveSkillOp(skills));
var showLibrary = Operation.From(new ShowLibraryOp(library));
var saveSkill = Operation.From(new SaveSkillOp(library));
var toolbox = new Toolbox(
    tools,
    [
        listTodos,
        showLibrary,
        Operation.From(new ListChatsOp(chats)),
        Operation.From(new ReadChatOp(chats)),
        Operation.From(new ShowPictureOp(pictures)),
        paint,
        Toolbox.Borrow("painting", Operation.From(new ListArtworksOp(artworks))),
        Toolbox.Borrow("painting", Operation.From(new ShowArtworkOp(artworks))),
        Toolbox.Borrow("painting", restore),
        Toolbox.Borrow("painting", Operation.From(new GroupArtworkOp(artworks, lit))),
        Toolbox.Borrow("dye", Operation.From(new ListPalettesOp(palettes))),
        Toolbox.Borrow("dye", Operation.From(new ShowPaletteOp(palettes))),
        Toolbox.Borrow("dye", Operation.From(new ListThemesOp(themes))),
        Toolbox.Borrow("dye", Operation.From(new ShowThemeOp(themes))),
    ],
    [addTodo, completeTodo, removeTodo, editProject, giveSkill, saveSkill],
    axioms,
    summaries,
    Environment.ProcessPath);

var summoned = Presence.Read(args);

if (summoned is not null)
{
    args = [];
}

if (args is [Toolbox.Serve, var served])
{
    await toolbox.ServeAsync(served, () => lit.LightAsync(Toolbox.Signal, CancellationToken.None).GetAwaiter().GetResult(), CancellationToken.None);

    return 0;
}
using var link = new AllayLink();
var workshop = new Workshop(crew, new ProjectBrief(summaries, axioms, skills), toolbox, chats, link, (folder, chat) => Presence.Opener(Environment.ProcessPath, folder, chat));
var ringer = new Ringer(new BellClient(new RedstoneClient(SocketPath.Resolve())), "crafter");
var attention = new Attention();
Overworld.Allay.Briefs.ToolServer? window = null;

var abilities = new Abilities
{
    ListProjects = Operation.From(new ListProjectsOp(shelf, settings)),
    AddProject = Operation.From(new AddProjectOp(shelf, settings)),
    EditProject = editProject,
    RemoveProject = Operation.From(new RemoveProjectOp(shelf, settings)),
    MoveProject = Operation.From(new MoveProjectOp(shelf, settings)),
    ListTodos = listTodos,
    AddTodo = addTodo,
    CompleteTodo = completeTodo,
    RemoveTodo = removeTodo,
    ListAxioms = Operation.From(new ListAxiomsOp(axioms)),
    AddAxiom = Operation.From(new AddAxiomOp(axioms)),
    RemoveAxiom = Operation.From(new RemoveAxiomOp(axioms)),
    ShowSummary = Operation.From(new ShowSummaryOp(shelf, summaries)),
    WriteSummary = Operation.From(new WriteSummaryOp(shelf, summaries)),
    ListProjectSkills = Operation.From(new ListProjectSkillsOp(skills)),
    GiveSkill = giveSkill,
    ShowLibrary = showLibrary,
    LinkLibrary = Operation.From(new LinkLibraryOp(library)),
    SyncLibrary = Operation.From(new SyncLibraryOp(library)),
    SetDefaultSkill = Operation.From(new SetDefaultSkillOp(library)),
    ShowPreferences = Operation.From(new ShowPreferencesOp(preferences)),
    SetProjectsRoot = Operation.From(new SetProjectsRootOp(preferences)),
    Chats = chats,
    Pictures = pictures,
    Lenders = [painting, new DyeLender()],
    Workshop = workshop,
    Ringer = ringer,
    Attention = attention,
    OpenAllay = OpenAllay,
};

return await AppHost.RunAsync(
    app.Command,
    [
        abilities.ListProjects,
        abilities.AddProject,
        abilities.EditProject,
        abilities.RemoveProject,
        abilities.MoveProject,
        abilities.ListAxioms,
        abilities.AddAxiom,
        abilities.RemoveAxiom,
        abilities.ShowSummary,
        abilities.WriteSummary,
        abilities.ListTodos,
        abilities.AddTodo,
        abilities.CompleteTodo,
        abilities.RemoveTodo,
        abilities.ShowPreferences,
        abilities.SetProjectsRoot,
        abilities.ShowLibrary,
        abilities.LinkLibrary,
        abilities.SyncLibrary,
        saveSkill,
        abilities.SetDefaultSkill,
        abilities.ListProjectSkills,
        abilities.GiveSkill,
    ],
    args,
    withoutArgs: () =>
    {
        using var presence = Presence.Claim();

        if (presence is null)
        {
            if (summoned is { } summons)
            {
                Presence.Leave(summons);
            }

            lit.LightAsync(Presence.Signal, CancellationToken.None).GetAwaiter().GetResult();

            return Task.FromResult(0);
        }

        tools.Start();

        var code = WebHost.Run(app, page =>
        {
            page.Leaving.Add(async token =>
            {
                await ringer.WithdrawAllAsync(token);
                workshop.LeaveAll();
            });

            Hide(page.Mirror);

            var chat = new ChatViewModel(new Lobby(crew, link), HomeBriefAsync, abilities.Lenders, HomeBrief.Suggestions, ringer, "Crafter");
            page.Leaving.Add(_ =>
            {
                chat.End();

                return Task.CompletedTask;
            });
            var workbench = new WorkbenchViewModel(abilities, chat: chat);
            window = toolbox.Lend(
                HomeBrief.Window,
                [Operation.From(new OpenChatOp(shelf, (folder, prompt) => page.Loop.RunAsync(() => workbench.OpenChatAsync(folder, prompt)).Unwrap()))],
                HomeBrief.WindowInstructions);
            _ = OpenAsync(workbench, attention, summoned);
            _ = FollowSummonsAsync(attention, page);

            var gate = new GateViewModel(Operation.From(new ListAgentsOp(crew)), Operation.From(new CheckAgentOp(crew)), abilities.OpenAllay);
            _ = gate.LoadAsync(CancellationToken.None);
            _ = gate.FollowAsync(new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName)).WatchAsync(CancellationToken.None), CancellationToken.None);
            _ = painting.FollowAsync(new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName)).WatchAsync(CancellationToken.None), CancellationToken.None);
            _ = ringer.FollowAsync(new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName)).WatchAsync(CancellationToken.None), CancellationToken.None);
            _ = FollowKeptAsync(workshop);

            page.Activated += (_, _) => attention.IsActive = chat.IsAttended = true;
            page.Deactivated += (_, _) => attention.IsActive = chat.IsAttended = false;
            chat.Summoned += (_, _) => page.Summon();
            attention.Summoned += (_, _) => page.Summon();

            page.Handle("attach", asked =>
            {
                if (Conversation(page.Mirror, asked) is { } conversation)
                {
                    foreach (var path in asked?["paths"]?.AsArray().Select(each => each?.GetValue<string>()).OfType<string>() ?? [])
                    {
                        conversation.Attach(Attachment(path));
                    }
                }
            });
            page.Handle("attach-picture", asked =>
            {
                if (Conversation(page.Mirror, asked) is { } conversation && asked?["data"]?.GetValue<string>() is { Length: > 0 } data)
                {
                    conversation.Attach(new Picture(asked["type"]?.GetValue<string>() ?? "image/png", Convert.FromBase64String(data)));
                }
            });
            page.Handle("open-outside", asked =>
            {
                if (asked?["path"]?.GetValue<string>() is { Length: > 0 } path && File.Exists(path))
                {
                    using var opened = Process.Start(new ProcessStartInfo("xdg-open") { ArgumentList = { path }, UseShellExecute = false });
                }
            });

            page.Show(new { Workbench = workbench, Gate = gate });
        });

        workshop.LeaveAll();

        return Task.FromResult(code);
    });

async Task<Overworld.Allay.Briefs.Brief> HomeBriefAsync(CancellationToken cancellationToken)
{
    var read = await preferences.ReadAsync(cancellationToken);
    var root = read.IsSuccess && read.Value.ProjectsRoot is { } kept ? kept : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    return HomeBrief.Of(root, Environment.ProcessPath ?? app.Command, window ?? throw new InvalidOperationException("Crafter's window has not opened."));
}

static async Task OpenAsync(WorkbenchViewModel workbench, Attention attention, Summons? summoned)
{
    await workbench.LoadAsync(CancellationToken.None);

    if (summoned is { } summons)
    {
        attention.Summon(summons.Folder, summons.Chat);
    }
}

static async Task FollowSummonsAsync(Attention attention, Overworld.Web.Page page)
{
    await foreach (var signal in new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName)).WatchAsync(CancellationToken.None))
    {
        if (signal != Presence.Signal)
        {
            continue;
        }

        if (Presence.Take() is { } summons)
        {
            attention.Summon(summons.Folder, summons.Chat);
        }
        else
        {
            page.Summon();
        }
    }
}

static async Task FollowKeptAsync(Workshop workshop)
{
    await foreach (var signal in new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName)).WatchAsync(CancellationToken.None))
    {
        if (signal == Toolbox.Signal)
        {
            await workshop.KeptEverywhereAsync();
        }
    }
}

static void Hide(Overworld.Web.Mirroring.Mirror mirror)
{
    mirror.Ignore<ChatSummaryViewModel>(nameof(ChatSummaryViewModel.Record), nameof(ChatSummaryViewModel.Conversation));
    mirror.Ignore<ConversationViewModel>(nameof(ConversationViewModel.Record), nameof(ConversationViewModel.Setup));

    foreach (var (type, property) in Hidden.Properties)
    {
        mirror.Ignore(type, property);
    }

    mirror.Ignore<SummaryProposalEntry>(nameof(SummaryProposalEntry.Blocks));
    mirror.Ignore<OverviewViewModel>(nameof(OverviewViewModel.Blocks), nameof(OverviewViewModel.Project));
    mirror.Ignore<AttachmentView>(nameof(AttachmentView.Attachment));
}

static ConversationViewModel? Conversation(Overworld.Web.Mirroring.Mirror mirror, JsonNode? asked) =>
    asked?["conversation"]?["$ref"]?.GetValue<int>() is { } id ? mirror.Find(id) as ConversationViewModel : null;

static Attachment Attachment(string path)
{
    var kind = Path.GetExtension(path).ToUpperInvariant() switch
    {
        ".PNG" => "image/png",
        ".JPG" or ".JPEG" => "image/jpeg",
        ".GIF" => "image/gif",
        ".WEBP" => "image/webp",
        _ => null,
    };

    if (kind is not null)
    {
        try
        {
            return new Picture(kind, File.ReadAllBytes(path));
        }
        catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
        {
        }
    }

    return new FileMention(path);
}

static bool OpenAllay()
{
    try
    {
        using var allay = Process.Start(new ProcessStartInfo("overworld:allay") { UseShellExecute = false });

        return allay is not null;
    }
    catch (Win32Exception)
    {
        return false;
    }
}
