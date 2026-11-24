using SharedNotes.Api.Models;
using SharedNotes.Api.Repositories;
using SharedNotes.Api.Services;

namespace SharedNotes.Tests;

public sealed class NoteServiceTests
{
    [Fact]
    public async Task TeamMember_CanCreateAndViewOwnedTeamNote()
    {
        var (service, teams) = CreateService();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });
        await teams.AddMemberAsync(team.Id, "alice");

        var created = await service.CreateAsync(team.Id, "alice", NewCreateRequest("Trip", "Details"));
        var visible = await service.GetVisibleAsync(team.Id, "alice");

        Assert.Equal(team.Id, created.TeamId);
        Assert.Equal("alice", created.OwnerUserId);
        Assert.Contains(visible, note => note.Id == created.Id);
    }

    [Fact]
    public async Task NonMember_CannotCreateNote()
    {
        var (service, teams) = CreateService();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(team.Id, "eve", NewCreateRequest("Secret", "Details")));
    }

    [Fact]
    public async Task DirectShare_AllowsAnotherUserToReadAndUpdate()
    {
        var (service, teams) = CreateService();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });
        await teams.AddMemberAsync(team.Id, "alice");
        await teams.AddMemberAsync(team.Id, "bob");
        var created = await service.CreateAsync(team.Id, "alice", NewCreateRequest("Trip", "Details"));

        await service.ShareAsync(created.Id, team.Id, "alice", new ShareNoteRequest { UserId = "bob" });

        Assert.NotNull(await service.GetAsync(created.Id, team.Id, "bob"));
        Assert.NotNull(await service.UpdateAsync(created.Id, team.Id, "bob",
            new UpdateNoteRequest { Title = "Updated", Content = "Shared edit" }));
        Assert.False(await service.DeleteAsync(created.Id, team.Id, "bob"));
    }

    [Fact]
    public async Task TeamShare_AllowsMembersOfTargetTeam()
    {
        var (service, teams) = CreateService();
        var source = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });
        var target = await teams.CreateAsync(new CreateTeamRequest { Name = "Blue Team" });
        await teams.AddMemberAsync(source.Id, "alice");
        await teams.AddMemberAsync(target.Id, "bob");
        var created = await service.CreateAsync(source.Id, "alice", NewCreateRequest("Trip", "Details"));

        await service.ShareAsync(created.Id, source.Id, "alice", new ShareNoteRequest { TeamId = target.Id });

        Assert.NotNull(await service.GetAsync(created.Id, target.Id, "bob"));
    }

    [Fact]
    public async Task TeamShare_DoesNotExposeNoteOutsideTargetTeam()
    {
        var (service, teams) = CreateService();
        var source = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });
        var target = await teams.CreateAsync(new CreateTeamRequest { Name = "Blue Team" });
        await teams.AddMemberAsync(source.Id, "alice");
        await teams.AddMemberAsync(target.Id, "bob");
        var created = await service.CreateAsync(source.Id, "alice", NewCreateRequest("Trip", "Details"));
        await service.ShareAsync(created.Id, source.Id, "alice", new ShareNoteRequest { TeamId = target.Id });

        Assert.Null(await service.GetAsync(created.Id, source.Id, "eve"));
        Assert.Null(await service.GetAsync(created.Id, target.Id, "eve"));
    }

    [Fact]
    public async Task OnlyOwnerCanShareOrDelete()
    {
        var (service, teams) = CreateService();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });
        await teams.AddMemberAsync(team.Id, "alice");
        await teams.AddMemberAsync(team.Id, "bob");
        var created = await service.CreateAsync(team.Id, "alice", NewCreateRequest("Trip", "Details"));

        Assert.Null(await service.ShareAsync(created.Id, team.Id, "bob", new ShareNoteRequest { UserId = "eve" }));
        Assert.False(await service.DeleteAsync(created.Id, team.Id, "bob"));
        Assert.True(await service.DeleteAsync(created.Id, team.Id, "alice"));
    }

    [Fact]
    public async Task MissingNoteReturnsNullOrFalse()
    {
        var (service, teams) = CreateService();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });
        await teams.AddMemberAsync(team.Id, "alice");
        var missing = Guid.NewGuid();

        Assert.Null(await service.GetAsync(missing, team.Id, "alice"));
        Assert.Null(await service.UpdateAsync(missing, team.Id, "alice",
            new UpdateNoteRequest { Title = "X", Content = "Y" }));
        Assert.False(await service.DeleteAsync(missing, team.Id, "alice"));
        Assert.Null(await service.ShareAsync(missing, team.Id, "alice", new ShareNoteRequest { UserId = "bob" }));
    }

    [Fact]
    public async Task Create_TrimsTitleAndContent()
    {
        var (service, teams) = CreateService();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });
        await teams.AddMemberAsync(team.Id, "alice");

        var created = await service.CreateAsync(team.Id, "alice", new CreateNoteRequest
        {
            Title = "  Trimmed title  ",
            Content = "  Trimmed content  "
        });

        Assert.Equal("Trimmed title", created.Title);
        Assert.Equal("Trimmed content", created.Content);
    }

    [Fact]
    public async Task SharingIsIdempotent()
    {
        var (service, teams) = CreateService();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });
        await teams.AddMemberAsync(team.Id, "alice");
        var created = await service.CreateAsync(team.Id, "alice", NewCreateRequest("Trip", "Details"));

        var first = await service.ShareAsync(created.Id, team.Id, "alice", new ShareNoteRequest { UserId = "bob" });
        var duplicate = await service.ShareAsync(created.Id, team.Id, "alice", new ShareNoteRequest { UserId = "bob" });

        Assert.Single(first!.SharedWithUserIds);
        Assert.Single(duplicate!.SharedWithUserIds);
    }

    [Fact]
    public async Task ConcurrentCreates_PreserveEveryTeamNote()
    {
        var (service, teams) = CreateService();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });
        await teams.AddMemberAsync(team.Id, "alice");
        var tasks = Enumerable.Range(1, 100)
            .Select(index => service.CreateAsync(team.Id, "alice", NewCreateRequest($"Note {index}", "Details")));

        var created = await Task.WhenAll(tasks);
        var visible = await service.GetVisibleAsync(team.Id, "alice");

        Assert.Equal(100, created.Select(note => note.Id).Distinct().Count());
        Assert.Equal(100, visible.Count);
    }

    [Fact]
    public async Task CanceledOperation_ThrowsOperationCanceledException()
    {
        var (service, teams) = CreateService();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });
        await teams.AddMemberAsync(team.Id, "alice");
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetVisibleAsync(team.Id, "alice", source.Token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ShareRejectsBlankIdentifiers(string userId)
    {
        var (service, teams) = CreateService();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Red Team" });
        await teams.AddMemberAsync(team.Id, "alice");
        var created = await service.CreateAsync(team.Id, "alice", NewCreateRequest("Trip", "Details"));

        Assert.Null(await service.ShareAsync(created.Id, team.Id, "alice", new ShareNoteRequest { UserId = userId }));
    }

    private static (INoteService Service, ITeamRepository Teams) CreateService()
    {
        var teams = new ConcurrentDictionaryTeamRepository();
        var notes = new ConcurrentDictionaryNoteRepository();
        return (new NoteService(notes, teams), teams);
    }

    private static CreateNoteRequest NewCreateRequest(string title, string content) =>
        new() { Title = title, Content = content };
}
