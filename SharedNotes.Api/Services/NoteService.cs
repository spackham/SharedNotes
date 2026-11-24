using SharedNotes.Api.Models;
using SharedNotes.Api.Repositories;

namespace SharedNotes.Api.Services;

public sealed class NoteService : INoteService
{
    private readonly IUnitOfWork _unitOfWork;

    public NoteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<NoteResponse>> GetVisibleAsync(string teamId, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!await IsTeamMemberAsync(teamId, userId, ct)) return Array.Empty<NoteResponse>();
        return await _unitOfWork.Notes.GetVisibleToAsync(teamId, userId, ct);
    }

    public async Task<NoteResponse?> GetAsync(Guid id, string teamId, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await GetNoteAsync(id, ct);
        if (note is null) return null;
        return await CanReadAsync(note, teamId, userId, ct) ? note : null;
    }

    public async Task<NoteResponse> CreateAsync(string teamId, string ownerUserId, CreateNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!await IsTeamMemberAsync(teamId, ownerUserId, ct))
            throw new InvalidOperationException("The user is not a member of the team.");

        var created = await _unitOfWork.Notes.CreateAsync(teamId, ownerUserId, request, ct);
        await _unitOfWork.CommitAsync(ct);
        return created;
    }

    public async Task<NoteResponse?> UpdateAsync(Guid id, string teamId, string userId, UpdateNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await GetNoteAsync(id, ct);
        if (note is null) return null;
        if (!await CanUpdateAsync(note, teamId, userId, ct)) return null;
        if (!await _unitOfWork.Notes.UpdateAsync(id, request, ct)) return null;

        await _unitOfWork.CommitAsync(ct);
        return await GetNoteAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, string teamId, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await GetNoteAsync(id, ct);
        if (note is null || !CanDelete(note, teamId, userId)) return false;

        var deleted = await _unitOfWork.Notes.DeleteAsync(id, ct);
        if (deleted) await _unitOfWork.CommitAsync(ct);
        return deleted;
    }

    public async Task<NoteResponse?> ShareAsync(Guid id, string teamId, string ownerUserId, ShareNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await GetNoteAsync(id, ct);
        if (note is null || !CanShare(note, teamId, ownerUserId)) return null;

        string? directUserId = null;
        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            directUserId = request.UserId.Trim();
            if (directUserId == ownerUserId) return null;
        }

        string? sharedTeamId = null;
        if (!string.IsNullOrWhiteSpace(request.TeamId))
        {
            sharedTeamId = request.TeamId.Trim();
            if (sharedTeamId == teamId) return null;
            if (!await TeamExistsAsync(sharedTeamId, ct)) return null;
        }

        if (directUserId is null && sharedTeamId is null) return null;

        if (!await _unitOfWork.Notes.ShareAsync(id, directUserId, sharedTeamId,
            request.AllowUpdates, ct)) return null;

        await _unitOfWork.CommitAsync(ct);
        return await GetNoteAsync(id, ct);
    }

    public async Task<bool> RemoveUserShareAsync(Guid id, string teamId, string ownerUserId,
        string sharedUserId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await GetNoteAsync(id, ct);
        if (note is null || !CanShare(note, teamId, ownerUserId)) return false;

        var normalizedUserId = sharedUserId.Trim();
        if (normalizedUserId.Length == 0) return false;

        var removed = await _unitOfWork.Notes.RemoveUserShareAsync(id, normalizedUserId, ct);
        if (removed) await _unitOfWork.CommitAsync(ct);
        return removed;
    }

    public async Task<bool> RemoveTeamShareAsync(Guid id, string teamId, string ownerUserId,
        string sharedTeamId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await GetNoteAsync(id, ct);
        if (note is null || !CanShare(note, teamId, ownerUserId)) return false;

        var normalizedTeamId = sharedTeamId.Trim();
        if (normalizedTeamId.Length == 0) return false;

        var removed = await _unitOfWork.Notes.RemoveTeamShareAsync(id, normalizedTeamId, ct);
        if (removed) await _unitOfWork.CommitAsync(ct);
        return removed;
    }

    private Task<NoteResponse?> GetNoteAsync(Guid id, CancellationToken ct) =>
        _unitOfWork.Notes.GetByIdAsync(id, ct);

    private Task<bool> IsTeamMemberAsync(string teamId, string userId, CancellationToken ct) =>
        _unitOfWork.Teams.IsMemberAsync(teamId, userId, ct);

    private async Task<bool> TeamExistsAsync(string teamId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await _unitOfWork.Teams.GetByIdAsync(teamId, ct) is not null;
    }

    private async Task<bool> CanReadAsync(NoteResponse note, string teamId, string userId, CancellationToken ct)
    {
        if (note.TeamId == teamId)
        {
            var isMember = await IsTeamMemberAsync(teamId, userId, ct);
            return isMember || note.SharedWithUserIds.Contains(userId);
        }

        var isTargetTeamMember = await IsTeamMemberAsync(teamId, userId, ct);
        return isTargetTeamMember && note.SharedWithTeamIds.Contains(teamId);
    }

    private async Task<bool> CanUpdateAsync(NoteResponse note, string teamId, string userId, CancellationToken ct)
    {
        if (note.TeamId == teamId)
        {
            var isOwnerTeamMember = await IsTeamMemberAsync(teamId, userId, ct);
            if (!isOwnerTeamMember) return false;
            if (note.OwnerUserId == userId) return true;
            return note.AllowedUpdateUserIds.Contains(userId);
        }

        var isTargetTeamMember = await IsTeamMemberAsync(teamId, userId, ct);
        return isTargetTeamMember && note.SharedWithTeamIds.Contains(teamId) &&
            note.AllowedUpdateTeamIds.Contains(teamId);
    }

    private static bool CanDelete(NoteResponse note, string teamId, string userId) =>
        note.TeamId == teamId && note.OwnerUserId == userId;

    private static bool CanShare(NoteResponse note, string teamId, string ownerUserId) =>
        note.TeamId == teamId && note.OwnerUserId == ownerUserId;
}
