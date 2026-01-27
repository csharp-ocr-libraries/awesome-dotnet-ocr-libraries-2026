/**
 * Google Cloud Vision vs IronOCR: Code Examples
 *
 * This file demonstrates OCR implementations with both Google Cloud Vision
 * and IronOCR, highlighting the differences in setup, data handling, and PDF support.
 *
 * KEY INSIGHT: Google Cloud Vision sends documents to Google's cloud.
 *              IronOCR processes everything locally.
 *              Google Cloud Vision is NOT FedRAMP authorized.
 *
 * NuGet Packages:
 * - Google.Cloud.Vision.V1 (Google)
 * - IronOcr (IronOCR)
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// GOOGLE CLOUD VISION IMPLEMENTATION
// Documents are sent to Google Cloud for processing
// ============================================================================

namespace GoogleCloudVisionExamples
{
    using Google.Cloud.Vision.V1;

    /// <summary>
    /// Google Cloud Vision requires:
    /// 1. Google Cloud project
    /// 2. Enable Cloud Vision API
    /// 3. Service account with credentials JSON
    /// 4. GOOGLE_APPLICATION_CREDENTIALS environment variable
    /// </summary>
    public class GoogleVisionService
    {
        private readonly ImageAnnotatorClient _client;

        public GoogleVisionService()
        {
            // Requires GOOGLE_APPLICATION_CREDENTIALS env var
            _client = ImageAnnotatorClient.Create();
        }

        /// <summary>
        /// Basic text detection
        /// </summary>
        public string DetectText(string imagePath)
        {
            // WARNING: Image uploaded to Google Cloud
            var image = Image.FromFile(imagePath);
            var response = _client.DetectText(image);

            if (response.Count > 0)
            {
                return response[0].Description;
            }

            return string.Empty;
        }

        /// <summary>
        /// Document text detection (better for dense documents)
        /// </summary>
        public TextAnnotation DetectDocumentText(string imagePath)
        {
            var image = Image.FromFile(imagePath);
            return _client.DetectDocumentText(image);
        }

        /// <summary>
        /// Access document structure
        /// </summary>
        public void PrintDocumentStructure(string imagePath)
        {
            var annotation = DetectDocumentText(imagePath);

            foreach (var page in annotation.Pages)
            {
                Console.WriteLine($"Page confidence: {page.Confidence:P1}");

                foreach (var block in page.Blocks)
                {
                    Console.WriteLine($"  Block: {block.BlockType}");

                    foreach (var paragraph in block.Paragraphs)
                    {
                        var text = string.Join("", paragraph.Words
                            .SelectMany(w => w.Symbols)
                            .Select(s => s.Text));
                        Console.WriteLine($"    Paragraph: {text}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Google Cloud Vision PDF processing requires GCS + async
    /// </summary>
    public class GoogleVisionPdfService
    {
        /// <summary>
        /// PDF processing with Google Cloud Vision is complex:
        /// 1. Upload PDF to Google Cloud Storage
        /// 2. Submit async annotation request
        /// 3. Wait for completion
        /// 4. Download JSON results from GCS
        /// 5. Parse results
        /// 6. Clean up GCS objects
        /// </summary>
        public async Task<string> ProcessPdfAsync(string pdfPath, string gcsBucket)
        {
            // This is a simplified outline - actual implementation is much longer
            throw new NotImplementedException(@"
PDF processing with Google Cloud Vision requires:
1. Upload PDF to GCS bucket
2. Create AsyncAnnotateFileRequest
3. Submit async batch request
4. Poll for completion
5. Download JSON output files from GCS
6. Parse and combine results
7. Clean up GCS objects

This typically requires 50-100 lines of code
plus Google.Cloud.Storage.V1 NuGet package.
");
        }
    }
}


// ============================================================================
// IRONOCR IMPLEMENTATION
// All processing happens locally - no GCS, no async, no JSON parsing
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR processes everything locally
    /// No Google Cloud account, no GCS, no async job management
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Basic text detection - one line
        /// </summary>
        public string DetectText(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// Document text with structure
        /// </summary>
        public void PrintDocumentStructure(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);

            Console.WriteLine($"Confidence: {result.Confidence:F1}%");

            foreach (var page in result.Pages)
            {
                Console.WriteLine($"Page {page.PageNumber}:");

                foreach (var paragraph in page.Paragraphs)
                {
                    Console.WriteLine($"  Paragraph: {paragraph.Text}");
                }
            }
        }

        /// <summary>
        /// PDF processing - direct, simple
        /// </summary>
        public string ProcessPdf(string pdfPath)
        {
            // No GCS, no async, no JSON parsing
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Password-protected PDF
        /// </summary>
        public string ProcessEncryptedPdf(string pdfPath, string password)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath, Password: password);
            return new IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// COMPARISON
// ============================================================================

namespace Comparison
{
    public class GoogleVsIronOcrComparison
    {
        public void ComparePdfProcessing()
        {
            Console.WriteLine("=== PDF PROCESSING COMPARISON ===\n");

            Console.WriteLine("GOOGLE CLOUD VISION:");
            Console.WriteLine(@"
// 1. Install Google.Cloud.Storage.V1 NuGet
// 2. Create GCS bucket
// 3. Upload PDF to GCS
var storageClient = StorageClient.Create();
await storageClient.UploadObjectAsync(bucket, objectName, mimeType, stream);

// 4. Create async request
var asyncRequest = new AsyncAnnotateFileRequest { ... };

// 5. Submit and wait
var operation = await client.AsyncBatchAnnotateFilesAsync(requests);
await operation.PollUntilCompletedAsync();

// 6. Download and parse JSON results from GCS
// 7. Clean up GCS objects

// Total: ~50-100 lines of code
");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine(@"
var text = new IronTesseract().Read(""document.pdf"").Text;

// Total: 1 line of code
");
        }

        public void CompareFeatures()
        {
            Console.WriteLine("=== FEATURE COMPARISON ===\n");

            Console.WriteLine("Feature                    | Google Vision | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────────────");
            Console.WriteLine("Basic OCR                  | Yes          | Yes");
            Console.WriteLine("PDF (direct)               | No           | Yes");
            Console.WriteLine("PDF (via GCS async)        | Yes          | N/A");
            Console.WriteLine("Password PDFs              | No           | Yes");
            Console.WriteLine("Searchable PDF output      | No           | Yes");
            Console.WriteLine("Offline operation          | No           | Yes");
            Console.WriteLine("FedRAMP authorized         | No           | N/A (on-prem)");
        }

        public void CompareSecurity()
        {
            Console.WriteLine("=== SECURITY COMPARISON ===\n");

            Console.WriteLine("GOOGLE CLOUD VISION:");
            Console.WriteLine("  [!] NOT FedRAMP authorized");
            Console.WriteLine("  [!] Documents sent to Google Cloud");
            Console.WriteLine("  [!] PDFs stored in GCS during processing");
            Console.WriteLine("  [!] Subject to Google data policies");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  [✓] On-premise (no cloud = no FedRAMP needed)");
            Console.WriteLine("  [✓] Documents never leave your infrastructure");
            Console.WriteLine("  [✓] No external storage");
            Console.WriteLine("  [✓] You control all data");
        }
    }
}
