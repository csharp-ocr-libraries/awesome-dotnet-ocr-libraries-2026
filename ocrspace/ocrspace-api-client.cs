// =============================================================================
// OCR.space REST API Client - Complete .NET Implementation
// =============================================================================
// This file demonstrates what .NET developers must build themselves since
// OCR.space provides no official NuGet package or SDK.
//
// Compare this 80+ line implementation to IronOCR's 3-line equivalent:
//   var result = new IronTesseract().Read("image.png");
//   Console.WriteLine(result.Text);
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OcrSpaceClient
{
    /// <summary>
    /// Manual REST client for OCR.space API.
    /// This is what every .NET developer must build themselves because
    /// OCR.space provides no official NuGet package.
    /// </summary>
    public class OcrSpaceApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly SemaphoreSlim _rateLimiter;
        private const string ApiEndpoint = "https://api.ocr.space/parse/image";
        private const int MaxFileSizeFree = 5 * 1024 * 1024; // 5MB limit on free tier
        private const int RateLimitPerMinute = 60;

        public OcrSpaceApiClient(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(120);

            // DIY rate limiting - you must implement this yourself
            _rateLimiter = new SemaphoreSlim(RateLimitPerMinute, RateLimitPerMinute);
        }

        /// <summary>
        /// Extract text from an image file.
        /// WARNING: Document is uploaded to OCR.space cloud servers.
        /// </summary>
        public async Task<OcrResult> ExtractTextFromFileAsync(
            string filePath,
            string language = "eng",
            CancellationToken cancellationToken = default)
        {
            // Validate file exists
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Image file not found", filePath);

            // Check file size (free tier: 5MB limit)
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MaxFileSizeFree)
            {
                throw new InvalidOperationException(
                    $"File size {fileInfo.Length / 1024 / 1024}MB exceeds free tier limit of 5MB. " +
                    "Upgrade to paid plan or use IronOCR (no size limits).");
            }

            // Apply rate limiting
            await _rateLimiter.WaitAsync(cancellationToken);

            try
            {
                // Read and base64 encode file (documents leave your infrastructure here)
                byte[] imageBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                string base64Image = Convert.ToBase64String(imageBytes);
                string mimeType = GetMimeType(filePath);

                // Build form data
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("apikey", _apiKey),
                    new KeyValuePair<string, string>("base64Image", $"data:{mimeType};base64,{base64Image}"),
                    new KeyValuePair<string, string>("language", language),
                    new KeyValuePair<string, string>("isOverlayRequired", "false"),
                    new KeyValuePair<string, string>("filetype", Path.GetExtension(filePath).TrimStart('.')),
                    new KeyValuePair<string, string>("detectOrientation", "true"),
                    new KeyValuePair<string, string>("scale", "true"),
                    new KeyValuePair<string, string>("OCREngine", "2") // Engine 2 for better accuracy
                });

                // Send request to OCR.space cloud
                var response = await _httpClient.PostAsync(ApiEndpoint, formContent, cancellationToken);

                // Handle HTTP errors
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new OcrSpaceException(
                        $"OCR.space API returned {response.StatusCode}: {errorBody}",
                        (int)response.StatusCode);
                }

                // Parse JSON response (no typed models - you must deserialize yourself)
                string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseOcrResponse(jsonResponse);
            }
            finally
            {
                // Release rate limiter after delay
                _ = Task.Run(async () =>
                {
                    await Task.Delay(60000 / RateLimitPerMinute); // Space out requests
                    _rateLimiter.Release();
                });
            }
        }

        /// <summary>
        /// Parse the JSON response from OCR.space.
        /// No SDK means you parse JSON manually.
        /// </summary>
        private OcrResult ParseOcrResponse(string jsonResponse)
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            // Check for API-level errors
            if (root.TryGetProperty("IsErroredOnProcessing", out var isErrored) && isErrored.GetBoolean())
            {
                string errorMessage = "OCR processing failed";
                if (root.TryGetProperty("ErrorMessage", out var errorMessages))
                {
                    errorMessage = string.Join("; ", ParseErrorMessages(errorMessages));
                }
                throw new OcrSpaceException(errorMessage, 500);
            }

            // Extract parsed results
            var result = new OcrResult();

            if (root.TryGetProperty("ParsedResults", out var parsedResults))
            {
                foreach (var parsedResult in parsedResults.EnumerateArray())
                {
                    if (parsedResult.TryGetProperty("ParsedText", out var parsedText))
                    {
                        result.ParsedText += parsedText.GetString();
                    }

                    if (parsedResult.TryGetProperty("ErrorMessage", out var pageError) &&
                        !string.IsNullOrEmpty(pageError.GetString()))
                    {
                        result.Warnings.Add(pageError.GetString());
                    }

                    // Extract exit code for this page
                    if (parsedResult.TryGetProperty("FileParseExitCode", out var exitCode))
                    {
                        result.ExitCodes.Add(exitCode.GetInt32());
                    }
                }
            }

            // Extract processing time
            if (root.TryGetProperty("ProcessingTimeInMilliseconds", out var processingTime))
            {
                result.ProcessingTimeMs = processingTime.GetDouble();
            }

            return result;
        }

        private List<string> ParseErrorMessages(JsonElement errorMessages)
        {
            var messages = new List<string>();
            if (errorMessages.ValueKind == JsonValueKind.Array)
            {
                foreach (var msg in errorMessages.EnumerateArray())
                {
                    messages.Add(msg.GetString() ?? "Unknown error");
                }
            }
            return messages;
        }

        private string GetMimeType(string filePath)
        {
            return Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tiff" or ".tif" => "image/tiff",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _rateLimiter?.Dispose();
        }
    }

    /// <summary>
    /// OCR result container - manually defined since no SDK provides this.
    /// </summary>
    public class OcrResult
    {
        public string ParsedText { get; set; } = string.Empty;
        public double ProcessingTimeMs { get; set; }
        public List<string> Warnings { get; } = new List<string>();
        public List<int> ExitCodes { get; } = new List<int>();
    }

    /// <summary>
    /// Custom exception for OCR.space errors - you must define this yourself.
    /// </summary>
    public class OcrSpaceException : Exception
    {
        public int StatusCode { get; }

        public OcrSpaceException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    // =========================================================================
    // Usage Example - What developers must write
    // =========================================================================
    public class OcrSpaceUsageExample
    {
        public async Task DemoUsage()
        {
            // 80+ lines of client code, plus usage:
            using var client = new OcrSpaceApiClient("YOUR_API_KEY");

            try
            {
                // Document is sent to OCR.space cloud
                var result = await client.ExtractTextFromFileAsync("invoice.png");
                Console.WriteLine($"Extracted text: {result.ParsedText}");
                Console.WriteLine($"Processing time: {result.ProcessingTimeMs}ms");
            }
            catch (OcrSpaceException ex) when (ex.StatusCode == 429)
            {
                Console.WriteLine("Rate limit exceeded - try again later");
            }
            catch (OcrSpaceException ex)
            {
                Console.WriteLine($"OCR.space error: {ex.Message}");
            }
        }
    }

    // =========================================================================
    // IronOCR Equivalent - 3 Lines Total
    // =========================================================================
    /*
    using IronOcr;

    public class IronOcrEquivalent
    {
        public void DemoUsage()
        {
            // Complete implementation - nothing else needed
            var result = new IronTesseract().Read("invoice.png");
            Console.WriteLine($"Extracted text: {result.Text}");

            // That's it. No HTTP client, no JSON parsing, no rate limiting,
            // no API keys, no cloud dependency, no data privacy concerns.
        }
    }
    */
}
