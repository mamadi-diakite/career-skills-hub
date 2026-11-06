namespace CareerSkillsHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public sealed class EmployeesController(EmployeeService employees) : ControllerBase
{
    /// <summary>Only the employees the caller may see: everyone for HR, the team for a manager, oneself otherwise.</summary>
    [HttpGet]
    public Task<ActionResult> List(CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => employees.ListAsync(userId, cancellationToken));

    [HttpGet("{id:guid}")]
    public Task<ActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => employees.GetAsync(userId, id, cancellationToken));

    [HttpPost]
    public Task<ActionResult> Create(CreateEmployeeRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(
            userId => employees.CreateAsync(userId, request, cancellationToken),
            details => Created($"/api/employees/{details.Employee.Id}", details));

    [HttpPost("{id:guid}/job-changes")]
    public Task<ActionResult> ChangeJob(Guid id, JobChangeRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => employees.ChangeJobAsync(userId, id, request, cancellationToken));

    /// <summary>Creates the account of the employee; the temporary password is only returned here.</summary>
    [HttpPost("{id:guid}/invitation")]
    public Task<ActionResult> Invite(Guid id, InvitationRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => employees.InviteAsync(userId, id, request, cancellationToken));
}
