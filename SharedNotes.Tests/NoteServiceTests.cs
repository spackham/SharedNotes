using SharedNotes.Api.Models;
using SharedNotes.Api.Repositories;
using SharedNotes.Api.Services;

namespace SharedNotes.Tests;

public sealed class NoteServiceTests
{
    [Fact]
    public async Task Create_MakesNoteVisibleToOwner()
    {
        var service = CreateService();
        var created = await service.CreateAsync("alice", NewCreateRequest("Trip", "Details"));

        var visible = await service.GetVisibleAsync("alice");

        Assert.Contains(visible, note => note.Id == created.Id);
    }

    [Fact]
    public async Task SharedUser_CanReadAndUpdateButCannotDelete()
    {
        var service = CreateService();
        var created = await service.CreateAsync("alice", NewCreateRequest("Trip", "Details"));
        await service.ShareAsync(created.Id, "alice", new ShareNoteRequest { UserId = "bob" });

        var read = await service.GetAsync(created.Id, "bob");
        var updated = await service.UpdateAsync(created.Id, "bob", new UpdateNoteRequest { Title = "Updated", Content = "Shared edit" });
        var deleted = await service.DeleteAsync(created.Id, "bob");

        Assert.NotNull(read);
        Assert.NotNull(updated);
        Assert.False(deleted);
    }

    [Fact]
    public async Task UserOutsideShareCannotReadOrUpdate()
    {
        var service = CreateService();
        var created = await service.CreateAsync("alice", NewCreateRequest("Secret", "Only Alice"));

        Assert.Null(await service.GetAsync(created.Id, "eve"));
        Assert.Null(await service.UpdateAsync(created.Id, "eve", new UpdateNoteRequest { Title = "Hack", Content = "No" }));
    }

    [Fact]
    public async Task OnlyOwnerCanShare()
    {
        var service = CreateService();
        var created = await service.CreateAsync("alice", NewCreateRequest("Trip", "Details"));
        await service.ShareAsync(created.Id, "alice", new ShareNoteRequest { UserId = "bob" });

        var forbidden = await service.ShareAsync(created.Id, "bob", new ShareNoteRequest { UserId = "eve" });

        Assert.Null(forbidden);
    }

    [Fact]
    public async Task OwnerCanDeleteOwnedNote()
    {
        var service = CreateService();
        var created = await service.CreateAsync("alice", NewCreateRequest("Trip", "Details"));

        Assert.True(await service.DeleteAsync(created.Id, "alice"));
        Assert.Null(await service.GetAsync(created.Id, "alice"));
    }

    [Fact]
    public async Task MissingNoteReturnsNullOrFalse()
    {
        var service = CreateService();
        var missing = Guid.NewGuid();

        Assert.Null(await service.GetAsync(missing, "alice"));
        Assert.Null(await service.UpdateAsync(missing, "alice", new UpdateNoteRequest { Title = "X", Content = "Y" }));
        Assert.False(await service.DeleteAsync(missing, "alice"));
        Assert.Null(await service.ShareAsync(missing, "alice", new ShareNoteRequest { UserId = "bob" }));
    }

    private static INoteService CreateService() =>
        new NoteService(new ConcurrentDictionaryNoteRepository());

    private static CreateNoteRequest NewCreateRequest(string title, string content) =>
        new() { Title = title, Content = content };
}
