/**
 * Azure Computer Vision vs IronOCR: Code Examples
 *
 * This file demonstrates OCR implementations with both Azure Computer Vision
 * and IronOCR, highlighting the differences in complexity, data handling,
 * and overall developer experience.
 *
 * KEY INSIGHT: Azure sends your documents to Microsoft's cloud.
 *              IronOCR processes everything locally.
 *
 * NuGet Packages:
 * - Azure.AI.Vision.ImageAnalysis (Azure)
 * - IronOcr (IronOCR)
 */

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// AZURE COMPUTER VISION IMPLEMENTATION
// All documents are uploaded to Microsoft Azure servers for processing
// ============================================================================

namespace AzureComputerVisionExamples
{
    using Azure;
    using Azure.AI.Vision.ImageAnalysis;

    /// <summary>
    /// Azure Computer Vision OCR requires:
    /// 1. Azure subscription
    /// 2. Cognitive Services resource
    /// 3. API key and endpoint configuration
    /// 4. Network connectivity to Azure
    /// </summary>
    public class AzureOcrService
    {
        private readonly ImageAnalysisClient _client;

        public AzureOcrService(string endpoint, string apiKey)
        {
            // Requires Azure resource setup
            _client = new ImageAnalysisClient(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey));
        }

        /// <summary>
        /// Basic text extraction - sends image to Azure
        /// </summary>
        public async Task<string> ExtractTextAsync(string imagePath)
        {
            // WARNING: Your document is uploaded to Microsoft Azure
            using var stream = File.OpenRead(imagePath);
            var imageData = BinaryData.FromStream(stream);

            var result = await _client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read);

            var text = new StringBuilder();
            foreach (var block in result.Value.Read.Blocks)
            {
                foreach (var line in block.Lines)
                {
                    text.AppendLine(line.Text);
                }
            }

