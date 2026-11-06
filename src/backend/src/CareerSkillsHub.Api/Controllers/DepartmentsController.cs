namespace CareerSkillsHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/departments")]
public sealed class DepartmentsController(DepartmentService departments) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult> List(CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => departments.ListAsync(userId, cancellationToken));

    [HttpPost]
    public Task<ActionResult> Create(DepartmentRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(
            userId => departments.CreateAsync(userId, request, cancellationToken),
            department => Created($"/api/departments/{department.Id}", department));

    [HttpPut("{id:guid}")]
    public Task<ActionResult> Update(Guid id, DepartmentRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => departments.UpdateAsync(userId, id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => departments.DeleteAsync(userId, id, cancellationToken), _ => NoContent());
}
