using System.Collections.Concurrent;
using SharedNotes.Api.Models;

namespace SharedNotes.Api.Repositories;

public sealed class ConcurrentDictionaryNoteRepository : INoteRepository
{
    private readonly ConcurrentDictionary<Guid, NoteRecord> _notes = new();

    public IReadOnlyCollection<NoteResponse> GetVisibleTo(string userId) =>
        _notes.Values
            .Where(note => note.OwnerId == userId || note.SharedWith.Contains(userId))
            .OrderByDescending(note => note.UpdatedAt)
            .Select(ToResponse)
            .ToArray();

    public NoteResponse? GetById(Guid id) =>
        _notes.TryGetValue(id, out var note) ? ToResponse(note) : null;

    public NoteResponse Create(string ownerId, CreateNoteRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var note = new NoteRecord(Guid.NewGuid(), ownerId, request.Title.Trim(),
            request.Content.Trim(), new HashSet<string>(StringComparer.Ordinal), now, now);
        _notes[note.Id] = note;
        return ToResponse(note);
    }

    public bool Update(Guid id, UpdateNoteRequest request)
    {
        return _notes.AddOrUpdate(id,
            _ => throw new InvalidOperationException("The note was not found."),
            (_, note) => note with
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                UpdatedAt = DateTimeOffset.UtcNow
            }) is not null;
    }

    public bool Delete(Guid id) => _notes.TryRemove(id, out _);

    public bool Share(Guid id, string userId)
    {
        if (!_notes.TryGetValue(id, out _)) return false;

        _notes.AddOrUpdate(id,
            _ => throw new InvalidOperationException("The note was not found."),
            (_, note) =>
            {
                if (note.SharedWith.Contains(userId)) return note;

                var sharedWith = new HashSet<string>(note.SharedWith, StringComparer.Ordinal)
                {
                    userId
                };

                return note with
                {
                    SharedWith = sharedWith,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
            });

        return true;
    }

    private static NoteResponse ToResponse(NoteRecord note) => new(
        note.Id,
        note.OwnerId,
        note.Title,
        note.Content,
        note.SharedWith.ToArray(),
        note.CreatedAt,
        note.UpdatedAt);

    private sealed record NoteRecord(
        Guid Id,
        string OwnerId,
        string Title,
        string Content,
        HashSet<string> SharedWith,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
