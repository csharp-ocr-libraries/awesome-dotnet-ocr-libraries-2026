/**
 * AWS Textract Async Processing vs IronOCR Direct Processing
 *
 * This file specifically addresses one of Textract's major pain points:
 * the async API requirement for multi-page documents (PDFs > 1 page).
 *
 * AWS Textract Async Workflow:
 * 1. Upload document to S3 bucket
 * 2. Call StartDocumentTextDetection with S3 location
 * 3. Receive JobId for tracking
 * 4. Poll GetDocumentTextDetection until status changes from IN_PROGRESS
 * 5. Handle pagination for large result sets
 * 6. Clean up S3 object
 *
 * IronOCR Direct Workflow:
 * 1. Call Read() with file path
 * 2. Get results
 *
 * NuGet Packages:
 * - AWS: AWSSDK.Textract, AWSSDK.S3
 * - IronOCR: IronOcr
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ============================================================================
// AWS TEXTRACT: Full Async Processing Pipeline
// This demonstrates the complete complexity required for PDF processing
// ============================================================================

namespace AwsTextractAsync
{
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.Textract;
    using Amazon.Textract.Model;

    /// <summary>
    /// Complete AWS Textract async document processor.
    /// Demonstrates the full complexity required for multi-page documents.
    /// </summary>
    public class TextractAsyncProcessor
    {
        private readonly AmazonTextractClient _textractClient;
        private readonly AmazonS3Client _s3Client;
        private readonly string _bucketName;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _maxWaitTime = TimeSpan.FromMinutes(10);

        public TextractAsyncProcessor(string bucketName)
        {
            _bucketName = bucketName;
            _textractClient = new AmazonTextractClient(Amazon.RegionEndpoint.USEast1);
            _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
        }

        /// <summary>
        /// Process a PDF document through the full async pipeline.
        /// This is the minimum code required for reliable PDF processing.
        /// </summary>
        public async Task<DocumentResult> ProcessDocumentAsync(
            string localFilePath,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var s3Key = $"textract-uploads/{Guid.NewGuid()}{Path.GetExtension(localFilePath)}";

            try
            {
                // Phase 1: Upload to S3
                Console.WriteLine($"[Textract] Uploading to S3: {s3Key}");
                await UploadToS3Async(localFilePath, s3Key, cancellationToken);
                var uploadTime = stopwatch.Elapsed;
                Console.WriteLine($"[Textract] Upload complete: {uploadTime.TotalSeconds:F1}s");

                // Phase 2: Start Textract job
                Console.WriteLine("[Textract] Starting async job...");
                var jobId = await StartTextractJobAsync(s3Key, cancellationToken);
                Console.WriteLine($"[Textract] Job started: {jobId}");

                // Phase 3: Poll for completion
                Console.WriteLine("[Textract] Polling for completion...");
                var pollResult = await PollForCompletionAsync(jobId, cancellationToken);
                var processingTime = stopwatch.Elapsed - uploadTime;
                Console.WriteLine($"[Textract] Processing complete: {processingTime.TotalSeconds:F1}s");

                if (!pollResult.Success)
                {
                    throw new Exception($"Textract job failed: {pollResult.ErrorMessage}");
                }

                // Phase 4: Retrieve results (may be paginated)
                Console.WriteLine("[Textract] Retrieving results...");
                var text = await GetAllResultsAsync(jobId, cancellationToken);
                var totalTime = stopwatch.Elapsed;

                return new DocumentResult
                {
                    Text = text,
                    UploadTimeMs = (long)uploadTime.TotalMilliseconds,
                    ProcessingTimeMs = (long)processingTime.TotalMilliseconds,
                    TotalTimeMs = (long)totalTime.TotalMilliseconds,
                    Success = true
                };
            }
            finally
            {
                // Phase 5: Clean up S3 (always, even on failure)
                try
                {
                    await DeleteFromS3Async(s3Key, cancellationToken);
                    Console.WriteLine("[Textract] S3 cleanup complete");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Textract] S3 cleanup failed: {ex.Message}");
                }
            }
        }

        private async Task UploadToS3Async(
            string localPath,
            string s3Key,
            CancellationToken cancellationToken)
        {
            using var fileStream = File.OpenRead(localPath);

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key,
                InputStream = fileStream,
                ContentType = GetContentType(localPath)
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);
        }

        private async Task<string> StartTextractJobAsync(
            string s3Key,
            CancellationToken cancellationToken)
        {
            var request = new StartDocumentTextDetectionRequest
            {
                DocumentLocation = new DocumentLocation
                {
                    S3Object = new S3Object
                    {
                        Bucket = _bucketName,
                        Name = s3Key
                    }
                }
            };

            var response = await _textractClient.StartDocumentTextDetectionAsync(
                request, cancellationToken);

            return response.JobId;
        }

        private async Task<(bool Success, string ErrorMessage)> PollForCompletionAsync(
            string jobId,
            CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            int pollCount = 0;

            while (DateTime.UtcNow - startTime < _maxWaitTime)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = await _textractClient.GetDocumentTextDetectionAsync(
                    new GetDocumentTextDetectionRequest { JobId = jobId },
                    cancellationToken);

                pollCount++;
                Console.WriteLine($"[Textract] Poll #{pollCount}: {response.JobStatus}");

                switch (response.JobStatus)
                {
                    case JobStatus.SUCCEEDED:
                        return (true, null);

                    case JobStatus.FAILED:
                        return (false, response.StatusMessage ?? "Unknown error");

                    case JobStatus.IN_PROGRESS:
                        await Task.Delay(_pollInterval, cancellationToken);
                        break;

                    default:
                        throw new Exception($"Unknown job status: {response.JobStatus}");
                }
            }

            return (false, "Job timed out");
        }

        private async Task<string> GetAllResultsAsync(
            string jobId,
            CancellationToken cancellationToken)
        {
            var allText = new StringBuilder();
            string nextToken = null;
            int pageCount = 0;

            do
            {
                var request = new GetDocumentTextDetectionRequest
                {
                    JobId = jobId,
                    NextToken = nextToken
                };

                var response = await _textractClient.GetDocumentTextDetectionAsync(
                    request, cancellationToken);

                pageCount++;
                Console.WriteLine($"[Textract] Retrieved result page {pageCount}");

                foreach (var block in response.Blocks)
                {
                    if (block.BlockType == BlockType.LINE)
                    {
                        allText.AppendLine(block.Text);
                    }
                }

                nextToken = response.NextToken;

            } while (nextToken != null);

            return allText.ToString();
        }

        private async Task DeleteFromS3Async(
            string s3Key,
            CancellationToken cancellationToken)
        {
            await _s3Client.DeleteObjectAsync(
                _bucketName, s3Key, cancellationToken);
        }

        private string GetContentType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".tiff" or ".tif" => "image/tiff",
                _ => "application/octet-stream"
            };
        }
    }

    /// <summary>
    /// Rate-limited batch processor for AWS Textract.
    /// Required because Textract has TPS limits.
    /// </summary>
    public class TextractRateLimitedBatchProcessor
    {
        private readonly TextractAsyncProcessor _processor;
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly SemaphoreSlim _rateLimiter;
        private readonly int _requestsPerSecond;

        public TextractRateLimitedBatchProcessor(
            string bucketName,
            int maxConcurrent = 5,
            int requestsPerSecond = 5)
        {
            _processor = new TextractAsyncProcessor(bucketName);
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrent);
            _rateLimiter = new SemaphoreSlim(requestsPerSecond);
            _requestsPerSecond = requestsPerSecond;

            // Replenish rate limit tokens
            _ = ReplenishRateLimitAsync();
        }

        private async Task ReplenishRateLimitAsync()
        {
            while (true)
            {
                await Task.Delay(1000);
                int toRelease = _requestsPerSecond - _rateLimiter.CurrentCount;
                if (toRelease > 0)
                {
                    _rateLimiter.Release(toRelease);
                }
            }
        }

        public async Task<Dictionary<string, DocumentResult>> ProcessBatchAsync(
            string[] filePaths,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, DocumentResult>();
            var tasks = new List<Task>();
            int completed = 0;

            foreach (var path in filePaths)
            {
                // Wait for rate limit slot
                await _rateLimiter.WaitAsync(cancellationToken);

                // Wait for concurrency slot
                await _concurrencyLimiter.WaitAsync(cancellationToken);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var result = await _processor.ProcessDocumentAsync(
                            path, cancellationToken);

                        lock (results)
                        {
                            results[path] = result;
                        }

                        var current = Interlocked.Increment(ref completed);
                        progress?.Report(current * 100 / filePaths.Length);
                    }
                    catch (Exception ex)
                    {
                        lock (results)
                        {
                            results[path] = new DocumentResult
                            {
                                Success = false,
                                ErrorMessage = ex.Message
                            };
                        }
                    }
                    finally
                    {
                        _concurrencyLimiter.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            return results;
        }
    }

    public class DocumentResult
    {
        public string Text { get; set; }
        public long UploadTimeMs { get; set; }
        public long ProcessingTimeMs { get; set; }
        public long TotalTimeMs { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}


// ============================================================================
// IRONOCR: Direct Processing (No Async, No S3, No Polling)
// ============================================================================

namespace IronOcrDirect
{
    using IronOcr;
    using System.Collections.Concurrent;

    /// <summary>
    /// IronOCR document processor.
    /// Demonstrates the simplicity of local processing.
    /// </summary>
    public class IronOcrProcessor
    {
        private readonly IronTesseract _ocr;

        public IronOcrProcessor()
        {
            _ocr = new IronTesseract();
        }

        /// <summary>
        /// Process a document directly. No upload, no polling, no cleanup.
        /// </summary>
        public DocumentResult ProcessDocument(string filePath)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                Console.WriteLine($"[IronOCR] Processing: {Path.GetFileName(filePath)}");

                using var input = new OcrInput();

                // Load based on file type
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".pdf")
                {
                    input.LoadPdf(filePath);
                }
                else
                {
                    input.LoadImage(filePath);
                }

                var result = _ocr.Read(input);

                Console.WriteLine($"[IronOCR] Complete: {stopwatch.ElapsedMilliseconds}ms");

                return new DocumentResult
                {
                    Text = result.Text,
                    Confidence = result.Confidence,
                    PageCount = result.Pages?.Length ?? 1,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new DocumentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Process with preprocessing for difficult documents.
        /// </summary>
        public DocumentResult ProcessWithEnhancement(string filePath)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var input = new OcrInput();

                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".pdf")
                {
                    input.LoadPdf(filePath);
                }
                else
                {
                    input.LoadImage(filePath);
                }

                // Apply preprocessing
                input.Deskew();
                input.DeNoise();
                input.EnhanceResolution(300);

                var result = _ocr.Read(input);

                return new DocumentResult
                {
                    Text = result.Text,
                    Confidence = result.Confidence,
                    PageCount = result.Pages?.Length ?? 1,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new DocumentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Process password-protected PDF.
        /// Not possible with Textract without external decryption.
        /// </summary>
        public DocumentResult ProcessEncryptedPdf(string filePath, string password)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var input = new OcrInput();
                input.LoadPdf(filePath, Password: password);

                var result = _ocr.Read(input);

                return new DocumentResult
                {
                    Text = result.Text,
                    Confidence = result.Confidence,
                    PageCount = result.Pages?.Length ?? 1,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new DocumentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }
    }

    /// <summary>
    /// Batch processor for IronOCR.
    /// No rate limiting required - process as fast as hardware allows.
    /// </summary>
    public class IronOcrBatchProcessor
    {
        public Dictionary<string, DocumentResult> ProcessBatch(
            string[] filePaths,
            IProgress<int> progress = null)
        {
            var ocr = new IronTesseract();
            var results = new ConcurrentDictionary<string, DocumentResult>();
            int completed = 0;

            // Process in parallel - no rate limits to worry about
            Parallel.ForEach(filePaths, path =>
            {
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    using var input = new OcrInput();

                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    if (ext == ".pdf")
                    {
                        input.LoadPdf(path);
                    }
                    else
                    {
                        input.LoadImage(path);
                    }

                    var result = ocr.Read(input);

                    results[path] = new DocumentResult
                    {
                        Text = result.Text,
                        Confidence = result.Confidence,
                        PageCount = result.Pages?.Length ?? 1,
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                        Success = true
                    };
                }
                catch (Exception ex)
                {
                    results[path] = new DocumentResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message,
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                var current = Interlocked.Increment(ref completed);
                progress?.Report(current * 100 / filePaths.Length);
            });

            return results.ToDictionary(x => x.Key, x => x.Value);
        }
    }

    public class DocumentResult
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
        public int PageCount { get; set; }
        public long ProcessingTimeMs { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}


// ============================================================================
// COMPARISON: Processing 100 Documents
// ============================================================================

namespace Comparison
{
    /// <summary>
    /// Side-by-side comparison showing complexity difference for batch processing.
    /// </summary>
    public class ProcessingComparison
    {
        public static void CompareApproaches()
        {
            Console.WriteLine("=== PROCESSING 100 DOCUMENTS ===\n");

            Console.WriteLine("AWS TEXTRACT WORKFLOW:");
            Console.WriteLine("  Prerequisites:");
            Console.WriteLine("    - AWS account with IAM user/role");
            Console.WriteLine("    - S3 bucket with appropriate permissions");
            Console.WriteLine("    - AWS credentials configured locally");
            Console.WriteLine("    - Internet connectivity");
            Console.WriteLine();
            Console.WriteLine("  Per Document:");
            Console.WriteLine("    1. Read file from disk (~5ms)");
            Console.WriteLine("    2. Upload to S3 (~500-2000ms depending on size/bandwidth)");
            Console.WriteLine("    3. Call StartDocumentTextDetection (~100ms)");
            Console.WriteLine("    4. Poll GetDocumentTextDetection every 5s until complete");
            Console.WriteLine("       - Typical: 3-10 polls for PDFs (~15-50s)");
            Console.WriteLine("    5. Handle result pagination if large (~100ms per page)");
            Console.WriteLine("    6. Delete S3 object (~100ms)");
            Console.WriteLine();
            Console.WriteLine("  Rate Limiting:");
            Console.WriteLine("    - StartDocumentTextDetection: 5 TPS default");
            Console.WriteLine("    - GetDocumentTextDetection: 5 TPS default");
            Console.WriteLine("    - Must implement throttling and retry logic");
            Console.WriteLine();
            Console.WriteLine("  100 Documents Estimate:");
            Console.WriteLine("    - Sequential: ~30-60 minutes");
            Console.WriteLine("    - Parallel (5 concurrent): ~6-12 minutes");
            Console.WriteLine("    - Requires careful rate limit management");
            Console.WriteLine();

            Console.WriteLine("IRONOCR WORKFLOW:");
            Console.WriteLine("  Prerequisites:");
            Console.WriteLine("    - Install NuGet package");
            Console.WriteLine("    - License key (optional for trial)");
            Console.WriteLine();
            Console.WriteLine("  Per Document:");
            Console.WriteLine("    1. Call ocr.Read(path) (~100-2000ms based on complexity)");
            Console.WriteLine("    2. Done");
            Console.WriteLine();
            Console.WriteLine("  Rate Limiting:");
            Console.WriteLine("    - None - limited only by local hardware");
            Console.WriteLine();
            Console.WriteLine("  100 Documents Estimate:");
            Console.WriteLine("    - Sequential: ~2-5 minutes");
            Console.WriteLine("    - Parallel (all cores): ~30-90 seconds");
            Console.WriteLine("    - No rate limiting considerations");
            Console.WriteLine();

            Console.WriteLine("CODE COMPARISON:");
            Console.WriteLine();
            Console.WriteLine("  AWS Textract: ~200 lines minimum for robust PDF processing");
            Console.WriteLine("  IronOCR: ~5 lines");
            Console.WriteLine();

            Console.WriteLine("INFRASTRUCTURE COMPARISON:");
            Console.WriteLine();
            Console.WriteLine("  AWS Textract requires:");
            Console.WriteLine("    - AWS account ($0+)");
            Console.WriteLine("    - IAM user/role setup (time)");
            Console.WriteLine("    - S3 bucket (storage costs)");
            Console.WriteLine("    - Credential management (security overhead)");
            Console.WriteLine("    - Internet connectivity (always)");
            Console.WriteLine("    - Per-page fees ($0.0015+ per page)");
            Console.WriteLine();
            Console.WriteLine("  IronOCR requires:");
            Console.WriteLine("    - License key (one-time cost)");
            Console.WriteLine("    - That's it");
        }

        public static void CompareCodeComplexity()
        {
            Console.WriteLine("=== CODE COMPLEXITY ===\n");

            Console.WriteLine("AWS TEXTRACT - Minimum viable PDF processor:");
            Console.WriteLine("  - S3 upload logic");
            Console.WriteLine("  - Textract job start");
            Console.WriteLine("  - Polling loop with timeout");
            Console.WriteLine("  - Result pagination handling");
            Console.WriteLine("  - S3 cleanup");
            Console.WriteLine("  - Error handling for:");
            Console.WriteLine("    * S3 upload failures");
            Console.WriteLine("    * Textract job failures");
            Console.WriteLine("    * Rate limiting (ProvisionedThroughputExceededException)");
            Console.WriteLine("    * Network timeouts");
            Console.WriteLine("    * Job timeouts");
            Console.WriteLine("  - Total: 150-300 lines");
            Console.WriteLine();

            Console.WriteLine("IRONOCR - Complete PDF processor:");
            Console.WriteLine("  var text = new IronTesseract().Read(\"document.pdf\").Text;");
            Console.WriteLine("  - Total: 1 line");
            Console.WriteLine();

            Console.WriteLine("IRONOCR - With preprocessing and error handling:");
            Console.WriteLine("  var ocr = new IronTesseract();");
            Console.WriteLine("  using var input = new OcrInput();");
            Console.WriteLine("  input.LoadPdf(path);");
            Console.WriteLine("  input.Deskew();");
            Console.WriteLine("  input.DeNoise();");
            Console.WriteLine("  var result = ocr.Read(input);");
            Console.WriteLine("  - Total: 6 lines");
        }
    }
}
