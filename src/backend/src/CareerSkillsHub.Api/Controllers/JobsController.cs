namespace CareerSkillsHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/jobs")]
public sealed class JobsController(JobService jobs) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult> List(CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => jobs.ListAsync(userId, cancellationToken));

    [HttpGet("{id:guid}")]
    public Task<ActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => jobs.GetAsync(userId, id, cancellationToken));

    [HttpPost]
    public Task<ActionResult> Create(JobRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(
            userId => jobs.CreateAsync(userId, request, cancellationToken),
            job => Created($"/api/jobs/{job.Id}", job));

    [HttpPut("{id:guid}")]
    public Task<ActionResult> Update(Guid id, JobRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => jobs.UpdateAsync(userId, id, request, cancellationToken));

    [HttpPut("{id:guid}/required-skills")]
    public Task<ActionResult> SetRequiredSkills(Guid id, RequiredSkillsRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => jobs.SetRequiredSkillsAsync(userId, id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => jobs.DeleteAsync(userId, id, cancellationToken), _ => NoContent());
}
