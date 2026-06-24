using System.Text;
using System.Text.Json;
using RentalHub.Services.Interfaces;

namespace RentalHub.Services.Implementations;

public class KycService : IKycService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public KycService(IConfiguration config, HttpClient http)
    {
        _config = config;
        _http   = http;
    }

    public async Task<(bool approved, string reason)> VerifyAsync(
        string documentPath, string selfiePath)
    {
        var apiKey = _config["OpenAI:ApiKey"];

        // Si no hay API key usar simulacion
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "TU_API_KEY_AQUI")
        {
            await Task.Delay(1500);
            var docSize    = new FileInfo(documentPath).Length;
            var selfieSize = new FileInfo(selfiePath).Length;

            if (docSize < 1000)
                return (false, "El documento no es legible o esta vacio.");
            if (selfieSize < 1000)
                return (false, "La selfie no es valida o esta vacia.");

            return (true, "Verificacion simulada completada correctamente.");
        }

        // Verificacion real con GPT-4o
        try
        {
            var docBytes     = await File.ReadAllBytesAsync(documentPath);
            var selfieBytes  = await File.ReadAllBytesAsync(selfiePath);
            var docBase64    = Convert.ToBase64String(docBytes);
            var selfieBase64 = Convert.ToBase64String(selfieBytes);

            var payload = new
            {
                model      = "gpt-4o",
                max_tokens = 256,
                messages   = new[]
                {
                    new
                    {
                        role    = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = @"Eres un sistema KYC. Analiza el documento de identidad (primera imagen) y la selfie (segunda imagen).
Responde SOLO con este JSON sin texto adicional ni markdown:
{""approved"": true, ""reason"": ""motivo breve""}
Aprueba si: documento legible con foto y datos visibles, persona en selfie parece real.
Rechaza si: documento ilegible, imagen borrosa, no es un documento de identidad valido."
                            },
                            new
                            {
                                type      = "image_url",
                                image_url = new { url = $"data:image/jpeg;base64,{docBase64}" }
                            },
                            new
                            {
                                type      = "image_url",
                                image_url = new { url = $"data:image/jpeg;base64,{selfieBase64}" }
                            }
                        }
                    }
                }
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response     = await _http.PostAsync(
                "https://api.openai.com/v1/chat/completions", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Error al verificar: {response.StatusCode}");

            var doc  = JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            text = text.Replace("```json", "").Replace("```", "").Trim();

            var result   = JsonDocument.Parse(text);
            var approved = result.RootElement.GetProperty("approved").GetBoolean();
            var reason   = result.RootElement.GetProperty("reason").GetString() ?? "";

            return (approved, reason);
        }
        catch (Exception ex)
        {
            return (false, $"Error en la verificacion: {ex.Message}");
        }
    }
}
