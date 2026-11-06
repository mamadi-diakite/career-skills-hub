namespace CareerSkillsHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("sign-up")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> SignUp(SignUpRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.SignUpAsync(request, cancellationToken);

        return result.Response is { } response
            ? CreatedAtAction(nameof(Me), response)
            : this.InvalidRequest(result.Errors);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);

        return response is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Email or password is not correct.")
            : Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var user = User.GetUserId() is { } userId
            ? await authService.GetCurrentUserAsync(userId, cancellationToken)
            : null;

        return user is null ? Unauthorized() : Ok(user);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await authService.ChangePasswordAsync(userId, request, cancellationToken);

        return result.Status switch
        {
            ChangePasswordStatus.Changed => NoContent(),
            ChangePasswordStatus.UserNotFound => Unauthorized(),
            _ => this.InvalidRequest(result.Errors),
        };
    }
}
