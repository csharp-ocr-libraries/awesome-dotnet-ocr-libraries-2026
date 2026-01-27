/**
 * AWS Textract to IronOCR Migration Examples
 *
 * This file demonstrates complete migration patterns from AWS Textract to IronOCR.
 * Each scenario shows the "Before" (Textract) and "After" (IronOCR) implementations.
 *
 * Key Migration Benefits:
 * - Remove AWS credential dependencies
 * - Eliminate S3 staging requirements
 * - Simplify async patterns to synchronous calls
 * - Reduce code complexity by 80-90%
 * - Process data locally (no cloud transmission)
 *
 * NuGet Changes:
 * - Remove: AWSSDK.Textract, AWSSDK.S3, AWSSDK.Core
 * - Add: IronOcr
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// SCENARIO 1: Basic Text Extraction
// ============================================================================

namespace Migration.BasicTextExtraction
{
    // BEFORE: AWS Textract
    namespace Before
    {
        using Amazon.Textract;
        using Amazon.Textract.Model;

        public class TextractBasicService
        {
            private readonly AmazonTextractClient _client;

            public TextractBasicService()
            {
                // Requires AWS credentials configured
                _client = new AmazonTextractClient(Amazon.RegionEndpoint.USEast1);
            }

            public async Task<string> ExtractTextAsync(string imagePath)
            {
                // Document data is sent to AWS cloud
                var imageBytes = File.ReadAllBytes(imagePath);

                var request = new DetectDocumentTextRequest
                {
                    Document = new Document
                    {
                        Bytes = new MemoryStream(imageBytes)
                    }
                };

                var response = await _client.DetectDocumentTextAsync(request);

                // Must filter blocks to get text lines
                return string.Join("\n", response.Blocks
                    .Where(b => b.BlockType == BlockType.LINE)
                    .Select(b => b.Text));
            }
        }
    }

    // AFTER: IronOCR
    namespace After
    {
        using IronOcr;

        public class IronOcrBasicService
        {
            public string ExtractText(string imagePath)
            {
                // All processing happens locally
                var ocr = new IronTesseract();
                return ocr.Read(imagePath).Text;
            }
        }
    }
}

// ============================================================================
// SCENARIO 2: Table Extraction
// ============================================================================

namespace Migration.TableExtraction
{
    // BEFORE: AWS Textract (Complex relationship parsing)
    namespace Before
    {
        using Amazon.Textract;
        using Amazon.Textract.Model;

        public class TextractTableService
        {
            private readonly AmazonTextractClient _client;

            public async Task<List<List<List<string>>>> ExtractTablesAsync(string imagePath)
            {
                var imageBytes = File.ReadAllBytes(imagePath);

                var request = new AnalyzeDocumentRequest
                {
                    Document = new Document
                    {
                        Bytes = new MemoryStream(imageBytes)
                    },
                    FeatureTypes = new List<string> { "TABLES" }
                };

                var response = await _client.AnalyzeDocumentAsync(request);

                // Complex table reconstruction from blocks
                var tables = new List<List<List<string>>>();
                var tableBlocks = response.Blocks.Where(b => b.BlockType == BlockType.TABLE);

                foreach (var tableBlock in tableBlocks)
                {
                    var table = new List<List<string>>();

                    // Find all cell blocks belonging to this table
                    var cellBlocks = response.Blocks
                        .Where(b => b.BlockType == BlockType.CELL)
                        .Where(b => tableBlock.Relationships?
                            .Any(r => r.Type == RelationshipType.CHILD && r.Ids.Contains(b.Id)) == true)
                        .ToList();

                    // Group cells by row index
                    var rows = cellBlocks
                        .GroupBy(c => c.RowIndex)
                        .OrderBy(g => g.Key);

                    foreach (var row in rows)
                    {
                        var rowData = new List<string>();
                        var orderedCells = row.OrderBy(c => c.ColumnIndex);

                        foreach (var cell in orderedCells)
                        {
                            // Get word blocks for cell content
                            var cellText = GetCellText(response.Blocks, cell);
                            rowData.Add(cellText);
                        }
                        table.Add(rowData);
                    }
                    tables.Add(table);
                }

                return tables;
            }

            private string GetCellText(List<Block> allBlocks, Block cell)
            {
                if (cell.Relationships == null) return "";

                var wordIds = cell.Relationships
                    .Where(r => r.Type == RelationshipType.CHILD)
                    .SelectMany(r => r.Ids);

                var words = allBlocks
                    .Where(b => b.BlockType == BlockType.WORD && wordIds.Contains(b.Id))
                    .Select(b => b.Text);

                return string.Join(" ", words);
            }
        }
    }

    // AFTER: IronOCR (Position-based reconstruction)
    namespace After
    {
        using IronOcr;

        public class IronOcrTableService
        {
            public List<List<List<string>>> ExtractTables(string imagePath)
            {
                var ocr = new IronTesseract();
                var result = ocr.Read(imagePath);

                var tables = new List<List<List<string>>>();

                // Use word positions to identify tables
                var words = result.Words.OrderBy(w => w.Y).ThenBy(w => w.X).ToList();

                // Simple table detection: group by Y position
                var rows = words
                    .GroupBy(w => w.Y / 20)  // 20px tolerance for row grouping
                    .Where(g => g.Count() > 2) // Multiple columns suggests table
                    .OrderBy(g => g.Key)
                    .ToList();

                if (rows.Count > 1)
                {
                    var table = new List<List<string>>();
                    foreach (var row in rows)
                    {
                        var rowData = row
                            .OrderBy(w => w.X)
                            .Select(w => w.Text)
                            .ToList();
                        table.Add(rowData);
                    }
                    tables.Add(table);
                }

                return tables;
            }
        }
    }
}

// ============================================================================
// SCENARIO 3: Form Field Extraction
// ============================================================================

namespace Migration.FormExtraction
{
    // BEFORE: AWS Textract
    namespace Before
    {
        using Amazon.Textract;
        using Amazon.Textract.Model;

        public class TextractFormService
        {
            private readonly AmazonTextractClient _client;

            public async Task<Dictionary<string, string>> ExtractFormFieldsAsync(string imagePath)
            {
                var imageBytes = File.ReadAllBytes(imagePath);

                var request = new AnalyzeDocumentRequest
                {
                    Document = new Document
                    {
                        Bytes = new MemoryStream(imageBytes)
                    },
                    FeatureTypes = new List<string> { "FORMS" }
                };

                var response = await _client.AnalyzeDocumentAsync(request);

                var formFields = new Dictionary<string, string>();

                // Find KEY_VALUE_SET blocks (complex relationship structure)
                var keyBlocks = response.Blocks
                    .Where(b => b.BlockType == BlockType.KEY_VALUE_SET &&
                               b.EntityTypes?.Contains("KEY") == true);

                foreach (var keyBlock in keyBlocks)
                {
                    // Get key text
                    var keyText = GetBlockText(response.Blocks, keyBlock);

                    // Find value block via relationship
                    var valueBlockId = keyBlock.Relationships?
                        .FirstOrDefault(r => r.Type == RelationshipType.VALUE)?
                        .Ids.FirstOrDefault();

                    if (valueBlockId != null)
                    {
                        var valueBlock = response.Blocks.FirstOrDefault(b => b.Id == valueBlockId);
                        if (valueBlock != null)
                        {
                            var valueText = GetBlockText(response.Blocks, valueBlock);
                            formFields[keyText] = valueText;
                        }
                    }
                }

                return formFields;
            }

            private string GetBlockText(List<Block> allBlocks, Block block)
            {
                if (block.Relationships == null) return "";

                var childIds = block.Relationships
                    .Where(r => r.Type == RelationshipType.CHILD)
                    .SelectMany(r => r.Ids);

                var words = allBlocks
                    .Where(b => b.BlockType == BlockType.WORD && childIds.Contains(b.Id))
                    .Select(b => b.Text);

                return string.Join(" ", words);
            }
        }
    }

    // AFTER: IronOCR (Zone-based extraction)
    namespace After
    {
        using IronOcr;

        public class IronOcrFormService
        {
            public Dictionary<string, string> ExtractFormFields(string imagePath, FormTemplate template)
            {
                var ocr = new IronTesseract();
                var formFields = new Dictionary<string, string>();

                foreach (var field in template.Fields)
                {
                    using var input = new OcrInput();

                    // Extract specific region for each field
                    input.LoadImage(imagePath, new CropRectangle(
                        field.X, field.Y, field.Width, field.Height));

                    var result = ocr.Read(input);
                    formFields[field.Name] = result.Text.Trim();
                }

                return formFields;
            }
        }

        // Define expected form regions
        public class FormTemplate
        {
            public List<FormField> Fields { get; set; } = new List<FormField>();
        }

        public class FormField
        {
            public string Name { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }
}

// ============================================================================
// SCENARIO 4: PDF Processing (Multi-Page)
// ============================================================================

namespace Migration.PdfProcessing
{
    // BEFORE: AWS Textract (Requires S3 + async)
    namespace Before
    {
        using Amazon.S3;
        using Amazon.S3.Model;
        using Amazon.Textract;
        using Amazon.Textract.Model;

        public class TextractPdfService
        {
            private readonly AmazonTextractClient _textractClient;
            private readonly AmazonS3Client _s3Client;
            private readonly string _bucketName = "my-textract-bucket";

            public TextractPdfService()
            {
                _textractClient = new AmazonTextractClient(Amazon.RegionEndpoint.USEast1);
                _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
            }

            public async Task<string> ProcessPdfAsync(string pdfPath)
            {
                // Step 1: Upload PDF to S3 (required for multi-page)
                var key = $"uploads/{Guid.NewGuid()}.pdf";

                using (var fileStream = File.OpenRead(pdfPath))
                {
                    await _s3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = key,
                        InputStream = fileStream
                    });
                }

                try
                {
                    // Step 2: Start async Textract job
                    var startResponse = await _textractClient.StartDocumentTextDetectionAsync(
                        new StartDocumentTextDetectionRequest
                        {
                            DocumentLocation = new DocumentLocation
                            {
                                S3Object = new S3Object
                                {
                                    Bucket = _bucketName,
                                    Name = key
                                }
                            }
                        });

                    // Step 3: Poll for completion
                    var jobId = startResponse.JobId;
                    GetDocumentTextDetectionResponse getResponse;

                    do
                    {
                        await Task.Delay(5000); // Poll every 5 seconds
                        getResponse = await _textractClient.GetDocumentTextDetectionAsync(
                            new GetDocumentTextDetectionRequest { JobId = jobId });

                    } while (getResponse.JobStatus == JobStatus.IN_PROGRESS);

                    if (getResponse.JobStatus != JobStatus.SUCCEEDED)
                    {
                        throw new Exception($"Textract job failed: {getResponse.StatusMessage}");
                    }

                    // Step 4: Handle paginated results
                    var allText = new StringBuilder();
                    string nextToken = null;

                    do
                    {
                        var pageResponse = await _textractClient.GetDocumentTextDetectionAsync(
                            new GetDocumentTextDetectionRequest
                            {
                                JobId = jobId,
                                NextToken = nextToken
                            });

                        foreach (var block in pageResponse.Blocks
                            .Where(b => b.BlockType == BlockType.LINE))
                        {
                            allText.AppendLine(block.Text);
                        }

                        nextToken = pageResponse.NextToken;
                    } while (nextToken != null);

                    return allText.ToString();
                }
                finally
                {
                    // Step 5: Clean up S3 object
                    await _s3Client.DeleteObjectAsync(_bucketName, key);
                }
            }
        }
    }

    // AFTER: IronOCR (Direct processing)
    namespace After
    {
        using IronOcr;

        public class IronOcrPdfService
        {
            public string ProcessPdf(string pdfPath)
            {
                // Direct processing - no S3, no async, no polling
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdf(pdfPath);

                return ocr.Read(input).Text;
            }

            // Process specific pages
            public string ProcessPdfPages(string pdfPath, int startPage, int endPage)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdfPages(pdfPath, startPage, endPage);

                return ocr.Read(input).Text;
            }

            // Process password-protected PDF (no Textract equivalent)
            public string ProcessEncryptedPdf(string pdfPath, string password)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdf(pdfPath, Password: password);

                return ocr.Read(input).Text;
            }
        }
    }
}

// ============================================================================
// SCENARIO 5: Batch Processing
// ============================================================================

namespace Migration.BatchProcessing
{
    // BEFORE: AWS Textract (Credential/TPS management)
    namespace Before
    {
        using Amazon.Textract;
        using Amazon.Textract.Model;

        public class TextractBatchService
        {
            private readonly AmazonTextractClient _client;
            private readonly SemaphoreSlim _throttle;

            public TextractBatchService()
            {
                _client = new AmazonTextractClient(Amazon.RegionEndpoint.USEast1);
                // Textract has TPS limits - must throttle
                _throttle = new SemaphoreSlim(5); // 5 concurrent requests max
            }

            public async Task<Dictionary<string, string>> ProcessBatchAsync(string[] imagePaths)
            {
                var results = new Dictionary<string, string>();
                var tasks = new List<Task>();

                foreach (var path in imagePaths)
                {
                    await _throttle.WaitAsync();

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var imageBytes = File.ReadAllBytes(path);

                            var request = new DetectDocumentTextRequest
                            {
                                Document = new Document
                                {
                                    Bytes = new MemoryStream(imageBytes)
                                }
                            };

                            var response = await _client.DetectDocumentTextAsync(request);

                            var text = string.Join("\n", response.Blocks
                                .Where(b => b.BlockType == BlockType.LINE)
                                .Select(b => b.Text));

                            lock (results)
                            {
                                results[path] = text;
                            }
                        }
                        catch (AmazonTextractException ex) when (ex.ErrorCode == "ProvisionedThroughputExceededException")
                        {
                            // Handle rate limiting
                            await Task.Delay(1000);
                            // Retry logic needed...
                        }
                        finally
                        {
                            _throttle.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                return results;
            }
        }
    }

    // AFTER: IronOCR (Simple parallel processing)
    namespace After
    {
        using IronOcr;
        using System.Collections.Concurrent;

        public class IronOcrBatchService
        {
            public Dictionary<string, string> ProcessBatch(string[] imagePaths)
            {
                var ocr = new IronTesseract();
                var results = new ConcurrentDictionary<string, string>();

                // No TPS limits - process as fast as local hardware allows
                Parallel.ForEach(imagePaths, path =>
                {
                    var result = ocr.Read(path);
                    results[path] = result.Text;
                });

                return results.ToDictionary(x => x.Key, x => x.Value);
            }

            // Batch with progress reporting
            public Dictionary<string, string> ProcessBatchWithProgress(
                string[] imagePaths,
                IProgress<int> progress)
            {
                var ocr = new IronTesseract();
                var results = new ConcurrentDictionary<string, string>();
                int processed = 0;

                Parallel.ForEach(imagePaths, path =>
                {
                    var result = ocr.Read(path);
                    results[path] = result.Text;

                    var current = Interlocked.Increment(ref processed);
                    progress?.Report(current * 100 / imagePaths.Length);
                });

                return results.ToDictionary(x => x.Key, x => x.Value);
            }
        }
    }
}

// ============================================================================
// SCENARIO 6: Error Handling
// ============================================================================

namespace Migration.ErrorHandling
{
    // BEFORE: AWS Textract exception handling
    namespace Before
    {
        using Amazon.Textract;
        using Amazon.Textract.Model;

        public class TextractErrorHandling
        {
            private readonly AmazonTextractClient _client;

            public async Task<string> ExtractWithErrorHandlingAsync(string imagePath)
            {
                try
                {
                    var imageBytes = File.ReadAllBytes(imagePath);

                    var request = new DetectDocumentTextRequest
                    {
                        Document = new Document
                        {
                            Bytes = new MemoryStream(imageBytes)
                        }
                    };

                    var response = await _client.DetectDocumentTextAsync(request);

                    return string.Join("\n", response.Blocks
                        .Where(b => b.BlockType == BlockType.LINE)
                        .Select(b => b.Text));
                }
                catch (AmazonTextractException ex) when (ex.ErrorCode == "InvalidParameterException")
                {
                    throw new ArgumentException("Invalid image format or size", ex);
                }
                catch (AmazonTextractException ex) when (ex.ErrorCode == "ProvisionedThroughputExceededException")
                {
                    throw new Exception("Rate limit exceeded - retry after delay", ex);
                }
                catch (AmazonTextractException ex) when (ex.ErrorCode == "UnsupportedDocumentException")
                {
                    throw new NotSupportedException("Document format not supported", ex);
                }
                catch (AmazonTextractException ex) when (ex.ErrorCode == "AccessDeniedException")
                {
                    throw new UnauthorizedAccessException("AWS credentials invalid or insufficient permissions", ex);
                }
                catch (AmazonTextractException ex) when (ex.ErrorCode == "InternalServerError")
                {
                    throw new Exception("AWS Textract service error - retry later", ex);
                }
                catch (AmazonTextractException ex)
                {
                    throw new Exception($"Textract error: {ex.ErrorCode} - {ex.Message}", ex);
                }
            }
        }
    }

    // AFTER: IronOCR exception handling
    namespace After
    {
        using IronOcr;

        public class IronOcrErrorHandling
        {
            public string ExtractWithErrorHandling(string imagePath)
            {
                try
                {
                    var ocr = new IronTesseract();
                    var result = ocr.Read(imagePath);

                    // Check confidence for quality issues
                    if (result.Confidence < 50)
                    {
                        Console.WriteLine($"Warning: Low confidence ({result.Confidence}%) - consider preprocessing");
                    }

                    return result.Text;
                }
                catch (FileNotFoundException ex)
                {
                    throw new ArgumentException($"Image file not found: {imagePath}", ex);
                }
                catch (IOException ex)
                {
                    throw new InvalidOperationException($"Cannot read file: {imagePath}", ex);
                }
                catch (IronOcr.Exceptions.OcrException ex)
                {
                    throw new InvalidOperationException($"OCR processing failed: {ex.Message}", ex);
                }
            }

            // With preprocessing for problematic images
            public string ExtractWithPreprocessing(string imagePath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadImage(imagePath);

                // Apply preprocessing for better results
                input.Deskew();
                input.DeNoise();
                input.EnhanceResolution(300);

                var result = ocr.Read(input);

                if (string.IsNullOrWhiteSpace(result.Text))
                {
                    throw new InvalidOperationException("No text detected in image");
                }

                return result.Text;
            }
        }
    }
}

// ============================================================================
// MIGRATION HELPER: Complete Service Replacement
// ============================================================================

namespace Migration.CompleteReplacement
{
    using IronOcr;

    /// <summary>
    /// Drop-in replacement service that mimics Textract API structure
    /// for easier migration of existing codebases
    /// </summary>
    public class TextractCompatibleService
    {
        private readonly IronTesseract _ocr = new IronTesseract();

        // Mimics DetectDocumentTextAsync
        public Task<DetectDocumentTextResult> DetectDocumentTextAsync(string imagePath)
        {
            var result = _ocr.Read(imagePath);

            return Task.FromResult(new DetectDocumentTextResult
            {
                Text = result.Text,
                Lines = result.Lines.Select(l => new TextLine
                {
                    Text = l.Text,
                    Confidence = l.Confidence,
                    X = l.X,
                    Y = l.Y,
                    Width = l.Width,
                    Height = l.Height
                }).ToList(),
                Words = result.Words.Select(w => new TextWord
                {
                    Text = w.Text,
                    Confidence = w.Confidence,
                    X = w.X,
                    Y = w.Y
                }).ToList()
            });
        }

        // Mimics async PDF processing (but actually synchronous)
        public Task<string> ProcessLargeDocumentAsync(string pdfPath)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);
            var result = _ocr.Read(input);
            return Task.FromResult(result.Text);
        }
    }

    public class DetectDocumentTextResult
    {
        public string Text { get; set; }
        public List<TextLine> Lines { get; set; }
        public List<TextWord> Words { get; set; }
    }

    public class TextLine
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class TextWord
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
