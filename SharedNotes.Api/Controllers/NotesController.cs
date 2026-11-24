using Microsoft.AspNetCore.Mvc;
using SharedNotes.Api.Identity;
using SharedNotes.Api.Models;
using SharedNotes.Api.Services;

namespace SharedNotes.Api.Controllers;

[ApiController]
[Route("api/v1/teams/{teamId}/notes")]
public sealed class NotesController : ControllerBase
{
    private readonly INoteService _service;
    private readonly ICurrentUser _currentUser;

    public NotesController(INoteService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<NoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<NoteResponse>>> GetAll(
        string teamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_currentUser.TryGetUserId(out var userId)) return MissingIdentity();
        var notes = await _service.GetVisibleAsync(teamId, userId, ct);
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(notes.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToArray());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponse>> Get(string teamId, Guid id, CancellationToken ct)
    {
        if (!_currentUser.TryGetUserId(out var userId)) return MissingIdentity();
        var note = await _service.GetAsync(id, teamId, userId, ct);
        if (note is null) return NotFound(Problem("Note not found", StatusCodes.Status404NotFound));
        Response.Headers.ETag = $"\"{note.Version}\"";
        return Ok(note);
    }

    [HttpPost]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NoteResponse>> Create(string teamId, [FromBody] CreateNoteRequest request, CancellationToken ct)
    {
        if (!_currentUser.TryGetUserId(out var userId)) return MissingIdentity();
        try
        {
            var note = await _service.CreateAsync(teamId, userId, request, ct);
            return CreatedAtAction(nameof(Get), new { teamId, id = note.Id }, note);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(Problem("Invalid team membership", StatusCodes.Status400BadRequest, exception.Message));
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NoteResponse>> Update(string teamId, Guid id, [FromBody] UpdateNoteRequest request, CancellationToken ct)
    {
        if (!_currentUser.TryGetUserId(out var userId)) return MissingIdentity();
        var note = await _service.UpdateAsync(id, teamId, userId, request, ct);
        if (note is null) return NotFound(Problem("Note not found or update not allowed", StatusCodes.Status404NotFound));
        Response.Headers.ETag = $"\"{note.Version}\"";
        return Ok(note);
    }

    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponse>> Share(string teamId, Guid id, [FromBody] ShareNoteRequest request, CancellationToken ct)
    {
        if (!_currentUser.TryGetUserId(out var userId)) return MissingIdentity();
        var note = await _service.ShareAsync(id, teamId, userId, request, ct);
        return note is null ? NotFound(Problem("Note or share target not found", StatusCodes.Status404NotFound)) : Ok(note);
    }

    [HttpDelete("{id:guid}/share/user/{sharedUserId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUserShare(string teamId, Guid id, string sharedUserId, CancellationToken ct)
    {
        if (!_currentUser.TryGetUserId(out var userId)) return MissingIdentity();
        if (!await _service.RemoveUserShareAsync(id, teamId, userId, sharedUserId, ct))
            return NotFound(Problem("Note or direct share not found", StatusCodes.Status404NotFound));
        return NoContent();
    }

    [HttpDelete("{id:guid}/share/team/{sharedTeamId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTeamShare(string teamId, Guid id, string sharedTeamId, CancellationToken ct)
    {
        if (!_currentUser.TryGetUserId(out var userId)) return MissingIdentity();
        if (!await _service.RemoveTeamShareAsync(id, teamId, userId, sharedTeamId, ct))
            return NotFound(Problem("Note or team share not found", StatusCodes.Status404NotFound));
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string teamId, Guid id, CancellationToken ct)
    {
        if (!_currentUser.TryGetUserId(out var userId)) return MissingIdentity();
        if (!await _service.DeleteAsync(id, teamId, userId, ct))
            return NotFound(Problem("Note not found or delete not allowed", StatusCodes.Status404NotFound));
        return NoContent();
    }

    private UnauthorizedObjectResult MissingIdentity() => Unauthorized(Problem("Authentication required", StatusCodes.Status401Unauthorized,
        "X-User-Id header is required."));

    private static ProblemDetails Problem(string title, int status, string? detail = null) =>
        new() { Title = title, Status = status, Detail = detail };
}
