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
        if (!await _unitOfWork.Teams.IsMemberAsync(teamId, userId, ct)) return Array.Empty<NoteResponse>();
        return await _unitOfWork.Notes.GetVisibleToAsync(teamId, userId, ct);
    }

    public async Task<NoteResponse?> GetAsync(Guid id, string teamId, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await _unitOfWork.Notes.GetByIdAsync(id, ct);
        if (note is null) return null;

        var isOwnerTeam = note.TeamId == teamId;
        var isMember = await _unitOfWork.Teams.IsMemberAsync(teamId, userId, ct);
        var canDirectRead = note.SharedWithUserIds.Contains(userId);
        var canTeamRead = isMember && note.SharedWithTeamIds.Contains(teamId);

        if (isOwnerTeam) return isMember || canDirectRead ? note : null;
        return canDirectRead || canTeamRead ? note : null;
    }

    public async Task<NoteResponse> CreateAsync(string teamId, string ownerUserId, CreateNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!await _unitOfWork.Teams.IsMemberAsync(teamId, ownerUserId, ct))
            throw new InvalidOperationException("The user is not a member of the team.");

        var created = await _unitOfWork.Notes.CreateAsync(teamId, ownerUserId, request, ct);
        await _unitOfWork.CommitAsync(ct);
        return created;
    }

    public async Task<NoteResponse?> UpdateAsync(Guid id, string teamId, string userId, UpdateNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await _unitOfWork.Notes.GetByIdAsync(id, ct);
        if (note is null) return null;

        var isOwner = note.OwnerUserId == userId;
        var isOwnerTeam = note.TeamId == teamId;
        var isTeamMember = await _unitOfWork.Teams.IsMemberAsync(teamId, userId, ct);
        var canDirectUpdate = note.AllowedUpdateUserIds.Contains(userId);
        var canTeamUpdate = isTeamMember && note.AllowedUpdateTeamIds.Contains(teamId);

        if (!isOwner && !canDirectUpdate && !(isOwnerTeam && canTeamUpdate)) return null;
        if (!await _unitOfWork.Notes.UpdateAsync(id, request, ct)) return null;

        await _unitOfWork.CommitAsync(ct);
        return await _unitOfWork.Notes.GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, string teamId, string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await _unitOfWork.Notes.GetByIdAsync(id, ct);
        if (note is null || note.TeamId != teamId || note.OwnerUserId != userId) return false;

        var deleted = await _unitOfWork.Notes.DeleteAsync(id, ct);
        if (deleted) await _unitOfWork.CommitAsync(ct);
        return deleted;
    }

    public async Task<NoteResponse?> ShareAsync(Guid id, string teamId, string ownerUserId, ShareNoteRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var note = await _unitOfWork.Notes.GetByIdAsync(id, ct);
        if (note is null || note.TeamId != teamId || note.OwnerUserId != ownerUserId) return null;

        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            var userId = request.UserId.Trim();
            if (userId == ownerUserId) return null;
            if (!await _unitOfWork.Notes.ShareWithUserAsync(id, userId, request.AllowUpdates, ct)) return null;
        }

        if (!string.IsNullOrWhiteSpace(request.TeamId))
        {
            var sharedTeamId = request.TeamId.Trim();
            if (sharedTeamId == teamId) return null;
            if (await _unitOfWork.Teams.GetByIdAsync(sharedTeamId, ct) is null) return null;
            if (!await _unitOfWork.Notes.ShareWithTeamAsync(id, sharedTeamId, request.AllowUpdates, ct)) return null;
        }

        if (string.IsNullOrWhiteSpace(request.UserId) && string.IsNullOrWhiteSpace(request.TeamId)) return null;

        await _unitOfWork.CommitAsync(ct);
        return await _unitOfWork.Notes.GetByIdAsync(id, ct);
    }
}
