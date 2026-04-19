using System.Drawing.Imaging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CaptchaAlarm.Services
{
    /// <summary>
    /// Uses GPT-4o Vision to read the distorted text in the Daeva Verification
    /// CAPTCHA image and return the plain-text answer.
    /// </summary>
    internal static class OpenAiCaptchaSolver
    {
        private const string Endpoint = "https://api.openai.com/v1/chat/completions";
        private const string Model    = "gpt-4o";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

        private const string Prompt =
            "This is a CAPTCHA image containing distorted, overlapping colored lines that form letters or numbers. " +
            "Read only the text shown in the image and reply with ONLY the characters — no explanation, no punctuation, no extra words. " +
            "If you cannot read it, reply with exactly: UNSOLVABLE";

        /// <summary>
        /// Sends the CAPTCHA image to GPT-4o Vision and returns
        /// (solution, null) on success or (null, errorMessage) on failure.
        /// </summary>
        public static async Task<(string? Solution, string? Error)> SolveAsync(string apiKey, Bitmap image)
        {
            try
            {
                // Encode image to base64 JPEG
                byte[] imageBytes;
                using (var ms = new System.IO.MemoryStream())
                {
                    // Scale up if too small — helps GPT-4o read tiny characters
                    Bitmap toSend = image.Width < 200
                        ? new Bitmap(image, new Size(image.Width * 2, image.Height * 2))
                        : image;

                    toSend.Save(ms, ImageFormat.Jpeg);
                    if (!ReferenceEquals(toSend, image)) toSend.Dispose();
                    imageBytes = ms.ToArray();
                }

                string base64 = Convert.ToBase64String(imageBytes);
                AppLogger.Log($"[OpenAI] Image size: {imageBytes.Length / 1024}KB");

                // Build request payload
                var payload = new
                {
                    model = Model,
                    max_tokens = 20,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = Prompt },
                                new
                                {
                                    type = "image_url",
                                    image_url = new
                                    {
                                        url    = $"data:image/jpeg;base64,{base64}",
                                        detail = "high"
                                    }
                                }
                            }
                        }
                    }
                };

                string json = JsonConvert.SerializeObject(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await Http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    AppLogger.Log($"[OpenAI] HTTP {(int)response.StatusCode}: {body}");
                    return (null, $"HTTP {(int)response.StatusCode}: {TryExtractOpenAiError(body)}");
                }

                var obj      = JObject.Parse(body);
                string? text = obj["choices"]?[0]?["message"]?["content"]?.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(text) || text.Equals("UNSOLVABLE", StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Log("[OpenAI] Could not read captcha.");
                    return (null, "GPT-4o could not read the CAPTCHA text.");
                }

                // Strip any accidental quotes/spaces
                text = text.Trim('"', '\'', ' ');
                AppLogger.Log($"[OpenAI] Solution: {text}");
                return (text, null);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[OpenAI] Exception: {ex.Message}");
                return (null, ex.Message);
            }
        }

        private static string TryExtractOpenAiError(string body)
        {
            try
            {
                var msg = JObject.Parse(body)["error"]?["message"]?.ToString();
                return msg ?? body;
            }
            catch { return body; }
        }
    }
}
