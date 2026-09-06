namespace Newton.Core.Security;

public interface IPermissionPolicy
{
    PermissionPolicyDecision Evaluate(PermissionRequest request);
}

public sealed record PermissionRequest(
    Uri Origin,
    BrowserPermission Permission,
    bool IsPrivateProfile,
    bool HasUserGesture);

public enum BrowserPermission { Camera, Microphone, Geolocation, Notifications, Clipboard, Other }
public enum PermissionPolicyDecision { Allow, Block, Ask }

public sealed class DefaultPermissionPolicy : IPermissionPolicy
{
    public PermissionPolicyDecision Evaluate(PermissionRequest request)
    {
        if (request.Origin.Scheme is not ("https" or "http")) return PermissionPolicyDecision.Block;
        if (request.Permission is BrowserPermission.Other) return PermissionPolicyDecision.Block;
        if (!request.HasUserGesture && request.Permission is BrowserPermission.Clipboard)
            return PermissionPolicyDecision.Block;
        return PermissionPolicyDecision.Ask;
    }
}
