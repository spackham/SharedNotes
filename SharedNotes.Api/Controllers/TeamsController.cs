using Microsoft.AspNetCore.Mvc;
using SharedNotes.Api.Models;
using SharedNotes.Api.Repositories;

namespace SharedNotes.Api.Controllers;

[ApiController]
[Route("api/v1/teams")]
public sealed class TeamsController : ControllerBase
{
    private readonly ITeamRepository _teams;

    public TeamsController(ITeamRepository teams) => _teams = teams;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<TeamResponse>>> GetAll(CancellationToken ct) =>
        Ok(await _teams.GetAllAsync(ct));

    [HttpGet("{teamId}")]
    public async Task<ActionResult<TeamResponse>> Get(string teamId, CancellationToken ct)
    {
        var team = await _teams.GetByIdAsync(teamId, ct);
        return team is null ? NotFound() : Ok(team);
    }

    [HttpPost]
    public async Task<ActionResult<TeamResponse>> Create([FromBody] CreateTeamRequest request, CancellationToken ct)
    {
        var team = await _teams.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { teamId = team.Id }, team);
    }

    [HttpPost("{teamId}/members")]
    public async Task<ActionResult<TeamResponse>> AddMember(string teamId, [FromBody] AddTeamMemberRequest request, CancellationToken ct)
    {
        var team = await _teams.AddMemberAsync(teamId, request.UserId.Trim(), ct);
        return team is null ? NotFound() : Ok(team);
    }
}
