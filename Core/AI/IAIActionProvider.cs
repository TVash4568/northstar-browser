namespace Newton.Core.AI;

public interface IAIActionProvider
{
    ValueTask<AIActionResult> ExecuteAsync(
        BrowserAction action,
        ActionPermissionGrant grant,
        CancellationToken cancellationToken = default);
}

public sealed record BrowserAction(
    AIActionKind Kind,
    Uri? TargetOrigin,
    string? Payload = null);

public sealed record ActionPermissionGrant(
    Guid GrantId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    Guid UserGestureId,
    IReadOnlySet<string> PermittedOrigins,
    IReadOnlySet<AIActionKind> PermittedActions,
    string ContextScopeId,
    int MaxOperations,
    IReadOnlySet<AIActionKind> RequiresConfirmationFor);

public sealed record AIActionResult(bool Succeeded, string? Detail = null);

public enum AIActionKind
{
    Read,
    Navigate,
    ManageTabs,
    TypeIntoForm,
    Clipboard,
    Download,
    Upload,
    Submit,
    Purchase,
    Authenticate
}

public static class ActionPermissionValidator
{
    public static void Validate(BrowserAction action, ActionPermissionGrant grant, DateTimeOffset nowUtc, int completedOperations, bool userConfirmed)
    {
        if (nowUtc < grant.IssuedAtUtc || nowUtc >= grant.ExpiresAtUtc)
            throw new UnauthorizedAccessException("The AI action grant is not currently valid.");
        if (completedOperations < 0 || completedOperations >= grant.MaxOperations)
            throw new UnauthorizedAccessException("The AI action grant operation limit has been reached.");
        if (!grant.PermittedActions.Contains(action.Kind))
            throw new UnauthorizedAccessException("The requested AI action is outside the grant.");
        if (action.TargetOrigin is not null && !grant.PermittedOrigins.Contains(action.TargetOrigin.GetLeftPart(UriPartial.Authority)))
            throw new UnauthorizedAccessException("The target origin is outside the AI action grant.");
        if (grant.RequiresConfirmationFor.Contains(action.Kind) && !userConfirmed)
            throw new UnauthorizedAccessException("This AI action requires fresh user confirmation.");
        if (action.Kind is AIActionKind.Purchase or AIActionKind.Authenticate)
            throw new UnauthorizedAccessException("Purchases and authentication are prohibited for Newton AI.");
    }
}
