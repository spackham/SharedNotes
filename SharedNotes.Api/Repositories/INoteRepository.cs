using SharedNotes.Api.Models;

namespace SharedNotes.Api.Repositories;

public interface INoteRepository
{
    IReadOnlyCollection<NoteResponse> GetVisibleTo(string userId);
    NoteResponse? GetById(Guid id);
    NoteResponse Create(string ownerId, CreateNoteRequest request);
    bool Update(Guid id, UpdateNoteRequest request);
    bool Delete(Guid id);
    bool Share(Guid id, string userId);
}
