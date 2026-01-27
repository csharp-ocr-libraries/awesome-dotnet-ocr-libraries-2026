/**
 * AWS Textract vs IronOCR: Code Examples
 *
 * This file demonstrates OCR implementations with both AWS Textract
 * and IronOCR, highlighting complexity, data handling, and cost differences.
 *
 * KEY INSIGHT: AWS Textract sends documents to Amazon's cloud.
 *              IronOCR processes everything locally.
 *
 * NuGet Packages:
 * - AWSSDK.Textract (AWS)
 * - IronOcr (IronOCR)
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// AWS TEXTRACT IMPLEMENTATION
// Documents are uploaded to Amazon Web Services for processing
// ============================================================================

namespace AwsTextractExamples
{
    using Amazon.Textract;
    using Amazon.Textract.Model;

    /// <summary>
    /// AWS Textract requires:
    /// 1. AWS account
    /// 2. IAM user/role with Textract permissions
    /// 3. AWS credentials configuration
    /// 4. For large documents: S3 bucket
    /// </summary>
    public class TextractOcrService
    {
        private readonly AmazonTextractClient _client;

        public TextractOcrService()
        {
            // Requires AWS credentials in environment or config
            _client = new AmazonTextractClient(Amazon.RegionEndpoint.USEast1);
        }

        /// <summary>
        /// Basic text detection - synchronous API (small documents only)
        /// </summary>
        public async Task<string> DetectTextAsync(string imagePath)
        {
            // WARNING: Image data is sent to AWS
            var imageBytes = File.ReadAllBytes(imagePath);

            var request = new DetectDocumentTextRequest
            {
                Document = new Document
                {
                    Bytes = new MemoryStream(imageBytes)
                }
            };

            var response = await _client.DetectDocumentTextAsync(request);

            var text = new StringBuilder();
            foreach (var block in response.Blocks.Where(b => b.BlockType == BlockType.LINE))
            {
                text.AppendLine(block.Text);
            }

            return text.ToString();
        }

        /// <summary>
        /// Table and form analysis
        /// </summary>
        public async Task<AnalysisResult> AnalyzeDocumentAsync(string imagePath)
        {
            var imageBytes = File.ReadAllBytes(imagePath);

            var request = new AnalyzeDocumentRequest
            {
                Document = new Document
                {
                    Bytes = new MemoryStream(imageBytes)
                },
                FeatureTypes = new List<string> { "TABLES", "FORMS" }
            };

            var response = await _client.AnalyzeDocumentAsync(request);

            return new AnalysisResult
            {
                Text = ExtractText(response.Blocks),
                TableCount = response.Blocks.Count(b => b.BlockType == BlockType.TABLE),
                FormFieldCount = response.Blocks.Count(b => b.BlockType == BlockType.KEY_VALUE_SET)
            };
        }

        private string ExtractText(List<Block> blocks)
        {
            return string.Join("\n", blocks
                .Where(b => b.BlockType == BlockType.LINE)
                .Select(b => b.Text));
        }
    }

    /// <summary>
    /// AWS Textract async processing for large documents
    /// Requires S3 bucket for staging
    /// </summary>
    public class TextractAsyncService
    {
        private readonly AmazonTextractClient _client;

        /// <summary>
        /// Large document processing requires S3 staging
        /// </summary>
        public async Task<string> ProcessLargeDocumentAsync(string s3Bucket, string s3Key)
        {
            // Step 1: Start async job (document must be in S3)
            var startRequest = new StartDocumentTextDetectionRequest
            {
                DocumentLocation = new DocumentLocation
                {
                    S3Object = new S3Object
                    {
                        Bucket = s3Bucket,
                        Name = s3Key
                    }
                }
            };

            var startResponse = await _client.StartDocumentTextDetectionAsync(startRequest);
            var jobId = startResponse.JobId;

            // Step 2: Poll for completion (can take minutes)
            GetDocumentTextDetectionResponse getResponse;
            do
            {
                await Task.Delay(5000); // Poll every 5 seconds

                getResponse = await _client.GetDocumentTextDetectionAsync(
                    new GetDocumentTextDetectionRequest { JobId = jobId });

            } while (getResponse.JobStatus == JobStatus.IN_PROGRESS);

            // Step 3: Check for failure
            if (getResponse.JobStatus != JobStatus.SUCCEEDED)
            {
                throw new Exception($"Textract job failed: {getResponse.StatusMessage}");
            }

            // Step 4: Extract text from results
            var text = new StringBuilder();
            foreach (var block in getResponse.Blocks.Where(b => b.BlockType == BlockType.LINE))
            {
                text.AppendLine(block.Text);
            }

            return text.ToString();
        }
    }

    public class AnalysisResult
    {
        public string Text { get; set; }
        public int TableCount { get; set; }
        public int FormFieldCount { get; set; }
    }
}


// ============================================================================
// IRONOCR IMPLEMENTATION
// All processing happens locally - no S3, no async polling, no cloud
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR processes everything locally
    /// No AWS account, no S3, no async job management
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Basic text detection - synchronous, local
        /// </summary>
        public string DetectText(string imagePath)
        {
            // All processing on your machine
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// Document analysis with structure
        /// </summary>
        public AnalysisResult AnalyzeDocument(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);

            return new AnalysisResult
            {
                Text = result.Text,
                Lines = result.Lines.Select(l => new LineInfo
                {
                    Text = l.Text,
                    X = l.X,
                    Y = l.Y
                }).ToList(),
                Confidence = result.Confidence
            };
        }

        /// <summary>
        /// Large document processing - no S3, no async
        /// </summary>
        public string ProcessLargeDocument(string pdfPath)
        {
            // Direct processing - no staging, no polling
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);
            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Batch processing - simple parallel
        /// </summary>
        public Dictionary<string, string> ProcessBatch(string[] imagePaths)
        {
            var results = new Dictionary<string, string>();
            var ocr = new IronTesseract();

            foreach (var path in imagePaths)
            {
                results[path] = ocr.Read(path).Text;
            }

            return results;
        }
    }

    public class AnalysisResult
    {
        public string Text { get; set; }
        public List<LineInfo> Lines { get; set; }
        public double Confidence { get; set; }
    }

    public class LineInfo
    {
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}


// ============================================================================
// COMPARISON
// ============================================================================

namespace Comparison
{
    public class AwsVsIronOcrComparison
    {
        public void CompareComplexity()
        {
            Console.WriteLine("=== COMPLEXITY COMPARISON ===\n");

            Console.WriteLine("AWS TEXTRACT:");
            Console.WriteLine("  Setup: AWS account, IAM permissions, credentials");
            Console.WriteLine("  Small docs: DetectDocumentTextAsync (sync-ish)");
            Console.WriteLine("  Large docs: Upload to S3 → Start job → Poll → Download results");
            Console.WriteLine("  PDFs: Must use S3 + async API");
            Console.WriteLine("  Tables: AnalyzeDocumentAsync (additional complexity)");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  Setup: Install NuGet package");
            Console.WriteLine("  Any size: ocr.Read(path)");
            Console.WriteLine("  PDFs: ocr.Read(\"doc.pdf\")");
            Console.WriteLine("  Tables: Use line/word positioning");
        }

        public void ComparePricing()
        {
            Console.WriteLine("=== PRICING COMPARISON ===\n");

            Console.WriteLine("AWS TEXTRACT:");
            Console.WriteLine("  DetectDocumentText: $0.0015/page (first 1M)");
            Console.WriteLine("  AnalyzeDocument (tables): $0.015/page");
            Console.WriteLine("  AnalyzeDocument (forms): $0.05/page");
            Console.WriteLine("  + S3 storage costs for large documents");
            Console.WriteLine("  + Data transfer costs");
            Console.WriteLine();
            Console.WriteLine("  Example: 100,000 pages/month with tables");
            Console.WriteLine("  Cost: $1,500/month = $18,000/year");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  Professional: $2,999 one-time");
            Console.WriteLine("  No per-page fees, no storage costs");
            Console.WriteLine();
            Console.WriteLine("  Break-even: ~2 months of AWS usage");
        }

        public void CompareSecurity()
        {
            Console.WriteLine("=== SECURITY COMPARISON ===\n");

            Console.WriteLine("AWS TEXTRACT:");
            Console.WriteLine("  [!] Documents uploaded to AWS");
            Console.WriteLine("  [!] Stored in S3 for large docs");
            Console.WriteLine("  [!] Subject to AWS data policies");
            Console.WriteLine("  [!] Requires internet connectivity");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  [✓] All processing local");
            Console.WriteLine("  [✓] No cloud storage");
            Console.WriteLine("  [✓] You control data");
            Console.WriteLine("  [✓] Works offline");
        }
    }
}
