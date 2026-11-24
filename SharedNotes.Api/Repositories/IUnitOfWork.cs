namespace SharedNotes.Api.Repositories;

/// <summary>
/// Groups repository boundaries behind one application unit of work.
/// The current implementation is in-memory, but the abstraction permits a transactional EF Core unit of work later.
/// </summary>
public interface IUnitOfWork
{
    INoteRepository Notes { get; }
    ITeamRepository Teams { get; }

    Task CommitAsync(CancellationToken ct = default);
}
