namespace CareerSkillsHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/company")]
public sealed class CompanyController(CompanyService companyService) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult> Get(CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => companyService.GetAsync(userId, cancellationToken));

    [HttpPut]
    public Task<ActionResult> Update(UpdateCompanyRequest request, CancellationToken cancellationToken) =>
        this.ForUserAsync(userId => companyService.UpdateAsync(userId, request, cancellationToken));
}
