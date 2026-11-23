using SharedNotes.Api.Models;

namespace SharedNotes.Api.Services;

public interface INoteService
{
    Task<IReadOnlyCollection<NoteResponse>> GetVisibleAsync(string userId, CancellationToken ct = default);
    Task<NoteResponse?> GetAsync(Guid id, string userId, CancellationToken ct = default);
    Task<NoteResponse> CreateAsync(string ownerId, CreateNoteRequest request, CancellationToken ct = default);
    Task<NoteResponse?> UpdateAsync(Guid id, string userId, UpdateNoteRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct = default);
    Task<NoteResponse?> ShareAsync(Guid id, string ownerId, ShareNoteRequest request, CancellationToken ct = default);
}
