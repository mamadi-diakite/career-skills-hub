namespace CareerSkillsHub.Api.Controllers;

internal static class ControllerExtensions
{
    /// <summary>Returns a validation problem with the messages of each request field.</summary>
    public static ActionResult InvalidRequest(this ControllerBase controller, IReadOnlyDictionary<string, string[]> errors)
    {
        foreach (var (field, messages) in errors)
        {
            foreach (var message in messages)
            {
                controller.ModelState.AddModelError(field, message);
            }
        }

        return controller.ValidationProblem();
    }

    /// <param name="onOk">Builds the success response; plain 200 with the value when omitted.</param>
    public static ActionResult ToActionResult<T>(
        this ControllerBase controller,
        ServiceResult<T> result,
        Func<T, ActionResult>? onOk = null) => result.Status switch
    {
        ServiceStatus.Ok => onOk is null ? controller.Ok(result.Value) : onOk(result.Value!),
        ServiceStatus.Unauthorized => controller.Unauthorized(),
        ServiceStatus.Invalid => controller.InvalidRequest(result.Errors),
        ServiceStatus.Forbidden => controller.Problem(statusCode: StatusCodes.Status403Forbidden, title: result.Message),
        ServiceStatus.NotFound => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: result.Message),
        _ => controller.Problem(statusCode: StatusCodes.Status409Conflict, title: result.Message),
    };
}
