using SharedNotes.Api.Models;
using SharedNotes.Api.Repositories;

namespace SharedNotes.Api.Services;

public sealed class NoteService : INoteService
{
    private readonly INoteRepository _repository;

    public NoteService(INoteRepository repository) => _repository = repository;

    public Task<IReadOnlyCollection<NoteResponse>> GetVisibleAsync(string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_repository.GetVisibleTo(userId));
    }

    public Task<NoteResponse?> GetAsync(Guid id, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = _repository.GetById(id);
        if (note is null || (note.OwnerId != userId && !note.SharedWith.Contains(userId))) return Task.FromResult<NoteResponse?>(null);
        return Task.FromResult<NoteResponse?>(note);
    }

    public Task<NoteResponse> CreateAsync(string ownerId, CreateNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_repository.Create(ownerId, request));
    }

    public Task<NoteResponse?> UpdateAsync(Guid id, string userId, UpdateNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = _repository.GetById(id);
        if (note is null || (note.OwnerId != userId && !note.SharedWith.Contains(userId))) return Task.FromResult<NoteResponse?>(null);
        if (!_repository.Update(id, request)) return Task.FromResult<NoteResponse?>(null);
        return Task.FromResult(_repository.GetById(id));
    }

    public Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = _repository.GetById(id);
        if (note is null || note.OwnerId != userId) return Task.FromResult(false);
        return Task.FromResult(_repository.Delete(id));
    }

    public Task<NoteResponse?> ShareAsync(Guid id, string ownerId, ShareNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = _repository.GetById(id);
        if (note is null) return Task.FromResult<NoteResponse?>(null);
        if (note.OwnerId != ownerId) return Task.FromResult<NoteResponse?>(null);

        var userId = request.UserId.Trim();
        if (userId.Length == 0 || userId == ownerId) return Task.FromResult<NoteResponse?>(null);
        if (!_repository.Share(id, userId)) return Task.FromResult<NoteResponse?>(null);
        return Task.FromResult(_repository.GetById(id));
    }
}
