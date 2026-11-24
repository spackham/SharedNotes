using System.Collections.Concurrent;
using SharedNotes.Api.Models;

namespace SharedNotes.Api.Repositories;

public sealed class ConcurrentDictionaryTeamRepository : ITeamRepository
{
    private readonly ConcurrentDictionary<string, TeamRecord> _teams = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _teamIdsByName =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<TeamResponse> CreateAsync(CreateTeamRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var name = request.Name.Trim();
        if (name.Length == 0) throw new ArgumentException("Team name is required.", nameof(request));

        var team = new TeamRecord(Guid.NewGuid().ToString("N"), name,
            Array.Empty<string>(), DateTimeOffset.UtcNow);

        if (!_teamIdsByName.TryAdd(name, team.Id))
            throw new InvalidOperationException("A team with that name already exists.");

        if (!_teams.TryAdd(team.Id, team))
        {
            _teamIdsByName.TryRemove(name, out _);
            throw new InvalidOperationException("The team could not be created.");
        }

        return Task.FromResult(ToResponse(team));
    }

    public Task<IReadOnlyCollection<TeamResponse>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyCollection<TeamResponse> teams = _teams.Values
            .OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToResponse).ToArray();
        return Task.FromResult(teams);
    }

    public Task<TeamResponse?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_teams.TryGetValue(id, out var team) ? ToResponse(team) : null);
    }

    public Task<bool> IsMemberAsync(string teamId, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_teams.TryGetValue(teamId, out var team) &&
            team.MemberUserIds.Contains(userId, StringComparer.Ordinal));
    }

    public Task<TeamResponse?> AddMemberAsync(string teamId, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var normalizedUserId = userId.Trim();
        if (normalizedUserId.Length == 0) throw new ArgumentException("User ID is required.", nameof(userId));

        for (var attempt = 0; attempt < 32; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            if (!_teams.TryGetValue(teamId, out var current)) return Task.FromResult<TeamResponse?>(null);
            if (current.MemberUserIds.Contains(normalizedUserId, StringComparer.Ordinal))
                return Task.FromResult<TeamResponse?>(ToResponse(current));

            var updated = current with
            {
                MemberUserIds = current.MemberUserIds.Concat([normalizedUserId]).Distinct(StringComparer.Ordinal).ToArray()
            };

            if (_teams.TryUpdate(teamId, updated, current))
                return Task.FromResult<TeamResponse?>(ToResponse(updated));
        }

        return Task.FromResult<TeamResponse?>(null);
    }

    private static TeamResponse ToResponse(TeamRecord team) => new(team.Id, team.Name,
        team.MemberUserIds, team.CreatedAt);

    private sealed record TeamRecord(string Id, string Name, string[] MemberUserIds, DateTimeOffset CreatedAt);
}
