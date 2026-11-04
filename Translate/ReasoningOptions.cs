namespace Translate;

public static class ReasoningOptions
{
    private static readonly string[] All = ["none", "minimal", "low", "medium", "high", "xhigh", "max"];
    public static IReadOnlyList<ReasoningChoice> ForModel(string providerId, string model, IReadOnlyList<string>? advertised = null)
    {
        var name = model.ToLowerInvariant();
        if (name.StartsWith("ft:")) name = name[3..].Split(':')[0];
        string[] levels;
        if (string.IsNullOrWhiteSpace(model)) levels = [];
        else if (providerId == "deepseek")
            levels = advertised is { Count: > 0 } ? new[] { "none" }.Concat(advertised.Where(All.Contains)).Distinct().ToArray() : ["none", "low", "high", "max"];
        else if (advertised is { Count: > 0 }) levels = advertised.Where(All.Contains).Distinct().ToArray();
        else if (name.StartsWith("gpt-4") || name.StartsWith("gpt-3") || name.StartsWith("chatgpt-") || name.Contains("-chat-latest") || name.StartsWith("o1-preview") || name.StartsWith("o1-mini")) levels = [];
        else if (name.StartsWith("gpt-6-astra") || name.StartsWith("gpt-6.1-sol")) levels = ["low", "medium", "high", "xhigh", "max"];
        else if (name.StartsWith("gpt-6-sol") || name.StartsWith("gpt-6-luna")) levels = ["none", "low", "medium", "high", "xhigh", "max"];
        else if (name == "gpt-5.6" || name.StartsWith("gpt-5.6-sol")) levels = ["none", "low", "medium", "high", "xhigh", "max"];
        else if (name.StartsWith("gpt-5-pro")) levels = ["high"];
        else if (name.StartsWith("gpt-5.1")) levels = ["none", "low", "medium", "high"];
        else if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^gpt-5\.[2-6](?:-|$)"))
            levels = name.Contains("-pro") ? ["medium", "high", "xhigh"] : ["none", "low", "medium", "high", "xhigh"];
        else if (System.Text.RegularExpressions.Regex.IsMatch(name, "^gpt-5(?:-|$)")) levels = ["minimal", "low", "medium", "high"];
        else if (System.Text.RegularExpressions.Regex.IsMatch(name, "^o[134](?:-|$)")) levels = ["low", "medium", "high"];
        else levels = providerId == "custom" ? All : [];
        return new[] { new ReasoningChoice("default", "模型默认") }.Concat(levels.Select(value => new ReasoningChoice(value, value switch
        {
            "none" => "关闭推理 · none", "minimal" => "极低 · minimal", "low" => "低 · low", "medium" => "中 · medium",
            "high" => "高 · high", "xhigh" => "很高 · xhigh", "max" => "最高 · max", _ => value
        }))).ToArray();
    }

    public static void Apply(Dictionary<string, object> payload, AppSettings settings)
    {
        var effort = settings.ReasoningEffort;
        if (string.IsNullOrWhiteSpace(effort) || effort == "default") return;
        if (!All.Contains(effort)) throw new InvalidOperationException("推理强度设置无效，请重新选择。" );
        if (settings.TranslationProviderId == "deepseek" || Uri.TryCreate(settings.TranslationBaseUrl, UriKind.Absolute, out var uri) && uri.Host == "api.deepseek.com")
        {
            payload["thinking"] = new { type = effort == "none" ? "disabled" : "enabled" };
            if (effort != "none") payload["reasoning_effort"] = effort;
        }
        else payload["reasoning_effort"] = effort;
    }
}
