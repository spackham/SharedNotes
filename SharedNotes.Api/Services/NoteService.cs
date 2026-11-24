using SharedNotes.Api.Models;
using SharedNotes.Api.Repositories;

namespace SharedNotes.Api.Services;

public sealed class NoteService : INoteService
{
    private readonly INoteRepository _notes;
    private readonly ITeamRepository _teams;

    public NoteService(INoteRepository notes, ITeamRepository teams)
    {
        _notes = notes;
        _teams = teams;
    }

    public async Task<IReadOnlyCollection<NoteResponse>> GetVisibleAsync(string teamId, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!await _teams.IsMemberAsync(teamId, userId, ct)) return Array.Empty<NoteResponse>();
        return await _notes.GetVisibleToAsync(teamId, userId, ct);
    }

    public async Task<NoteResponse?> GetAsync(Guid id, string teamId, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await _notes.GetByIdAsync(id, ct);
        if (note is null) return null;

        var isOwnerTeam = note.TeamId == teamId;
        var isMember = await _teams.IsMemberAsync(teamId, userId, ct);
        var canDirectRead = note.SharedWithUserIds.Contains(userId);
        var canTeamRead = isMember && note.SharedWithTeamIds.Contains(teamId);

        if (isOwnerTeam)
            return isMember || canDirectRead ? note : null;

        return canDirectRead || canTeamRead ? note : null;
    }

    public async Task<NoteResponse> CreateAsync(string teamId, string ownerUserId, CreateNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!await _teams.IsMemberAsync(teamId, ownerUserId, ct))
            throw new InvalidOperationException("The user is not a member of the team.");
        return await _notes.CreateAsync(teamId, ownerUserId, request, ct);
    }

    public async Task<NoteResponse?> UpdateAsync(Guid id, string teamId, string userId, UpdateNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await _notes.GetByIdAsync(id, ct);
        if (note is null) return null;

        var isMember = await _teams.IsMemberAsync(teamId, userId, ct);
        var canEdit = note.OwnerUserId == userId || note.SharedWithUserIds.Contains(userId) ||
            (isMember && (note.TeamId == teamId || note.SharedWithTeamIds.Contains(teamId)));
        if (!canEdit) return null;
        if (!await _notes.UpdateAsync(id, request, ct)) return null;
        return await _notes.GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, string teamId, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await _notes.GetByIdAsync(id, ct);
        if (note is null || note.TeamId != teamId || note.OwnerUserId != userId) return false;
        return await _notes.DeleteAsync(id, ct);
    }

    public async Task<NoteResponse?> ShareAsync(Guid id, string teamId, string ownerUserId, ShareNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await _notes.GetByIdAsync(id, ct);
        if (note is null || note.TeamId != teamId || note.OwnerUserId != ownerUserId) return null;

        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            var userId = request.UserId.Trim();
            if (userId == ownerUserId) return null;
            if (!await _notes.ShareWithUserAsync(id, userId, ct)) return null;
        }

        if (!string.IsNullOrWhiteSpace(request.TeamId))
        {
            var sharedTeamId = request.TeamId.Trim();
            if (sharedTeamId == teamId) return null;
            if (await _teams.GetByIdAsync(sharedTeamId, ct) is null) return null;
            if (!await _notes.ShareWithTeamAsync(id, sharedTeamId, ct)) return null;
        }

        if (string.IsNullOrWhiteSpace(request.UserId) && string.IsNullOrWhiteSpace(request.TeamId)) return null;
        return await _notes.GetByIdAsync(id, ct);
    }
}
