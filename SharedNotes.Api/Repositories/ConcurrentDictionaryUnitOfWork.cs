namespace SharedNotes.Api.Repositories;

public sealed class ConcurrentDictionaryUnitOfWork : IUnitOfWork
{
    public ConcurrentDictionaryUnitOfWork(INoteRepository notes, ITeamRepository teams)
    {
        Notes = notes;
        Teams = teams;
    }

    public INoteRepository Notes { get; }
    public ITeamRepository Teams { get; }

    public Task CommitAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
