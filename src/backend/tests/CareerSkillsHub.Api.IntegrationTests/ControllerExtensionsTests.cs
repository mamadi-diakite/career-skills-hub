namespace CareerSkillsHub.Api.IntegrationTests;

public sealed class ControllerExtensionsTests
{
    private readonly TestController _controller = new()
    {
        ProblemDetailsFactory = new TestProblemDetailsFactory(),
    };

    [Fact]
    public void Ok_returns_the_value_or_the_custom_response()
    {
        Assert.Equal(7, Assert.IsType<OkObjectResult>(_controller.ToActionResult(ServiceResult<int>.Ok(7))).Value);
        Assert.IsType<NoContentResult>(_controller.ToActionResult(ServiceResult<int>.Ok(7), _ => new NoContentResult()));
    }

    [Fact]
    public void Failures_map_to_their_status_code()
    {
        Assert.IsType<UnauthorizedResult>(_controller.ToActionResult(ServiceResult<int>.Unauthorized()));
        Assert.Equal(403, Status(_controller.ToActionResult(ServiceResult<int>.Forbidden("No"))));
        Assert.Equal(404, Status(_controller.ToActionResult(ServiceResult<int>.NotFound("Gone"))));
        Assert.Equal(409, Status(_controller.ToActionResult(ServiceResult<int>.Conflict("Used"))));
    }

    private static int? Status(ActionResult result) => Assert.IsType<ObjectResult>(result, exactMatch: false).StatusCode;

    private sealed class TestController : ControllerBase;

    private sealed class TestProblemDetailsFactory : Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            Microsoft.AspNetCore.Http.HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) => new() { Status = statusCode, Title = title };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            Microsoft.AspNetCore.Http.HttpContext httpContext,
            Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) => new(modelStateDictionary) { Status = statusCode ?? 400 };
    }
}
