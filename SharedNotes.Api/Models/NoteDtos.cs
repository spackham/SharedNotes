using System.ComponentModel.DataAnnotations;

namespace SharedNotes.Api.Models;

/// <summary>Payload for creating a shared note.</summary>
public sealed record CreateNoteRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Title { get; init; }

    [Required, StringLength(20_000, MinimumLength = 1)]
    public required string Content { get; init; }
}

/// <summary>Payload for replacing a note's editable fields.</summary>
public sealed record UpdateNoteRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Title { get; init; }

    [Required, StringLength(20_000, MinimumLength = 1)]
    public required string Content { get; init; }
}

/// <summary>Payload for granting another user access to a note.</summary>
public sealed record ShareNoteRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string UserId { get; init; }
}

/// <summary>A note returned by the API.</summary>
public sealed record NoteResponse(
    Guid Id,
    string OwnerId,
    string Title,
    string Content,
    IReadOnlyCollection<string> SharedWith,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
