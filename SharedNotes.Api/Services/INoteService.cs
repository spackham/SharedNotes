using SharedNotes.Api.Models;

namespace SharedNotes.Api.Services;

public interface INoteService
{
    Task<IReadOnlyCollection<NoteResponse>> GetVisibleAsync(string teamId, string userId, CancellationToken ct = default);
    Task<NoteResponse?> GetAsync(Guid id, string teamId, string userId, CancellationToken ct = default);
    Task<NoteResponse> CreateAsync(string teamId, string ownerUserId, CreateNoteRequest request, CancellationToken ct = default);
    Task<NoteResponse?> UpdateAsync(Guid id, string teamId, string userId, UpdateNoteRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, string teamId, string userId, CancellationToken ct = default);
    Task<NoteResponse?> ShareAsync(Guid id, string teamId, string ownerUserId, ShareNoteRequest request, CancellationToken ct = default);
}
