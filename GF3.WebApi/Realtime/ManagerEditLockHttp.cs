using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Realtime;

public static class ManagerEditLockHttp
{
    public static ActionResult? CreateConflictResult(
        ControllerBase controller,
        IManagerEditLockService? editLockService,
        ManagerEditLockTarget target,
        string resourceLabel)
    {
        if (editLockService is null)
        {
            return null;
        }

        var state = editLockService.GetLockState(target);
        if (state?.IsLocked != true || IsCurrentManagerOwner(controller.User, state))
        {
            return null;
        }

        var lockedBy = string.IsNullOrWhiteSpace(state.LockedBy) ? "another manager" : state.LockedBy;
        return controller.Conflict(new ProblemDetails
        {
            Type = "edit_lock_conflict",
            Title = "Edit Locked",
            Status = StatusCodes.Status409Conflict,
            Detail = $"{resourceLabel} is currently being edited by {lockedBy}.",
            Instance = controller.HttpContext.Request.Path,
        });
    }

    private static bool IsCurrentManagerOwner(ClaimsPrincipal user, ManagerEditLockState state)
    {
        if (!state.LockedByManagerId.HasValue)
        {
            return false;
        }

        return int.TryParse(user.FindFirstValue("manager_id"), out var managerId) &&
               managerId == state.LockedByManagerId.Value;
    }
}
