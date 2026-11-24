using SharedNotes.Api.Models;

namespace SharedNotes.Api.Repositories;

public interface ITeamRepository
{
    Task<TeamResponse> CreateAsync(CreateTeamRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<TeamResponse>> GetAllAsync(CancellationToken ct = default);
    Task<TeamResponse?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<bool> IsMemberAsync(string teamId, string userId, CancellationToken ct = default);
    Task<TeamResponse?> AddMemberAsync(string teamId, string userId, CancellationToken ct = default);
}
