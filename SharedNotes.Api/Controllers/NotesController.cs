using Microsoft.AspNetCore.Mvc;
using SharedNotes.Api.Models;
using SharedNotes.Api.Services;

namespace SharedNotes.Api.Controllers;

[ApiController]
[Route("api/v1/notes")]
public sealed class NotesController : ControllerBase
{
    private readonly INoteService _service;

    public NotesController(INoteService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<NoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<NoteResponse>>> GetAll(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        return Ok(await _service.GetVisibleAsync(userId, ct));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponse>> Get(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        var note = await _service.GetAsync(id, userId, ct);
        if (note is null) return NotFound();
        return Ok(note);
    }

    [HttpPost]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NoteResponse>> Create([FromBody] CreateNoteRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        var note = await _service.CreateAsync(userId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = note.Id }, note);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponse>> Update(Guid id, [FromBody] UpdateNoteRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        var note = await _service.UpdateAsync(id, userId, request, ct);
        if (note is null) return NotFound();
        return Ok(note);
    }

    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponse>> Share(Guid id, [FromBody] ShareNoteRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        var note = await _service.ShareAsync(id, userId, request, ct);
        if (note is null) return NotFound();
        return Ok(note);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return BadRequest(new { message = "X-User-Id header is required." });
        if (!await _service.DeleteAsync(id, userId, ct)) return NotFound();
        return NoContent();
    }

    private bool TryGetUserId(out string userId)
    {
        userId = Request.Headers["X-User-Id"].ToString().Trim();
        return userId.Length > 0;
    }
}
