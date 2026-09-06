using Microsoft.Web.WebView2.Core;
using Newton.Core.Security;

namespace NorthstarBrowser.Windows.WebView2;

public sealed record PermissionPrompt(Uri Origin, BrowserPermission Permission);

public sealed class WebView2PermissionAdapter(IPermissionPolicy policy)
{
    public void Attach(CoreWebView2 core, bool isPrivateProfile, Func<PermissionPrompt, bool> askUser)
    {
        core.PermissionRequested += (_, e) =>
        {
            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var origin))
            {
                e.State = CoreWebView2PermissionState.Deny;
                e.SavesInProfile = false;
                return;
            }

            var permission = Map(e.PermissionKind);
            var decision = policy.Evaluate(new PermissionRequest(origin, permission, isPrivateProfile, e.IsUserInitiated));
            e.State = decision switch
            {
                PermissionPolicyDecision.Allow => CoreWebView2PermissionState.Allow,
                PermissionPolicyDecision.Ask when askUser(new PermissionPrompt(origin, permission)) => CoreWebView2PermissionState.Allow,
                _ => CoreWebView2PermissionState.Deny
            };
            e.SavesInProfile = false;
        };
    }

    private static BrowserPermission Map(CoreWebView2PermissionKind kind) => kind switch
    {
        CoreWebView2PermissionKind.Camera => BrowserPermission.Camera,
        CoreWebView2PermissionKind.Microphone => BrowserPermission.Microphone,
        CoreWebView2PermissionKind.Geolocation => BrowserPermission.Geolocation,
        CoreWebView2PermissionKind.Notifications => BrowserPermission.Notifications,
        CoreWebView2PermissionKind.ClipboardRead => BrowserPermission.Clipboard,
        _ => BrowserPermission.Other
    };
}
