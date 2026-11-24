using SharedNotes.Api.Models;

namespace SharedNotes.Api.Repositories;

public interface INoteRepository
{
    Task<IReadOnlyCollection<NoteResponse>> GetVisibleToAsync(string teamId, string userId, CancellationToken ct = default);
    Task<NoteResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<NoteResponse> CreateAsync(string teamId, string ownerUserId, CreateNoteRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, UpdateNoteRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ShareWithUserAsync(Guid id, string userId, bool allowUpdates, CancellationToken ct = default);
    Task<bool> ShareWithTeamAsync(Guid id, string teamId, bool allowUpdates, CancellationToken ct = default);
    Task<bool> ShareAsync(Guid id, string? directUserId, string? sharedTeamId,
        bool allowUpdates, CancellationToken ct = default);
    Task<bool> RemoveUserShareAsync(Guid id, string sharedUserId, CancellationToken ct = default);
    Task<bool> RemoveTeamShareAsync(Guid id, string sharedTeamId, CancellationToken ct = default);
}
