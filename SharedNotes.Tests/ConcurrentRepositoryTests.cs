using SharedNotes.Api.Models;
using SharedNotes.Api.Repositories;

namespace SharedNotes.Tests;

public sealed class ConcurrentRepositoryTests
{
    [Fact]
    public async Task ConcurrentUpdatesDoNotCorruptANote()
    {
        var notes = new ConcurrentDictionaryNoteRepository();
        var created = await notes.CreateAsync("team-a", "alice",
            new CreateNoteRequest { Title = "Initial", Content = "Initial" });

        await Task.WhenAll(Enumerable.Range(1, 100).Select(index =>
            notes.UpdateAsync(created.Id, new UpdateNoteRequest
            {
                Title = $"Title {index}",
                Content = $"Content {index}"
            })));

        var updated = await notes.GetByIdAsync(created.Id);
        Assert.NotNull(updated);
        Assert.StartsWith("Title ", updated!.Title);
        Assert.StartsWith("Content ", updated.Content);
    }

    [Fact]
    public async Task ConcurrentSharesPreserveUniqueUserAndTeamIds()
    {
        var notes = new ConcurrentDictionaryNoteRepository();
        var created = await notes.CreateAsync("team-a", "alice",
            new CreateNoteRequest { Title = "Shared", Content = "Details" });

        await Task.WhenAll(Enumerable.Range(1, 100).Select(index =>
            index % 2 == 0
                ? notes.ShareWithUserAsync(created.Id, "bob")
                : notes.ShareWithTeamAsync(created.Id, "team-b")));

        var updated = await notes.GetByIdAsync(created.Id);
        Assert.Single(updated!.SharedWithUserIds);
        Assert.Single(updated.SharedWithTeamIds);
    }

    [Fact]
    public async Task ConcurrentDeleteAndGetReturnsConsistentResult()
    {
        var notes = new ConcurrentDictionaryNoteRepository();
        var created = await notes.CreateAsync("team-a", "alice",
            new CreateNoteRequest { Title = "Delete", Content = "Details" });

        var deleteResults = await Task.WhenAll(Enumerable.Range(1, 100).Select(_ =>
            notes.DeleteAsync(created.Id)));

        Assert.Equal(1, deleteResults.Count(result => result));
        Assert.Null(await notes.GetByIdAsync(created.Id));
    }
}
