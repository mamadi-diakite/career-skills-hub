namespace CareerSkillsHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class SkillFrameworkController(SkillFrameworkService framework) : ControllerBase
{
    /// <summary>All skill domains with their skills.</summary>
    [HttpGet("skill-domains")]
    public Task<ActionResult> List(CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => framework.ListAsync(userId, cancellationToken));

    [HttpPost("skill-domains")]
    public Task<ActionResult> CreateDomain(SkillDomainRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(
            userId => framework.CreateDomainAsync(userId, request, cancellationToken),
            domain => Created($"/api/skill-domains/{domain.Id}", domain));

    [HttpPut("skill-domains/{id:guid}")]
    public Task<ActionResult> UpdateDomain(Guid id, SkillDomainRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => framework.UpdateDomainAsync(userId, id, request, cancellationToken));

    [HttpDelete("skill-domains/{id:guid}")]
    public Task<ActionResult> DeleteDomain(Guid id, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => framework.DeleteDomainAsync(userId, id, cancellationToken), _ => NoContent());

    [HttpPost("skills")]
    public Task<ActionResult> CreateSkill(SkillRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(
            userId => framework.CreateSkillAsync(userId, request, cancellationToken),
            skill => Created($"/api/skills/{skill.Id}", skill));

    [HttpPut("skills/{id:guid}")]
    public Task<ActionResult> UpdateSkill(Guid id, SkillRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => framework.UpdateSkillAsync(userId, id, request, cancellationToken));

    [HttpDelete("skills/{id:guid}")]
    public Task<ActionResult> DeleteSkill(Guid id, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => framework.DeleteSkillAsync(userId, id, cancellationToken), _ => NoContent());
}
