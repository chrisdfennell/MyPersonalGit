using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MyPersonalGit.Data;

public interface IAiChatService
{
    Task<string?> ChatAsync(string userMessage, string? systemPrompt = null, string? codeContext = null);
    IAsyncEnumerable<string> ChatStreamAsync(string userMessage, string? systemPrompt = null, string? codeContext = null, CancellationToken ct = default);
    Task<string?> ExplainCodeAsync(string code, string language, string? fileName = null);
    Task<string?> RefactorCodeAsync(string code, string language, string instruction, string? fileName = null);
    Task<string?> GenerateTestsAsync(string code, string language, string? fileName = null, string? testFramework = null);
    Task<string?> FixCodeAsync(string code, string language, string diagnostics);
    Task<string?> GenerateDocstringAsync(string code, string language);
}

public class AiChatService : IAiChatService
{
    private readonly IAdminService _adminService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiChatService(IAdminService adminService, IHttpClientFactory httpClientFactory)
    {
        _adminService = adminService;
        _httpClientFactory = httpClientFactory;
    }

    private async Task<(HttpClient client, string model, string endpoint)?> GetConfiguredClientAsync()
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        if (!settings.AiCompletionEnabled
            || string.IsNullOrEmpty(settings.AiCompletionEndpoint)
            || string.IsNullOrEmpty(settings.AiCompletionApiKey))
            return null;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.AiCompletionApiKey);
        client.Timeout = TimeSpan.FromSeconds(120);

        var endpoint = settings.AiCompletionEndpoint.TrimEnd('/');
        return (client, settings.AiCompletionModel, endpoint);
    }

    public async Task<string?> ChatAsync(string userMessage, string? systemPrompt = null, string? codeContext = null)
    {
        var config = await GetConfiguredClientAsync();
        if (config == null) return null;

        var (client, model, endpoint) = config.Value;

        var messages = new List<object>();
        messages.Add(new { role = "system", content = systemPrompt ?? "You are an expert programming assistant integrated into a code editor. Be concise and helpful. Format code in markdown fenced blocks with the language specified." });

        if (!string.IsNullOrEmpty(codeContext))
            messages.Add(new { role = "user", content = $"Context code:\n```\n{TrimContext(codeContext, 6000)}\n```" });

        messages.Add(new { role = "user", content = userMessage });

        try
        {
            var requestBody = new
            {
                model,
                messages,
                max_tokens = 2048,
                temperature = 0.3
            };

            var response = await client.PostAsync(
                $"{endpoint}/chat/completions",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0) return null;

            return choices[0].GetProperty("message").GetProperty("content").GetString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AI Chat] Error: {ex.Message}");
            return null;
        }
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(string userMessage, string? systemPrompt = null, string? codeContext = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var config = await GetConfiguredClientAsync();
        if (config == null) yield break;

        var (client, model, endpoint) = config.Value;

        var messages = new List<object>();
        messages.Add(new { role = "system", content = systemPrompt ?? "You are an expert programming assistant integrated into a code editor. Be concise and helpful. Format code in markdown fenced blocks with the language specified." });

        if (!string.IsNullOrEmpty(codeContext))
            messages.Add(new { role = "user", content = $"Context code:\n```\n{TrimContext(codeContext, 6000)}\n```" });

        messages.Add(new { role = "user", content = userMessage });

        var requestBody = new
        {
            model,
            messages,
            max_tokens = 2048,
            temperature = 0.3,
            stream = true
        };

        HttpResponseMessage? response = null;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode) yield break;

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;
                if (!line.StartsWith("data: ")) continue;

                var data = line["data: ".Length..];
                if (data == "[DONE]") break;

                using var doc = JsonDocument.Parse(data);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0) continue;

                var delta = choices[0].GetProperty("delta");
                if (delta.TryGetProperty("content", out var content))
                {
                    var text = content.GetString();
                    if (!string.IsNullOrEmpty(text))
                        yield return text;
                }
            }
        }
        finally
        {
            response?.Dispose();
        }
    }

    public async Task<string?> ExplainCodeAsync(string code, string language, string? fileName = null)
    {
        var systemPrompt = "You are a code explanation expert. Explain the given code clearly and concisely. " +
                           "Break down complex logic, explain the purpose of each section, and highlight any notable patterns or potential issues. " +
                           "Use markdown formatting.";

        var prompt = $"Explain this {language} code" + (fileName != null ? $" from `{fileName}`" : "") + $":\n\n```{language}\n{TrimContext(code, 4000)}\n```";

        return await ChatAsync(prompt, systemPrompt);
    }

    public async Task<string?> RefactorCodeAsync(string code, string language, string instruction, string? fileName = null)
    {
        var systemPrompt = "You are a code refactoring expert. When asked to refactor code, return ONLY the refactored code inside a single markdown fenced code block. " +
                           "Add a brief explanation after the code block. Do not include the original code.";

        var prompt = $"Refactor this {language} code" + (fileName != null ? $" from `{fileName}`" : "") +
                     $". Instruction: {instruction}\n\n```{language}\n{TrimContext(code, 4000)}\n```";

        return await ChatAsync(prompt, systemPrompt);
    }

    public async Task<string?> GenerateTestsAsync(string code, string language, string? fileName = null, string? testFramework = null)
    {
        var framework = testFramework ?? GetDefaultTestFramework(language);
        var systemPrompt = $"You are a test generation expert. Generate comprehensive unit tests using {framework}. " +
                           "Cover edge cases, error conditions, and common scenarios. " +
                           "Return ONLY the test code inside a single markdown fenced code block. " +
                           "Include necessary imports/using statements.";

        var prompt = $"Generate unit tests for this {language} code" + (fileName != null ? $" from `{fileName}`" : "") +
                     $" using {framework}:\n\n```{language}\n{TrimContext(code, 4000)}\n```";

        return await ChatAsync(prompt, systemPrompt);
    }

    public async Task<string?> FixCodeAsync(string code, string language, string diagnostics)
    {
        var systemPrompt = "You are a code fixing expert. Fix the issues described in the diagnostics. " +
                           "Return ONLY the fixed code inside a single markdown fenced code block. " +
                           "Add a brief explanation of what was fixed after the code block.";

        var prompt = $"Fix the following issues in this {language} code:\n\nDiagnostics:\n{diagnostics}\n\n```{language}\n{TrimContext(code, 4000)}\n```";

        return await ChatAsync(prompt, systemPrompt);
    }

    public async Task<string?> GenerateDocstringAsync(string code, string language)
    {
        var systemPrompt = "You are a documentation expert. Generate documentation comments/docstrings for the given code. " +
                           "Return ONLY the documented code inside a single markdown fenced code block.";

        var prompt = $"Add documentation comments to this {language} code:\n\n```{language}\n{TrimContext(code, 3000)}\n```";

        return await ChatAsync(prompt, systemPrompt);
    }

    private static string GetDefaultTestFramework(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "csharp" or "c#" => "xUnit",
            "javascript" or "typescript" or "jsx" or "tsx" => "Jest",
            "python" => "pytest",
            "go" => "testing (standard library)",
            "rust" => "#[cfg(test)] module",
            "java" => "JUnit 5",
            "ruby" => "RSpec",
            "php" => "PHPUnit",
            _ => "appropriate testing framework"
        };
    }

    private static string TrimContext(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return text[..maxLength] + "\n// ... (truncated)";
    }
}
