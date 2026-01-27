// =============================================================================
// Google Cloud Vision to IronOCR Migration Examples
// =============================================================================
// This file demonstrates complete migration patterns from Google Cloud Vision
// to IronOCR, covering all common scenarios including credential handling,
// text detection, PDF processing, batch operations, and error handling.
//
// NuGet packages to remove:
//   dotnet remove package Google.Cloud.Vision.V1
//   dotnet remove package Google.Cloud.Storage.V1
//
// NuGet packages to add:
//   dotnet add package IronOcr
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// MIGRATION SCENARIO 1: Service Account Setup Removal
// =============================================================================
// Google Cloud Vision requires complex credential setup with JSON key files
// and environment variables. IronOCR uses a simple license key.

namespace GoogleVisionBefore
{
    using Google.Cloud.Vision.V1;

    public class GoogleVisionCredentialSetup
    {
        private readonly ImageAnnotatorClient _client;

        public GoogleVisionCredentialSetup()
        {
            // BEFORE: Requires GOOGLE_APPLICATION_CREDENTIALS environment variable
            // pointing to a service account JSON key file:
            // export GOOGLE_APPLICATION_CREDENTIALS="/path/to/service-account.json"
            //
            // The JSON file contains sensitive data:
            // - private_key: RSA private key
            // - client_email: service account email
            // - project_id: GCP project identifier
            //
            // Security concerns:
            // - Key file must never be committed to source control
            // - Key file must be rotated periodically
            // - Key file must be protected with file system permissions
            // - Key file compromise grants API access until revoked

            _client = ImageAnnotatorClient.Create();
        }
    }
}

namespace IronOcrAfter
{
    using IronOcr;

    public class IronOcrCredentialSetup
    {
        private readonly IronTesseract _ocr;

        public IronOcrCredentialSetup()
        {
            // AFTER: Simple license key - no key files, no environment variables
            // Can be set from environment variable for production:
            IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE")
                ?? "YOUR-LICENSE-KEY";

            // No service accounts, no key rotation, no IAM configuration
            _ocr = new IronTesseract();
        }
    }
}

// MIGRATION SCENARIO 2: Basic TEXT_DETECTION
// =============================================================================
// Simple text extraction from images - the most common use case.

namespace GoogleVisionBefore
{
    using Google.Cloud.Vision.V1;

    public class BasicTextDetection
    {
        private readonly ImageAnnotatorClient _client;

        public BasicTextDetection()
        {
            // Requires GOOGLE_APPLICATION_CREDENTIALS
            _client = ImageAnnotatorClient.Create();
        }

        public string ExtractText(string imagePath)
        {
            // Load image for Google Cloud
            var image = Image.FromFile(imagePath);

            // Call the API - image is uploaded to Google
            var response = _client.DetectText(image);

            // First annotation contains full text
            if (response != null && response.Count > 0)
            {
                return response[0].Description;
            }

            return string.Empty;
        }

        public List<TextBlock> ExtractTextWithBounds(string imagePath)
        {
            var image = Image.FromFile(imagePath);
            var response = _client.DetectText(image);

            var blocks = new List<TextBlock>();

            // Skip first annotation (full text), iterate individual words
            foreach (var annotation in response.Skip(1))
            {
                var vertices = annotation.BoundingPoly.Vertices;
                blocks.Add(new TextBlock
                {
                    Text = annotation.Description,
                    X = vertices[0].X,
                    Y = vertices[0].Y,
                    Width = vertices[1].X - vertices[0].X,
                    Height = vertices[2].Y - vertices[0].Y
                });
            }

            return blocks;
        }
    }

    public class TextBlock
    {
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}

namespace IronOcrAfter
{
    using IronOcr;

    public class BasicTextDetection
    {
        private readonly IronTesseract _ocr;

        public BasicTextDetection()
        {
            // No credentials required
            _ocr = new IronTesseract();
        }

        public string ExtractText(string imagePath)
        {
            // All processing happens locally - no cloud transmission
            return _ocr.Read(imagePath).Text;
        }

