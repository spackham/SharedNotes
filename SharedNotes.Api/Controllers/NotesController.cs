using Microsoft.AspNetCore.Mvc;
using SharedNotes.Api.Models;
using SharedNotes.Api.Services;

namespace SharedNotes.Api.Controllers;

[ApiController]
[Route("api/v1/teams/{teamId}/notes")]
public sealed class NotesController : ControllerBase
{
    private readonly INoteService _service;

    public NotesController(INoteService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<NoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<NoteResponse>>> GetAll(string teamId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        return Ok(await _service.GetVisibleAsync(teamId, userId, ct));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponse>> Get(string teamId, Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        var note = await _service.GetAsync(id, teamId, userId, ct);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpPost]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NoteResponse>> Create(string teamId, [FromBody] CreateNoteRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        try
        {
            var note = await _service.CreateAsync(teamId, userId, request, ct);
            return CreatedAtAction(nameof(Get), new { teamId, id = note.Id }, note);
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new { message = "The user is not a member of the team." });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponse>> Update(string teamId, Guid id, [FromBody] UpdateNoteRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        var note = await _service.UpdateAsync(id, teamId, userId, request, ct);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponse>> Share(string teamId, Guid id, [FromBody] ShareNoteRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        var note = await _service.ShareAsync(id, teamId, userId, request, ct);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string teamId, Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        if (!await _service.DeleteAsync(id, teamId, userId, ct)) return NotFound();
        return NoContent();
    }

    private bool TryGetUserId(out string userId)
    {
        userId = Request.Headers["X-User-Id"].ToString().Trim();
        return userId.Length > 0;
    }
}
