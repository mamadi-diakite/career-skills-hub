namespace CareerSkillsHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/company")]
public sealed class CompanyController(CompanyService companyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CompanyResponse>> Get(CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? this.ToActionResult(await companyService.GetAsync(userId, cancellationToken))
            : Unauthorized();

    [HttpPut]
    public async Task<ActionResult<CompanyResponse>> Update(UpdateCompanyRequest request, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? this.ToActionResult(await companyService.UpdateAsync(userId, request, cancellationToken))
            : Unauthorized();
}