        public List<TextBlock> ExtractTextWithBounds(string imagePath)
        {
            var result = _ocr.Read(imagePath);

            // Direct access to word-level results with confidence
            return result.Words.Select(w => new TextBlock
            {
                Text = w.Text,
                X = w.X,
                Y = w.Y,
                Width = w.Width,
                Height = w.Height,
                Confidence = w.Confidence
            }).ToList();
        }
    }

    public class TextBlock
    {
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double Confidence { get; set; }
    }
}

// MIGRATION SCENARIO 3: DOCUMENT_TEXT_DETECTION (Dense Documents)
// =============================================================================
// Processing scanned documents with hierarchical structure extraction.

namespace GoogleVisionBefore
{
    using Google.Cloud.Vision.V1;

    public class DocumentTextDetection
    {
        private readonly ImageAnnotatorClient _client;

        public DocumentTextDetection()
        {
            _client = ImageAnnotatorClient.Create();
        }

        public DocumentStructure ExtractDocumentStructure(string imagePath)
        {
            var image = Image.FromFile(imagePath);

            // Use DetectDocumentText for dense documents
            var annotation = _client.DetectDocumentText(image);

            var structure = new DocumentStructure
            {
                FullText = annotation.Text,
                Pages = new List<PageInfo>()
            };

            // Navigate complex hierarchy: Pages -> Blocks -> Paragraphs -> Words -> Symbols
            foreach (var page in annotation.Pages)
            {
                var pageInfo = new PageInfo
                {
                    Confidence = page.Confidence,
                    Paragraphs = new List<ParagraphInfo>()
                };

                foreach (var block in page.Blocks)
                {
                    foreach (var paragraph in block.Paragraphs)
                    {
                        // Must concatenate symbols to get paragraph text
                        var text = string.Join("", paragraph.Words
                            .SelectMany(w => w.Symbols)
                            .Select(s => s.Text));

                        pageInfo.Paragraphs.Add(new ParagraphInfo
                        {
                            Text = text,
                            Confidence = paragraph.Confidence
                        });
                    }
                }

                structure.Pages.Add(pageInfo);
            }

            return structure;
        }
    }

    public class DocumentStructure
    {
        public string FullText { get; set; }
        public List<PageInfo> Pages { get; set; }
    }

    public class PageInfo
    {
        public float Confidence { get; set; }
        public List<ParagraphInfo> Paragraphs { get; set; }
    }

    public class ParagraphInfo
    {
        public string Text { get; set; }
        public float Confidence { get; set; }
    }
}

namespace IronOcrAfter
{
    using IronOcr;

    public class DocumentTextDetection
    {
        private readonly IronTesseract _ocr;

        public DocumentTextDetection()
        {
            _ocr = new IronTesseract();
        }

        public DocumentStructure ExtractDocumentStructure(string imagePath)
        {
            var result = _ocr.Read(imagePath);

            // Simpler access to document structure
            return new DocumentStructure
            {
                FullText = result.Text,
                Confidence = result.Confidence,
                Paragraphs = result.Paragraphs.Select(p => new ParagraphInfo
                {
                    Text = p.Text,
                    Confidence = p.Confidence
                }).ToList(),
                Lines = result.Lines.Select(l => new LineInfo
                {
                    Text = l.Text,
                    X = l.X,
                    Y = l.Y
                }).ToList()
            };
        }
    }

    public class DocumentStructure
    {
        public string FullText { get; set; }
        public double Confidence { get; set; }
        public List<ParagraphInfo> Paragraphs { get; set; }
        public List<LineInfo> Lines { get; set; }
    }

    public class ParagraphInfo
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
    }

    public class LineInfo
    {
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}

// MIGRATION SCENARIO 4: PDF Processing
// =============================================================================
// Google Cloud Vision requires GCS upload + async API for PDF processing.
// IronOCR handles PDFs directly with synchronous processing.

namespace GoogleVisionBefore
{
    using Google.Cloud.Vision.V1;
    using Google.Cloud.Storage.V1;

    public class PdfProcessing
    {
        private readonly ImageAnnotatorClient _visionClient;
        private readonly string _bucketName;

        public PdfProcessing(string bucketName)
        {
            _visionClient = ImageAnnotatorClient.Create();
            _bucketName = bucketName;
        }

