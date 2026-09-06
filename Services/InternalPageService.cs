using System.Net;
using System.Reflection;
using Microsoft.Web.WebView2.Core;
using Newton.Core.Privacy;

namespace NorthstarBrowser.Services;

public static class InternalPageService
{
    public static bool TryRender(Uri uri, PrivacyLevel privacyLevel, PrivacyRulesetMetadata ruleset, out string html)
    {
        var name = uri.Host.ToLowerInvariant();
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        var runtime = CoreWebView2Environment.GetAvailableBrowserVersionString();
        var body = name switch
        {
            "version" => Rows(("Newton", assemblyVersion), ("Channel", "Alpha"), ("WebView2 runtime", runtime), ("OS", Environment.OSVersion.ToString()), ("Architecture", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString())),
            "policy" => Rows(("Privacy level", privacyLevel.ToString()), ("Ruleset", ruleset.Version), ("Ruleset published", ruleset.PublishedOn.ToString("yyyy-MM-dd")), ("Ruleset SHA-256", ruleset.IntegritySha256), ("Telemetry", "Off"), ("Remote suggestions", "Off"), ("AI", "Disabled"), ("Extensions", "Disabled")),
            "diagnostics" => Rows(("Database schema", NewtonDataStore.CurrentSchemaVersion.ToString()), ("Runtime", runtime), ("Profile data", "Isolated WebView2 profile"), ("Crash recovery", "15-second durable snapshot")),
            "performance" => Rows(("Status", "No verified performance claim"), ("Tab lifecycle", "Active / Background / Sleeping / Discarded / Crashed")),
            "crashes" => Rows(("Recovery", "Abnormal shutdown restores validated recovery records"), ("Safe Mode", "Planned - not yet implemented")),
            _ => string.Empty
        };
        if (body.Length == 0) { html = string.Empty; return false; }
        html = $"<!doctype html><meta charset=\"utf-8\"><title>Newton {WebUtility.HtmlEncode(name)}</title>" +
            "<style>body{font:15px system-ui;background:#f8fafc;color:#0f172a;margin:48px;max-width:900px}h1{font-size:30px}table{width:100%;border-collapse:collapse;background:white}th,td{padding:12px;border-bottom:1px solid #e2e8f0;text-align:left}th{width:220px;color:#475569}code{overflow-wrap:anywhere}</style>" +
            $"<h1>Newton / {WebUtility.HtmlEncode(name)}</h1><table>{body}</table>";
        return true;
    }

    private static string Rows(params (string Name, string Value)[] rows) => string.Join("", rows.Select(row =>
        $"<tr><th>{WebUtility.HtmlEncode(row.Name)}</th><td><code>{WebUtility.HtmlEncode(row.Value)}</code></td></tr>"));
}
