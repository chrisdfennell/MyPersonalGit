using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MyPersonalGit.Data;

public interface IAiCompletionService
{
    Task<string?> GetCompletionAsync(string filePath, string prefix, string suffix, string language);
}

public class AiCompletionService : IAiCompletionService
{
    private readonly IAdminService _adminService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiCompletionService(IAdminService adminService, IHttpClientFactory httpClientFactory)
    {
        _adminService = adminService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> GetCompletionAsync(string filePath, string prefix, string suffix, string language)
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        if (!settings.AiCompletionEnabled
            || string.IsNullOrEmpty(settings.AiCompletionEndpoint)
            || string.IsNullOrEmpty(settings.AiCompletionApiKey))
            return null;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.AiCompletionApiKey);
            client.Timeout = TimeSpan.FromSeconds(10);

            var fileName = Path.GetFileName(filePath);
            var systemPrompt = $"You are a code completion assistant. Complete the code at the cursor position. Return ONLY the completion text, no explanations, no markdown fences. Keep completions short (1-3 lines). Language: {language}, File: {fileName}";

            var userPrompt = prefix + "█" + suffix;
            // Trim to reasonable size
            if (userPrompt.Length > 4000)
            {
                var prefixTrimmed = prefix.Length > 2500 ? prefix[^2500..] : prefix;
                var suffixTrimmed = suffix.Length > 500 ? suffix[..500] : suffix;
                userPrompt = prefixTrimmed + "█" + suffixTrimmed;
            }

            var requestBody = new
            {
                model = settings.AiCompletionModel,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = $"Complete the code at the █ cursor:\n\n{userPrompt}" }
                },
                max_tokens = 128,
                temperature = 0.2,
                stop = new[] { "\n\n\n" }
            };

            var endpoint = settings.AiCompletionEndpoint.TrimEnd('/');
            var response = await client.PostAsync(
                $"{endpoint}/chat/completions",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0) return null;

            var text = choices[0].GetProperty("message").GetProperty("content").GetString();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AI] Completion error: {ex.Message}");
            return null;
        }
    }
}
