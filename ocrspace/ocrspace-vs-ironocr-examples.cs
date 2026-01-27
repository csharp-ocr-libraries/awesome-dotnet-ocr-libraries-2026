/**
 * OCR.space vs IronOCR: Code Examples
 *
 * Compare OCR.space cloud API with IronOCR's on-premise solution.
 * OCR.space sends documents to their cloud; IronOCR processes locally.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * OCR.space: Cloud API (https://ocr.space)
 * IronOCR NuGet: https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

// ============================================================================
// OCR.SPACE IMPLEMENTATION - Cloud API
// Your documents are uploaded to their servers
// ============================================================================

namespace OcrSpaceExamples
{
    /// <summary>
    /// OCR.space is a REST API.
    /// Documents are sent to their cloud servers.
    /// Free tier has limits; paid plans scale.
    /// </summary>
    public class OcrSpaceService
    {
        private readonly HttpClient _client;
        private readonly string _apiKey;
        private const string ApiUrl = "https://api.ocr.space/parse/image";

        public OcrSpaceService(string apiKey)
        {
            _apiKey = apiKey;
            _client = new HttpClient();
        }

        /// <summary>
        /// OCR via cloud API - document leaves your infrastructure
        /// </summary>
        public async Task<string> ExtractTextAsync(string imagePath)
        {
            // WARNING: File is uploaded to OCR.space servers
            using var content = new MultipartFormDataContent();
            var imageBytes = File.ReadAllBytes(imagePath);
            content.Add(new ByteArrayContent(imageBytes), "file", Path.GetFileName(imagePath));
            content.Add(new StringContent(_apiKey), "apikey");
            content.Add(new StringContent("eng"), "language");

            var response = await _client.PostAsync(ApiUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            // Parse JSON response...
            return json; // Simplified - would parse ParsedResults[0].ParsedText
        }

        /// <summary>
        /// OCR from URL
        /// </summary>
        public async Task<string> ExtractFromUrlAsync(string imageUrl)
        {
            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("apikey", _apiKey),
                new KeyValuePair<string, string>("url", imageUrl),
                new KeyValuePair<string, string>("language", "eng")
            });

            var response = await _client.PostAsync(ApiUrl, content);
            return await response.Content.ReadAsStringAsync();
        }
    }

    public class OcrSpaceConsiderations
    {
        public void ShowConsiderations()
        {
            Console.WriteLine("OCR.space Considerations:");
            Console.WriteLine("1. Documents uploaded to their servers");
            Console.WriteLine("2. Requires internet connection");
            Console.WriteLine("3. Free tier: 500 calls/month");
            Console.WriteLine("4. Latency depends on network");
            Console.WriteLine("5. Data privacy concerns");
            Console.WriteLine();
            Console.WriteLine("For on-premise OCR, use IronOCR:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - ON-PREMISE, NO CLOUD REQUIRED
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR processes locally - your data never leaves your server.
    /// No API calls, no internet dependency, no per-document costs.
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Local OCR - data stays on your machine
        /// </summary>
        public string ExtractText(string imagePath)
        {
            // No cloud, no API calls, no data transmission
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// Works offline - air-gapped networks supported
        /// </summary>
        public string ExtractTextOffline(string imagePath)
        {
            // Works without internet - perfect for secure environments
            var ocr = new IronTesseract();
            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// PDF processing - no upload required
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// No rate limits - process as many as you need
        /// </summary>
        public void BatchProcess(string[] imagePaths)
        {
            var ocr = new IronTesseract();

            // No 500/month limit like OCR.space free tier
            foreach (var path in imagePaths)
            {
                var result = ocr.Read(path);
                Console.WriteLine($"{path}: {result.Text.Length} chars");
            }
        }

        /// <summary>
        /// Multi-language - 125+ languages available
        /// </summary>
        public string MultiLanguageOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            ocr.AddSecondaryLanguage(OcrLanguage.German);

            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Create searchable PDF
        /// </summary>
        public void CreateSearchablePdf(string inputPath, string outputPath)
        {
            var result = new IronTesseract().Read(inputPath);
            result.SaveAsSearchablePdf(outputPath);
        }

        /// <summary>
        /// From byte array (no temp files needed)
        /// </summary>
        public string ExtractFromBytes(byte[] imageBytes)
        {
            using var input = new OcrInput();
            input.LoadImage(imageBytes);
            return new IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// COMPARISON: CLOUD vs ON-PREMISE
// ============================================================================

namespace Comparison
{
    public class OcrSpaceVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== OCR.SPACE vs IRONOCR ===\n");

            Console.WriteLine("Feature          | OCR.space      | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("Data location    | Their cloud    | Your machine");
            Console.WriteLine("Internet needed  | Yes            | No");
            Console.WriteLine("Free tier        | 500/month      | Trial");
            Console.WriteLine("Paid pricing     | Per call       | One-time");
            Console.WriteLine("PDF support      | Yes            | Yes");
            Console.WriteLine("Languages        | 25             | 125+");
            Console.WriteLine("Searchable PDF   | No             | Yes");
            Console.WriteLine("Air-gapped       | No             | Yes");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public void SecurityComparison()
        {
            Console.WriteLine("=== SECURITY & COMPLIANCE ===\n");

            Console.WriteLine("OCR.SPACE (Cloud):");
            Console.WriteLine("  - Data transmitted over internet");
            Console.WriteLine("  - Stored on third-party servers");
            Console.WriteLine("  - Compliance review needed");
            Console.WriteLine("  - Not suitable for classified data");
            Console.WriteLine();

            Console.WriteLine("IRONOCR (On-Premise):");
            Console.WriteLine("  - Data never leaves your server");
            Console.WriteLine("  - No network transmission");
            Console.WriteLine("  - HIPAA, GDPR compatible");
            Console.WriteLine("  - Works in air-gapped networks");
            Console.WriteLine("  - ITAR, FedRAMP, CMMC ready");
            Console.WriteLine();
            Console.WriteLine("Download: https://ironsoftware.com/csharp/ocr/");
        }

        public void CostComparison()
        {
            Console.WriteLine("=== COST COMPARISON (10,000 docs/month) ===\n");

            Console.WriteLine("OCR.SPACE:");
            Console.WriteLine("  - Free: 500/month (not enough)");
            Console.WriteLine("  - Pro: ~$50-200/month");
            Console.WriteLine("  - Annual: $600-2,400");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  - One-time: $749-2,999");
            Console.WriteLine("  - Unlimited documents");
            Console.WriteLine("  - Pays for itself in 1-4 months");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// KEEP YOUR DATA LOCAL WITH IRONOCR
//
// No cloud required. No API limits. No data transmission.
// Perfect for compliance-sensitive environments.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
//
// Install: Install-Package IronOcr
// ============================================================================