        public async Task<string> ProcessPdfAsync(string pdfPath)
        {
            // Step 1: Create storage client
            var storageClient = StorageClient.Create();
            var objectName = $"ocr-input/{Guid.NewGuid()}.pdf";

            // Step 2: Upload PDF to GCS (document leaves your infrastructure)
            using (var stream = File.OpenRead(pdfPath))
            {
                await storageClient.UploadObjectAsync(
                    _bucketName,
                    objectName,
                    "application/pdf",
                    stream);
            }

            // Step 3: Build async annotation request
            var asyncRequest = new AsyncAnnotateFileRequest
            {
                InputConfig = new InputConfig
                {
                    GcsSource = new GcsSource
                    {
                        Uri = $"gs://{_bucketName}/{objectName}"
                    },
                    MimeType = "application/pdf"
                },
                Features = { new Feature { Type = Feature.Types.Type.DocumentTextDetection } },
                OutputConfig = new OutputConfig
                {
                    GcsDestination = new GcsDestination
                    {
                        Uri = $"gs://{_bucketName}/ocr-output/"
                    },
                    BatchSize = 1
                }
            };

            // Step 4: Submit and wait for async operation
            var operation = await _visionClient.AsyncBatchAnnotateFilesAsync(
                new[] { asyncRequest });

            var completedOperation = await operation.PollUntilCompletedAsync();

            // Step 5: Download and parse results from GCS output
            var outputUri = completedOperation.Result.Responses[0]
                .OutputConfig.GcsDestination.Uri;
            var text = await DownloadAndParseResultsAsync(storageClient, outputUri);

            // Step 6: Clean up input file from GCS
            await storageClient.DeleteObjectAsync(_bucketName, objectName);

            // Note: Output files also need cleanup

            return text;
        }

        private async Task<string> DownloadAndParseResultsAsync(
            StorageClient client,
            string outputUri)
        {
            // Parse GCS URI and list output objects
            // Download JSON files and extract text
            // This adds significant complexity
            throw new NotImplementedException("Complex JSON parsing required");
        }
    }
}

namespace IronOcrAfter
{
    using IronOcr;

    public class PdfProcessing
    {
        private readonly IronTesseract _ocr;

        public PdfProcessing()
        {
            _ocr = new IronTesseract();
        }

        public string ProcessPdf(string pdfPath)
        {
            // Direct PDF processing - no GCS, no async, no cleanup
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);

            return _ocr.Read(input).Text;
        }

        public string ProcessPdfPages(string pdfPath, int startPage, int endPage)
        {
            // Process specific page range
            using var input = new OcrInput();
            input.LoadPdfPages(pdfPath, startPage, endPage);

            return _ocr.Read(input).Text;
        }

        public string ProcessEncryptedPdf(string pdfPath, string password)
        {
            // Password-protected PDFs - not possible with Google Cloud Vision
            using var input = new OcrInput();
            input.LoadPdf(pdfPath, Password: password);

            return _ocr.Read(input).Text;
        }

        public OcrResult ProcessPdfWithDetails(string pdfPath)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);

            var result = _ocr.Read(input);

            // Access per-page results
            foreach (var page in result.Pages)
            {
                Console.WriteLine($"Page {page.PageNumber}: {page.Text.Substring(0, Math.Min(100, page.Text.Length))}...");
                Console.WriteLine($"  Confidence: {page.Confidence:P1}");
            }

            return result;
        }
    }
}

// MIGRATION SCENARIO 5: Batch Processing
// =============================================================================
// Google Cloud Vision has rate limits. IronOCR has no rate limits (local).

namespace GoogleVisionBefore
{
    using Google.Cloud.Vision.V1;

    public class BatchProcessing
    {
        private readonly ImageAnnotatorClient _client;

        public BatchProcessing()
        {
            _client = ImageAnnotatorClient.Create();
        }

        public async Task<Dictionary<string, string>> ProcessBatchAsync(
            string[] imagePaths,
            IProgress<int> progress)
        {
            var results = new Dictionary<string, string>();
            int processed = 0;

            // Rate limit consideration: 1800 requests/minute default
            // May need to implement throttling for large batches

            foreach (var path in imagePaths)
            {
                try
                {
                    var image = Image.FromFile(path);
                    var response = _client.DetectText(image);

                    results[path] = response.FirstOrDefault()?.Description ?? "";
                }
                catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // Handle rate limiting
                    await Task.Delay(60000); // Wait 1 minute

                    // Retry
                    var image = Image.FromFile(path);
                    var response = _client.DetectText(image);
                    results[path] = response.FirstOrDefault()?.Description ?? "";
                }

                progress.Report(++processed * 100 / imagePaths.Length);
            }

