using System.ComponentModel.DataAnnotations;

namespace SharedNotes.Api.Models;

/// <summary>Payload for creating a temporary team.</summary>
public sealed record CreateTeamRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }
}

/// <summary>Payload for adding a user to a team.</summary>
public sealed record AddTeamMemberRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string UserId { get; init; }
}

/// <summary>A temporary team returned by the API.</summary>
public sealed record TeamResponse(
    string Id,
    string Name,
    IReadOnlyCollection<string> MemberUserIds,
    DateTimeOffset CreatedAt);
