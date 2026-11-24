using SharedNotes.Api.Repositories;
using SharedNotes.Api.Services;

namespace SharedNotes.Tests;

public sealed class SharedNotesTestFixture
{
    public (INoteService Service, ITeamRepository Teams) CreateService()
    {
        var teams = new ConcurrentDictionaryTeamRepository();
        var notes = new ConcurrentDictionaryNoteRepository();
        var unitOfWork = new ConcurrentDictionaryUnitOfWork(notes, teams);
        return (new NoteService(unitOfWork), teams);
    }
}