            return results;
        }
    }
}

namespace IronOcrAfter
{
    using IronOcr;
    using System.Collections.Concurrent;

    public class BatchProcessing
    {
        private readonly IronTesseract _ocr;

        public BatchProcessing()
        {
            _ocr = new IronTesseract();
        }

        public Dictionary<string, string> ProcessBatch(
            string[] imagePaths,
            IProgress<int> progress)
        {
            var results = new ConcurrentDictionary<string, string>();
            int processed = 0;

            // No rate limits - process as fast as CPU allows
            Parallel.ForEach(imagePaths, imagePath =>
            {
                var result = _ocr.Read(imagePath);
                results[imagePath] = result.Text;

                progress.Report(Interlocked.Increment(ref processed) * 100 / imagePaths.Length);
            });

            return results.ToDictionary(x => x.Key, x => x.Value);
        }

        public OcrResult ProcessBatchAsDocument(string[] imagePaths)
        {
            // Combine multiple images into single document
            using var input = new OcrInput();
            foreach (var path in imagePaths)
            {
                input.LoadImage(path);
            }

            // Single OCR call for all images
            return _ocr.Read(input);
        }
    }
}

// MIGRATION SCENARIO 6: Error Handling
// =============================================================================
// Different exception patterns between the libraries.

namespace GoogleVisionBefore
{
    using Google.Cloud.Vision.V1;
    using Grpc.Core;

    public class ErrorHandling
    {
        private readonly ImageAnnotatorClient _client;

        public ErrorHandling()
        {
            _client = ImageAnnotatorClient.Create();
        }

        public string SafeExtractText(string imagePath)
        {
            try
            {
                var image = Image.FromFile(imagePath);
                var response = _client.DetectText(image);
                return response.FirstOrDefault()?.Description ?? "";
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
            {
                // Credential or permission issue
                throw new InvalidOperationException(
                    "Google Cloud Vision permission denied. Check service account permissions.", ex);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.ResourceExhausted)
            {
                // Rate limit exceeded
                throw new InvalidOperationException(
                    "Google Cloud Vision rate limit exceeded. Implement retry logic.", ex);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
            {
                // Service unavailable
                throw new InvalidOperationException(
                    "Google Cloud Vision service unavailable. Try again later.", ex);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                // Timeout - often network or large image
                throw new InvalidOperationException(
                    "Google Cloud Vision request timed out. Check network or image size.", ex);
            }
            catch (Google.GoogleApiException ex)
            {
                // General API exception
                throw new InvalidOperationException(
                    $"Google Cloud Vision API error: {ex.Message}", ex);
            }
        }
    }
}

namespace IronOcrAfter
{
    using IronOcr;

    public class ErrorHandling
    {
        private readonly IronTesseract _ocr;

        public ErrorHandling()
        {
            _ocr = new IronTesseract();
        }

        public string SafeExtractText(string imagePath)
        {
            try
            {
                var result = _ocr.Read(imagePath);

                // Check confidence for potential issues
                if (result.Confidence < 50)
                {
                    Console.WriteLine($"Warning: Low confidence ({result.Confidence:F1}%). " +
                        "Consider preprocessing the image.");
                }

                return result.Text;
            }
            catch (IOException ex)
            {
                // File access issue
                throw new InvalidOperationException(
                    $"Cannot read image file: {imagePath}", ex);
            }
            catch (IronOcr.Exceptions.OcrException ex)
            {
                // OCR-specific exception
                throw new InvalidOperationException(
                    $"OCR processing failed: {ex.Message}", ex);
            }

            // No network exceptions - all processing is local
            // No rate limit exceptions - unlimited local processing
            // No credential exceptions - simple license key
        }

        public string SafeExtractWithPreprocessing(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);

            // Apply preprocessing to improve problematic images
            input.Deskew();
            input.DeNoise();
            input.EnhanceResolution(300);

