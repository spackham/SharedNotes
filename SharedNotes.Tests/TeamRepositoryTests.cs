using SharedNotes.Api.Models;
using SharedNotes.Api.Repositories;

namespace SharedNotes.Tests;

public sealed class TeamRepositoryTests
{
    [Fact]
    public async Task Create_TrimsAndReturnsTeamName()
    {
        var teams = new ConcurrentDictionaryTeamRepository();

        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "  Blue Team  " });

        Assert.Equal("Blue Team", team.Name);
        Assert.NotEmpty(team.Id);
        Assert.Empty(team.MemberUserIds);
    }

    [Fact]
    public async Task CreateRejectsBlankName()
    {
        var teams = new ConcurrentDictionaryTeamRepository();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            teams.CreateAsync(new CreateTeamRequest { Name = "   " }));
    }

    [Fact]
    public async Task CreateRejectsDuplicateNameIgnoringCase()
    {
        var teams = new ConcurrentDictionaryTeamRepository();
        await teams.CreateAsync(new CreateTeamRequest { Name = "Blue Team" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            teams.CreateAsync(new CreateTeamRequest { Name = "blue team" }));
    }

    [Fact]
    public async Task AddMemberIsIdempotentAndTrimsUserId()
    {
        var teams = new ConcurrentDictionaryTeamRepository();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Blue Team" });

        var first = await teams.AddMemberAsync(team.Id, "  alice  ");
        var duplicate = await teams.AddMemberAsync(team.Id, "alice");

        Assert.NotNull(first);
        Assert.Single(first!.MemberUserIds);
        Assert.Single(duplicate!.MemberUserIds);
        Assert.True(await teams.IsMemberAsync(team.Id, "alice"));
    }

    [Fact]
    public async Task AddMemberRejectsBlankUserId()
    {
        var teams = new ConcurrentDictionaryTeamRepository();
        var team = await teams.CreateAsync(new CreateTeamRequest { Name = "Blue Team" });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            teams.AddMemberAsync(team.Id, "   "));
    }

    [Fact]
    public async Task MissingTeamCannotBeFoundOrModified()
    {
        var teams = new ConcurrentDictionaryTeamRepository();

        Assert.Null(await teams.GetByIdAsync("missing"));
        Assert.False(await teams.IsMemberAsync("missing", "alice"));
        Assert.Null(await teams.AddMemberAsync("missing", "alice"));
    }

    [Fact]
    public async Task GetAllSortsTeamsByNameIgnoringCase()
    {
        var teams = new ConcurrentDictionaryTeamRepository();
        await teams.CreateAsync(new CreateTeamRequest { Name = "Zulu" });
        await teams.CreateAsync(new CreateTeamRequest { Name = "alpha" });

        var all = await teams.GetAllAsync();

        Assert.Equal(["alpha", "Zulu"], all.Select(team => team.Name));
    }
}