            return text.ToString();
        }

        /// <summary>
        /// Extract text with confidence scores
        /// </summary>
        public async Task<(string Text, float Confidence)> ExtractWithConfidenceAsync(string imagePath)
        {
            using var stream = File.OpenRead(imagePath);
            var imageData = BinaryData.FromStream(stream);

            var result = await _client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read);

            var text = new StringBuilder();
            float totalConfidence = 0;
            int wordCount = 0;

            foreach (var block in result.Value.Read.Blocks)
            {
                foreach (var line in block.Lines)
                {
                    text.AppendLine(line.Text);
                    foreach (var word in line.Words)
                    {
                        totalConfidence += word.Confidence;
                        wordCount++;
                    }
                }
            }

            float avgConfidence = wordCount > 0 ? totalConfidence / wordCount : 0;
            return (text.ToString(), avgConfidence);
        }

        /// <summary>
        /// Word-level extraction with bounding boxes
        /// </summary>
        public async Task ExtractWordsWithPositionsAsync(string imagePath)
        {
            using var stream = File.OpenRead(imagePath);
            var imageData = BinaryData.FromStream(stream);

            var result = await _client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read);

            foreach (var block in result.Value.Read.Blocks)
            {
                foreach (var line in block.Lines)
                {
                    foreach (var word in line.Words)
                    {
                        // Polygon-based bounding box (complex)
                        var points = word.BoundingPolygon;
                        Console.WriteLine($"'{word.Text}' at polygon points, Confidence: {word.Confidence:P1}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Azure PDF processing requires Form Recognizer (separate service)
    /// </summary>
    public class AzurePdfService
    {
        // Note: Azure Computer Vision's Read API has limited PDF support
        // Full PDF processing requires Azure Form Recognizer (additional service)

        /*
        using Azure.AI.FormRecognizer.DocumentAnalysis;

        private readonly DocumentAnalysisClient _docClient;

        public async Task<string> ExtractFromPdfAsync(string pdfPath)
        {
            // PDF is uploaded to Azure
            using var stream = File.OpenRead(pdfPath);

            var operation = await _docClient.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-read",
                stream);

            var result = operation.Value;
            var text = new StringBuilder();

            foreach (var page in result.Pages)
            {
                foreach (var line in page.Lines)
                {
                    text.AppendLine(line.Content);
                }
            }

            return text.ToString();
        }
        */
    }

    /// <summary>
    /// Error handling for Azure cloud operations
    /// </summary>
    public class AzureErrorHandling
    {
        private readonly ImageAnalysisClient _client;

        public async Task<string> RobustExtractAsync(string imagePath)
        {
            const int maxRetries = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    using var stream = File.OpenRead(imagePath);
                    var imageData = BinaryData.FromStream(stream);

                    var result = await _client.AnalyzeAsync(
                        imageData,
                        VisualFeatures.Read);

                    return string.Join("\n",
                        result.Value.Read.Blocks
                            .SelectMany(b => b.Lines)
                            .Select(l => l.Text));
                }
                catch (RequestFailedException ex) when (ex.Status == 429)
                {
                    // Rate limited - Azure limits requests per second
                    attempt++;
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
                catch (RequestFailedException ex) when (ex.Status >= 500)
                {
                    // Azure service error
                    attempt++;
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                catch (RequestFailedException ex)
                {
                    // Client error - don't retry
                    throw new Exception($"Azure OCR failed: {ex.Message}", ex);
                }
            }

            throw new Exception("Max retries exceeded for Azure OCR");
        }
    }
}


// ============================================================================
// IRONOCR IMPLEMENTATION
// All processing happens locally - no data leaves your infrastructure
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR requires:
    /// 1. NuGet package installation
    /// 2. License key (optional for development)
    ///
    /// That's it. No cloud setup, no API keys, no network dependency.
    /// </summary>
    public class IronOcrService
    {
        private readonly IronTesseract _ocr;

        public IronOcrService()
        {
            _ocr = new IronTesseract();
        }

        /// <summary>
        /// Basic text extraction - processes locally
        /// </summary>
        public string ExtractText(string imagePath)
        {
            // ALL processing happens on your machine
            // No data transmitted anywhere
            var result = _ocr.Read(imagePath);
            return result.Text;
        }

        /// <summary>
        /// One-liner version
        /// </summary>
        public string ExtractTextOneLine(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// Extract with confidence - synchronous, simple
        /// </summary>
        public (string Text, double Confidence) ExtractWithConfidence(string imagePath)
        {
            var result = _ocr.Read(imagePath);
            return (result.Text, result.Confidence);
        }

        /// <summary>
        /// Word-level extraction - clean API
        /// </summary>
        public void ExtractWordsWithPositions(string imagePath)
        {
            var result = _ocr.Read(imagePath);

            foreach (var word in result.Words)
            {
                // Simple rectangle-based positioning
                Console.WriteLine($"'{word.Text}' at ({word.X},{word.Y}) " +
                    $"size ({word.Width}x{word.Height}), Confidence: {word.Confidence:F1}%");
            }
        }
    }

    /// <summary>
    /// IronOCR PDF processing - built-in, no additional services
    /// </summary>
    public class IronOcrPdfService
    {
        /// <summary>
        /// Direct PDF OCR
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            // Native PDF support - no conversion needed
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Password-protected PDFs
        /// </summary>
        public string ExtractFromEncryptedPdf(string pdfPath, string password)
        {
            // Built-in password support
            using var input = new OcrInput();
            input.LoadPdf(pdfPath, Password: password);
            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Specific page range
        /// </summary>
        public string ExtractPages(string pdfPath, int startPage, int endPage)
        {
            using var input = new OcrInput();
            input.LoadPdfPages(pdfPath, startPage, endPage);
            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Create searchable PDF from scanned PDF
        /// </summary>
        public void CreateSearchablePdf(string inputPdf, string outputPdf)
        {
            var result = new IronTesseract().Read(inputPdf);
            result.SaveAsSearchablePdf(outputPdf);
        }
    }
}


// ============================================================================
// SIDE-BY-SIDE COMPARISON
// ============================================================================

namespace ComparisonExamples
{
    using Azure;
    using Azure.AI.Vision.ImageAnalysis;
    using IronOcr;

    /// <summary>
    /// Direct comparison of the same operations
    /// </summary>
    public class SideBySideComparison
    {
        /// <summary>
        /// Compare setup requirements
        /// </summary>
        public void CompareSetup()
        {
            Console.WriteLine("=== SETUP COMPARISON ===\n");

            Console.WriteLine("AZURE COMPUTER VISION:");
            Console.WriteLine("  1. Create Azure account");
            Console.WriteLine("  2. Create Cognitive Services resource");
            Console.WriteLine("  3. Get endpoint and API key");
            Console.WriteLine("  4. Install Azure.AI.Vision.ImageAnalysis NuGet");
            Console.WriteLine("  5. Configure network access to Azure");
            Console.WriteLine("  6. Handle Azure authentication");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  1. Install IronOcr NuGet");
            Console.WriteLine("  2. (Optional) Add license key");
            Console.WriteLine("  Done.");
        }

        /// <summary>
        /// Compare basic OCR operation
        /// </summary>
        public async Task CompareBasicOcr(string imagePath)
        {
            Console.WriteLine("=== BASIC OCR COMPARISON ===\n");

            // Azure approach
            Console.WriteLine("AZURE (async, cloud):");
            Console.WriteLine(@"
var client = new ImageAnalysisClient(endpoint, credential);
using var stream = File.OpenRead(imagePath);
var imageData = BinaryData.FromStream(stream);
var result = await client.AnalyzeAsync(imageData, VisualFeatures.Read);
var text = string.Join(""\n"", result.Value.Read.Blocks
    .SelectMany(b => b.Lines)
    .Select(l => l.Text));
");
            Console.WriteLine("Lines of code: ~8");
            Console.WriteLine("Data location: Azure cloud servers");
            Console.WriteLine();

            // IronOCR approach
            Console.WriteLine("IRONOCR (sync, local):");
            Console.WriteLine(@"
var text = new IronTesseract().Read(imagePath).Text;
");
            Console.WriteLine("Lines of code: 1");
            Console.WriteLine("Data location: Your machine only");
        }

        /// <summary>
        /// Compare PDF processing
        /// </summary>
        public void ComparePdfProcessing()
        {
            Console.WriteLine("=== PDF PROCESSING COMPARISON ===\n");

            Console.WriteLine("AZURE:");
            Console.WriteLine("  - Basic PDF via Read API (limited)");
            Console.WriteLine("  - Full PDF requires Form Recognizer (separate service)");
            Console.WriteLine("  - Password PDFs require additional handling");
            Console.WriteLine("  - Creates searchable PDFs: Manual process");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  - Native PDF support: result = ocr.Read(\"doc.pdf\")");
            Console.WriteLine("  - Password PDFs: input.LoadPdf(path, Password: \"secret\")");
            Console.WriteLine("  - Creates searchable PDFs: result.SaveAsSearchablePdf(path)");
        }

        /// <summary>
        /// Compare error handling requirements
        /// </summary>
        public void CompareErrorHandling()
        {
            Console.WriteLine("=== ERROR HANDLING COMPARISON ===\n");

            Console.WriteLine("AZURE (must handle):");
            Console.WriteLine("  - Network timeouts");
            Console.WriteLine("  - Rate limiting (429 errors)");
            Console.WriteLine("  - Service unavailability (5xx errors)");
            Console.WriteLine("  - Authentication failures");
            Console.WriteLine("  - Region failover");
            Console.WriteLine("  - Retry logic with exponential backoff");
            Console.WriteLine();

            Console.WriteLine("IRONOCR (simpler):");
            Console.WriteLine("  - File not found (standard IO)");
            Console.WriteLine("  - Invalid image format");
            Console.WriteLine("  - License validation");
            Console.WriteLine("  No network-related errors to handle.");
        }

        /// <summary>
        /// Compare security implications
        /// </summary>
        public void CompareSecurityImplications()
        {
            Console.WriteLine("=== SECURITY COMPARISON ===\n");

            Console.WriteLine("AZURE COMPUTER VISION:");
            Console.WriteLine("  [!] Documents transmitted to Microsoft Azure");
            Console.WriteLine("  [!] Data crosses organizational boundary");
            Console.WriteLine("  [!] Subject to Azure data retention policies");
            Console.WriteLine("  [!] Requires BAA for HIPAA (still cloud)");
            Console.WriteLine("  [!] Not suitable for ITAR-controlled data");
            Console.WriteLine("  [!] Not suitable for classified information");
            Console.WriteLine("  [!] Requires internet connectivity");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  [✓] All processing on your infrastructure");
            Console.WriteLine("  [✓] No data transmitted anywhere");
            Console.WriteLine("  [✓] You control all data retention");
            Console.WriteLine("  [✓] HIPAA-compatible (your environment)");
            Console.WriteLine("  [✓] Suitable for ITAR (on-premise)");
            Console.WriteLine("  [✓] Can run in air-gapped networks");
            Console.WriteLine("  [✓] No internet required");
        }

        /// <summary>
        /// Compare pricing
        /// </summary>
        public void ComparePricing()
        {
            Console.WriteLine("=== PRICING COMPARISON ===\n");

            Console.WriteLine("AZURE COMPUTER VISION:");
            Console.WriteLine("  Free tier: 5,000 transactions/month");
            Console.WriteLine("  S1 tier: $1.50 per 1,000 transactions");
            Console.WriteLine("  S2 tier: $2.50 per 1,000 transactions");
            Console.WriteLine();
            Console.WriteLine("  Example: 100,000 documents/month");
            Console.WriteLine("  Cost: ~$150/month = $1,800/year");
            Console.WriteLine("  3-year cost: $5,400");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  Lite: $749 one-time");
            Console.WriteLine("  Professional: $2,999 one-time");
            Console.WriteLine("  Unlimited: $5,999 one-time");
            Console.WriteLine();
            Console.WriteLine("  Example: 100,000 documents/month");
            Console.WriteLine("  Cost: $2,999 one-time (no per-doc fees)");
            Console.WriteLine("  3-year cost: $2,999");
            Console.WriteLine();
            Console.WriteLine("  Savings over 3 years: $2,401+");
        }
    }
}


// ============================================================================
// MIGRATION EXAMPLE
// ============================================================================

namespace MigrationExample
{
    using IronOcr;

    /// <summary>
    /// How to migrate from Azure Computer Vision to IronOCR
    /// </summary>
    public class AzureToIronOcrMigration
    {
        /// <summary>
        /// Before: Azure implementation
        /// </summary>
        /*
        public async Task<string> ProcessDocumentAzure(string imagePath)
        {
            var client = new ImageAnalysisClient(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey));

            using var stream = File.OpenRead(imagePath);
            var imageData = BinaryData.FromStream(stream);

            var result = await client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read);

            return string.Join("\n", result.Value.Read.Blocks
                .SelectMany(b => b.Lines)
                .Select(l => l.Text));
        }
        */

        /// <summary>
        /// After: IronOCR implementation
        /// </summary>
        public string ProcessDocumentIronOcr(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// Interface for gradual migration
        /// </summary>
        public interface IOcrService
        {
            string ExtractText(string imagePath);
        }

        /// <summary>
        /// IronOCR implementation
        /// </summary>
        public class IronOcrOcrService : IOcrService
        {
            public string ExtractText(string imagePath)
            {
                return new IronTesseract().Read(imagePath).Text;
            }
        }
    }
}