            var result = _ocr.Read(input);

            if (result.Confidence < 70)
            {
                // Try additional enhancement
                input.Binarize();
                result = _ocr.Read(input);
            }

            return result.Text;
        }
    }
}

// MIGRATION SCENARIO 7: Handwriting Recognition
// =============================================================================
// Both support handwriting, but with different feature names.

namespace GoogleVisionBefore
{
    using Google.Cloud.Vision.V1;

    public class HandwritingRecognition
    {
        private readonly ImageAnnotatorClient _client;

        public HandwritingRecognition()
        {
            _client = ImageAnnotatorClient.Create();
        }

        public string RecognizeHandwriting(string imagePath)
        {
            var image = Image.FromFile(imagePath);

            // DOCUMENT_TEXT_DETECTION handles handwriting
            // No separate handwriting mode needed
            var annotation = _client.DetectDocumentText(image);

            return annotation?.Text ?? "";
        }

        public List<WordWithConfidence> RecognizeWithConfidence(string imagePath)
        {
            var image = Image.FromFile(imagePath);
            var annotation = _client.DetectDocumentText(image);

            var words = new List<WordWithConfidence>();

            foreach (var page in annotation.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var paragraph in block.Paragraphs)
                    {
                        foreach (var word in paragraph.Words)
                        {
                            var text = string.Join("", word.Symbols.Select(s => s.Text));
                            words.Add(new WordWithConfidence
                            {
                                Text = text,
                                Confidence = word.Confidence
                            });
                        }
                    }
                }
            }

            return words;
        }
    }

    public class WordWithConfidence
    {
        public string Text { get; set; }
        public float Confidence { get; set; }
    }
}

namespace IronOcrAfter
{
    using IronOcr;

    public class HandwritingRecognition
    {
        private readonly IronTesseract _ocr;

        public HandwritingRecognition()
        {
            _ocr = new IronTesseract();
        }

        public string RecognizeHandwriting(string imagePath)
        {
            // IronOCR handles handwriting automatically
            return _ocr.Read(imagePath).Text;
        }

        public List<WordWithConfidence> RecognizeWithConfidence(string imagePath)
        {
            var result = _ocr.Read(imagePath);

            // Direct access to word-level confidence
            return result.Words.Select(w => new WordWithConfidence
            {
                Text = w.Text,
                Confidence = w.Confidence
            }).ToList();
        }

        public string RecognizeWithEnhancement(string imagePath)
        {
            // Preprocessing helps with handwriting
            using var input = new OcrInput();
            input.LoadImage(imagePath);

            // Handwriting benefits from these filters
            input.EnhanceResolution(300);
            input.DeNoise();
            input.Contrast();

            return _ocr.Read(input).Text;
        }
    }

    public class WordWithConfidence
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
    }
}

// =============================================================================
// MIGRATION SUMMARY
// =============================================================================
//
// Key differences when migrating from Google Cloud Vision to IronOCR:
//
// 1. CREDENTIALS
//    Before: Service account JSON + GOOGLE_APPLICATION_CREDENTIALS
//    After:  Simple license key string
//
// 2. DATA SOVEREIGNTY
//    Before: All documents sent to Google Cloud
//    After:  All processing happens locally
//
// 3. PDF PROCESSING
//    Before: Upload to GCS -> Async API -> Download results -> Cleanup
//    After:  input.LoadPdf(path) -> ocr.Read(input)
//
// 4. PASSWORD PDFs
//    Before: Not supported (need separate library)
//    After:  input.LoadPdf(path, Password: "secret")
//
// 5. RATE LIMITS
//    Before: 1800 requests/minute (default quota)
//    After:  Unlimited (local CPU is the limit)
//
// 6. ERROR HANDLING
//    Before: Network, auth, rate limit, timeout exceptions
//    After:  File IO and OCR exceptions only
//
// 7. DEPENDENCIES
//    Before: Google.Cloud.Vision.V1 + Google.Cloud.Storage.V1
//    After:  IronOcr
//
// 8. COMPLEXITY
//    Before: GCP project, API enablement, IAM, service accounts, key rotation
//    After:  Install NuGet package, set license key
// =============================================================================
