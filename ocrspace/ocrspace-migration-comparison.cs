// =============================================================================
// OCR.space to IronOCR Migration Comparison
// =============================================================================
// Side-by-side examples showing the complexity difference between:
// - OCR.space REST API (no SDK, DIY everything)
// - IronOCR NuGet package (first-class .NET support)
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using IronOcr;

namespace OcrSpaceMigration
{
    // =========================================================================
    // COMPARISON 1: Basic Text Extraction
    // =========================================================================

    /// <summary>
    /// OCR.space approach: 50+ lines for basic text extraction.
    /// </summary>
    public class OcrSpaceBasicExtraction
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OcrSpaceBasicExtraction(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public async Task<string> ExtractText(string imagePath)
        {
            // Read file and encode
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64 = Convert.ToBase64String(imageBytes);

            // Build form data
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("apikey", _apiKey),
                new KeyValuePair<string, string>("base64Image", $"data:image/png;base64,{base64}"),
                new KeyValuePair<string, string>("language", "eng")
            });

            // Send to cloud (document leaves your infrastructure)
            var response = await _httpClient.PostAsync(
                "https://api.ocr.space/parse/image", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API error: {response.StatusCode}");
            }

            // Parse JSON response manually
            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("ParsedResults")[0]
                .GetProperty("ParsedText")
                .GetString() ?? string.Empty;
        }
    }

    /// <summary>
    /// IronOCR approach: 3 lines for the same result.
    /// </summary>
    public class IronOcrBasicExtraction
    {
        public string ExtractText(string imagePath)
        {
            // Complete implementation - document stays local
            var result = new IronTesseract().Read(imagePath);
            return result.Text;
        }
    }

    // =========================================================================
    // COMPARISON 2: PDF Processing
    // =========================================================================

    /// <summary>
    /// OCR.space PDF processing: Must handle multi-page manually.
    /// </summary>
    public class OcrSpacePdfProcessing
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OcrSpacePdfProcessing(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public async Task<List<string>> ExtractTextFromPdf(string pdfPath)
        {
            var pageTexts = new List<string>();

            // Check file size (free tier: 5MB limit)
            var fileInfo = new FileInfo(pdfPath);
            if (fileInfo.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "File exceeds 5MB free tier limit. Upgrade or split PDF.");
            }

            // Read and encode PDF
            byte[] pdfBytes = File.ReadAllBytes(pdfPath);
            string base64 = Convert.ToBase64String(pdfBytes);

            // Build request
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("apikey", _apiKey),
                new KeyValuePair<string, string>("base64Image", $"data:application/pdf;base64,{base64}"),
                new KeyValuePair<string, string>("language", "eng"),
                new KeyValuePair<string, string>("filetype", "PDF"),
                new KeyValuePair<string, string>("isCreateSearchablePdf", "false") // Watermarked on free tier!
            });

            // Send to cloud
            var response = await _httpClient.PostAsync(
                "https://api.ocr.space/parse/image", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API error: {response.StatusCode}");
            }

            // Parse response - each page is a separate result
            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("ParsedResults", out var results))
            {
                foreach (var result in results.EnumerateArray())
                {
                    if (result.TryGetProperty("ParsedText", out var text))
                    {
                        pageTexts.Add(text.GetString() ?? string.Empty);
                    }
                }
            }

            return pageTexts;
        }
    }

    /// <summary>
    /// IronOCR PDF processing: Built-in support, no file limits.
    /// </summary>
    public class IronOcrPdfProcessing
    {
        public List<string> ExtractTextFromPdf(string pdfPath)
        {
            var pageTexts = new List<string>();

            // Built-in PDF support - no size limits, all pages
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);

            var result = new IronTesseract().Read(input);

            foreach (var page in result.Pages)
            {
                pageTexts.Add(page.Text);
            }

            return pageTexts;
        }
    }

    // =========================================================================
    // COMPARISON 3: Batch Processing
    // =========================================================================

    /// <summary>
    /// OCR.space batch processing: Must handle rate limits, retries.
    /// </summary>
    public class OcrSpaceBatchProcessing
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly SemaphoreSlim _rateLimiter;

        public OcrSpaceBatchProcessing(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _rateLimiter = new SemaphoreSlim(60, 60); // Free tier: 60/minute
        }

        public async Task<Dictionary<string, string>> ProcessBatch(string[] imagePaths)
        {
            var results = new Dictionary<string, string>();

            foreach (var imagePath in imagePaths)
            {
                // Apply rate limiting
                await _rateLimiter.WaitAsync();

                try
                {
                    // Process with retry logic
                    string text = await ProcessWithRetry(imagePath);
                    results[imagePath] = text;
                }
                finally
                {
                    // Release after delay
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000); // 1 second between requests
                        _rateLimiter.Release();
                    });
                }
            }

            return results;
        }

        private async Task<string> ProcessWithRetry(string imagePath, int maxRetries = 3)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(imagePath);
                    string base64 = Convert.ToBase64String(imageBytes);

                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("apikey", _apiKey),
                        new KeyValuePair<string, string>("base64Image", $"data:image/png;base64,{base64}"),
                        new KeyValuePair<string, string>("language", "eng")
                    });

                    var response = await _httpClient.PostAsync(
                        "https://api.ocr.space/parse/image", content);

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        // Rate limited - wait and retry
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    return doc.RootElement
                        .GetProperty("ParsedResults")[0]
                        .GetProperty("ParsedText")
                        .GetString() ?? string.Empty;
                }
                catch (HttpRequestException) when (attempt < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }

            throw new Exception($"Failed to process {imagePath} after {maxRetries} attempts");
        }
    }

    /// <summary>
    /// IronOCR batch processing: Simple loop, no rate limits.
    /// </summary>
    public class IronOcrBatchProcessing
    {
        public Dictionary<string, string> ProcessBatch(string[] imagePaths)
        {
            var results = new Dictionary<string, string>();
            var ocr = new IronTesseract();

            foreach (var imagePath in imagePaths)
            {
                // No rate limits, no retries needed, no cloud dependency
                var result = ocr.Read(imagePath);
                results[imagePath] = result.Text;
            }

            return results;
        }

        // Or process all at once with OcrInput
        public string ProcessAllImages(string[] imagePaths)
        {
            using var input = new OcrInput();

            foreach (var path in imagePaths)
            {
                input.LoadImage(path);
            }

            return new IronTesseract().Read(input).Text;
        }
    }

    // =========================================================================
    // COMPARISON 4: Error Handling
    // =========================================================================

    /// <summary>
    /// OCR.space error handling: Parse JSON errors, handle HTTP errors.
    /// </summary>
    public class OcrSpaceErrorHandling
    {
        public async Task<string> SafeExtract(HttpClient client, string apiKey, string imagePath)
        {
            try
            {
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                string base64 = Convert.ToBase64String(imageBytes);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("apikey", apiKey),
                    new KeyValuePair<string, string>("base64Image", $"data:image/png;base64,{base64}")
                });

                var response = await client.PostAsync(
                    "https://api.ocr.space/parse/image", content);

                // Handle HTTP-level errors
                if (!response.IsSuccessStatusCode)
                {
                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.Unauthorized:
                            throw new Exception("Invalid API key");
                        case System.Net.HttpStatusCode.TooManyRequests:
                            throw new Exception("Rate limit exceeded - wait and retry");
                        case System.Net.HttpStatusCode.PaymentRequired:
                            throw new Exception("Quota exceeded - upgrade plan");
                        default:
                            throw new Exception($"API error: {response.StatusCode}");
                    }
                }

                // Handle API-level errors in JSON
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("IsErroredOnProcessing", out var isError)
                    && isError.GetBoolean())
                {
                    string errorMsg = "Processing failed";
                    if (doc.RootElement.TryGetProperty("ErrorMessage", out var errors))
                    {
                        var messages = new List<string>();
                        foreach (var e in errors.EnumerateArray())
                        {
                            messages.Add(e.GetString() ?? "Unknown error");
                        }
                        errorMsg = string.Join("; ", messages);
                    }
                    throw new Exception(errorMsg);
                }

                // Check per-page errors
                if (doc.RootElement.TryGetProperty("ParsedResults", out var results))
                {
                    foreach (var result in results.EnumerateArray())
                    {
                        if (result.TryGetProperty("FileParseExitCode", out var exitCode)
                            && exitCode.GetInt32() != 1)
                        {
                            throw new Exception($"Page parse failed: exit code {exitCode.GetInt32()}");
                        }
                    }
                }

                return doc.RootElement
                    .GetProperty("ParsedResults")[0]
                    .GetProperty("ParsedText")
                    .GetString() ?? string.Empty;
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid API response: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// IronOCR error handling: Typed exceptions, clear messages.
    /// </summary>
    public class IronOcrErrorHandling
    {
        public string SafeExtract(string imagePath)
        {
            try
            {
                var result = new IronTesseract().Read(imagePath);
                return result.Text;
            }
            catch (FileNotFoundException)
            {
                throw; // File not found
            }
            catch (Exception ex) when (ex.Message.Contains("corrupt"))
            {
                throw new Exception($"Image file is corrupt: {imagePath}");
            }
            // That's it - no JSON parsing, no HTTP errors, no rate limits
        }
    }

    // =========================================================================
    // LINES OF CODE COMPARISON
    // =========================================================================
    /*
    Summary of code complexity:

    | Scenario           | OCR.space Lines | IronOCR Lines | Difference |
    |--------------------|-----------------|---------------|------------|
    | Basic extraction   | 50+             | 3             | 94% less   |
    | PDF processing     | 60+             | 12            | 80% less   |
    | Batch processing   | 80+             | 15            | 81% less   |
    | Error handling     | 70+             | 15            | 79% less   |
    | Full client        | 200+            | 0 (NuGet)     | 100% less  |

    Additional OCR.space requirements not shown:
    - API key management and security
    - Rate limiting implementation
    - Retry logic with exponential backoff
    - Network error handling
    - JSON parsing for every response
    - File size checking (free tier limits)
    - Watermark handling (free tier PDFs)

    IronOCR includes all of this built-in via NuGet package.
    */

    // =========================================================================
    // COMPLETE MIGRATION EXAMPLE
    // =========================================================================

    /// <summary>
    /// Before: OCR.space service wrapper (simplified).
    /// </summary>
    public class OcrSpaceService : IDisposable
    {
        private readonly HttpClient _client = new();
        private readonly string _apiKey;

        public OcrSpaceService(string apiKey) => _apiKey = apiKey;

        public async Task<string> ProcessDocument(string path)
        {
            // All the complexity from above...
            // 50+ lines of implementation
            byte[] bytes = File.ReadAllBytes(path);
            string base64 = Convert.ToBase64String(bytes);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("apikey", _apiKey),
                new KeyValuePair<string, string>("base64Image", $"data:image/png;base64,{base64}")
            });

            var response = await _client.PostAsync("https://api.ocr.space/parse/image", content);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("ParsedResults")[0].GetProperty("ParsedText").GetString()!;
        }

        public void Dispose() => _client.Dispose();
    }

    /// <summary>
    /// After: IronOCR service wrapper.
    /// </summary>
    public class IronOcrService
    {
        private readonly IronTesseract _ocr = new();

        public string ProcessDocument(string path)
        {
            return _ocr.Read(path).Text;
        }

        // No Dispose needed - no HttpClient, no external connections
    }

    // =========================================================================
    // MIGRATION CHECKLIST
    // =========================================================================
    /*
    To migrate from OCR.space to IronOCR:

    1. [ ] Install IronOCR NuGet package:
           Install-Package IronOcr

    2. [ ] Remove OCR.space dependencies:
           - HttpClient setup for OCR.space
           - API key configuration
           - JSON parsing code
           - Rate limiting implementation
           - Retry logic

    3. [ ] Replace extraction calls:
           Before: await ocrSpaceClient.ExtractText(path)
           After:  new IronTesseract().Read(path).Text

    4. [ ] Update error handling:
           Before: HTTP errors, JSON errors, rate limit errors
           After:  Standard .NET exceptions only

    5. [ ] Remove cloud-related configuration:
           - API keys from config
           - Rate limit settings
           - Timeout configurations for network calls

    6. [ ] Benefits gained:
           - Documents never leave your servers
           - No per-request costs
           - No rate limits
           - No file size limits
           - No watermarks
           - Full offline capability
           - First-class .NET IntelliSense support
    */
}
