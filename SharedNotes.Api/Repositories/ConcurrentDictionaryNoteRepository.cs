using System.Collections.Concurrent;
using SharedNotes.Api.Models;

namespace SharedNotes.Api.Repositories;

public sealed class ConcurrentDictionaryNoteRepository : INoteRepository
{
    private readonly ConcurrentDictionary<Guid, NoteRecord> _notes = new();

    public Task<IReadOnlyCollection<NoteResponse>> GetVisibleToAsync(string teamId, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyCollection<NoteResponse> notes = _notes.Values
            .Where(note =>
                note.SharedWithUserIds.Contains(userId) ||
                note.SharedWithTeamIds.Contains(teamId) ||
                (note.TeamId == teamId && (note.OwnerUserId == userId || note.SharedWithUserIds.Contains(userId))))
            .OrderByDescending(note => note.UpdatedAt)
            .Select(ToResponse)
            .ToArray();
        return Task.FromResult(notes);
    }

    public Task<NoteResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_notes.TryGetValue(id, out var note) ? ToResponse(note) : null);
    }

    public Task<NoteResponse> CreateAsync(string teamId, string ownerUserId, CreateNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var note = new NoteRecord(Guid.NewGuid(), teamId, ownerUserId, request.Title.Trim(),
            request.Content.Trim(), Array.Empty<string>(), Array.Empty<string>(), now, now);
        _notes[note.Id] = note;
        return Task.FromResult(ToResponse(note));
    }

    public Task<bool> UpdateAsync(Guid id, UpdateNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!_notes.TryGetValue(id, out var current)) return Task.FromResult(false);
        var updated = current with { Title = request.Title.Trim(), Content = request.Content.Trim(), UpdatedAt = DateTimeOffset.UtcNow };
        return Task.FromResult(_notes.TryUpdate(id, updated, current));
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_notes.TryRemove(id, out _));
    }

    public Task<bool> ShareWithUserAsync(Guid id, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!_notes.TryGetValue(id, out var current)) return Task.FromResult(false);
        if (current.SharedWithUserIds.Contains(userId)) return Task.FromResult(true);
        var updated = current with
        {
            SharedWithUserIds = current.SharedWithUserIds.Concat([userId]).Distinct(StringComparer.Ordinal).ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return Task.FromResult(_notes.TryUpdate(id, updated, current));
    }

    public Task<bool> ShareWithTeamAsync(Guid id, string teamId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!_notes.TryGetValue(id, out var current)) return Task.FromResult(false);
        if (current.SharedWithTeamIds.Contains(teamId)) return Task.FromResult(true);
        var updated = current with
        {
            SharedWithTeamIds = current.SharedWithTeamIds.Concat([teamId]).Distinct(StringComparer.Ordinal).ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return Task.FromResult(_notes.TryUpdate(id, updated, current));
    }

    private static NoteResponse ToResponse(NoteRecord note) => new(note.Id, note.TeamId, note.OwnerUserId,
        note.Title, note.Content, note.SharedWithUserIds, note.SharedWithTeamIds, note.CreatedAt, note.UpdatedAt);

    private sealed record NoteRecord(
        Guid Id,
        string TeamId,
        string OwnerUserId,
        string Title,
        string Content,
        string[] SharedWithUserIds,
        string[] SharedWithTeamIds,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
