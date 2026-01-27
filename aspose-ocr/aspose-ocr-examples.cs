/**
 * Aspose.OCR vs IronOCR: Code Examples Comparison
 *
 * This file demonstrates common OCR scenarios implemented in both
 * Aspose.OCR and IronOCR, highlighting API differences and complexity.
 *
 * Each section shows the Aspose.OCR approach followed by the IronOCR equivalent.
 *
 * NuGet Packages:
 * - Aspose.OCR for Aspose examples
 * - IronOcr for IronOCR examples
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// EXAMPLE 1: Basic Text Extraction
// ============================================================================

namespace BasicTextExtraction
{
    /// <summary>
    /// Aspose.OCR basic text extraction.
    /// </summary>
    public class AsposeExample
    {
        public string ExtractText(string imagePath)
        {
            // Initialize API
            var api = new Aspose.OCR.AsposeOcr();

            // Configure settings
            var settings = new Aspose.OCR.RecognitionSettings
            {
                Language = Aspose.OCR.Language.Eng,
                AutoSkew = true
            };

            // Perform OCR
            var result = api.RecognizeImage(imagePath, settings);

            return result.RecognitionText;
        }

        public (string Text, float Confidence) ExtractWithConfidence(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var settings = new Aspose.OCR.RecognitionSettings
            {
                Language = Aspose.OCR.Language.Eng
            };

            var result = api.RecognizeImage(imagePath, settings);

            // Confidence is an array per recognition area
            float avgConfidence = result.RecognitionAreasConfidence.Average();

            return (result.RecognitionText, avgConfidence);
        }
    }

    /// <summary>
    /// IronOCR equivalent - simpler API.
    /// </summary>
    public class IronOcrExample
    {
        public string ExtractText(string imagePath)
        {
            // One-liner for basic extraction
            return new IronOcr.IronTesseract().Read(imagePath).Text;
        }

        public (string Text, double Confidence) ExtractWithConfidence(string imagePath)
        {
            var result = new IronOcr.IronTesseract().Read(imagePath);

            // Single confidence value for entire result
            return (result.Text, result.Confidence);
        }
    }
}


// ============================================================================
// EXAMPLE 2: Multiple Language Support
// ============================================================================

namespace MultiLanguageOcr
{
    /// <summary>
    /// Aspose.OCR multi-language configuration.
    /// </summary>
    public class AsposeExample
    {
        public string ExtractMultiLanguage(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var settings = new Aspose.OCR.RecognitionSettings
            {
                // Limited language combination options
                Language = Aspose.OCR.Language.Eng,
                // Some language combinations require specific configuration
            };

            var result = api.RecognizeImage(imagePath, settings);
            return result.RecognitionText;
        }

        public string ExtractChinese(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var settings = new Aspose.OCR.RecognitionSettings
            {
                Language = Aspose.OCR.Language.Chi // Chinese
            };

            var result = api.RecognizeImage(imagePath, settings);
            return result.RecognitionText;
        }
    }

    /// <summary>
    /// IronOCR multi-language - add languages via NuGet.
    /// </summary>
    public class IronOcrExample
    {
        public string ExtractMultiLanguage(string imagePath)
        {
            var ocr = new IronOcr.IronTesseract();

            // Add multiple languages
            ocr.Language = IronOcr.OcrLanguage.English;
            ocr.AddSecondaryLanguage(IronOcr.OcrLanguage.French);
            ocr.AddSecondaryLanguage(IronOcr.OcrLanguage.German);

            return ocr.Read(imagePath).Text;
        }

        public string ExtractChinese(string imagePath)
        {
            // Requires: dotnet add package IronOcr.Languages.ChineseSimplified
            var ocr = new IronOcr.IronTesseract();
            ocr.Language = IronOcr.OcrLanguage.ChineseSimplified;

            return ocr.Read(imagePath).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 3: Image Preprocessing
// ============================================================================

namespace ImagePreprocessing
{
    /// <summary>
    /// Aspose.OCR preprocessing - requires explicit filter configuration.
    /// </summary>
    public class AsposeExample
    {
        public string ExtractWithPreprocessing(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // Build filter chain manually
            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // Add each filter explicitly
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoSkew());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.ContrastCorrectionFilter());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Median());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Threshold(128)); // Manual threshold

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            var result = api.RecognizeImage(imagePath, settings);
            return result.RecognitionText;
        }

        public string ExtractRotated(string imagePath, float angle)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Rotate(angle));

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }
    }

    /// <summary>
    /// IronOCR preprocessing - automatic or explicit via fluent API.
    /// </summary>
    public class IronOcrExample
    {
        public string ExtractWithPreprocessing(string imagePath)
        {
            var ocr = new IronOcr.IronTesseract();

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Preprocessing operations (not chainable)
            input.Deskew();          // Auto-rotate
            input.Contrast();        // Enhance contrast
            input.DeNoise();         // Remove noise
            input.Binarize();        // Auto-threshold

            return ocr.Read(input).Text;
        }

        public string ExtractRotated(string imagePath, float angle)
        {
            var ocr = new IronOcr.IronTesseract();

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);
            input.Rotate(angle);

            return ocr.Read(input).Text;
        }

        public string ExtractAuto(string imagePath)
        {
            // IronOCR applies intelligent preprocessing automatically
            return new IronOcr.IronTesseract().Read(imagePath).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 4: PDF Processing
// ============================================================================

namespace PdfProcessing
{
    /// <summary>
    /// Aspose.OCR PDF processing.
    /// </summary>
    public class AsposeExample
    {
        public string ExtractFromPdf(string pdfPath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var settings = new Aspose.OCR.DocumentRecognitionSettings();

            var results = api.RecognizePdf(pdfPath, settings);

            var text = new StringBuilder();
            foreach (var page in results)
            {
                text.AppendLine(page.RecognitionText);
            }

            return text.ToString();
        }

        public string ExtractPdfPages(string pdfPath, int startPage, int pageCount)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // Note: 0-based page indexing
            var settings = new Aspose.OCR.DocumentRecognitionSettings
            {
                StartPage = startPage, // 0-based
                PagesNumber = pageCount
            };

            var results = api.RecognizePdf(pdfPath, settings);

            var text = new StringBuilder();
            foreach (var page in results)
            {
                text.AppendLine(page.RecognitionText);
            }

            return text.ToString();
        }

        public string ExtractFromEncryptedPdf(string pdfPath, string password)
        {
            // Aspose.OCR cannot decrypt PDFs directly
            // Requires Aspose.PDF (separate license)

            /*
            // With Aspose.PDF (additional license required):
            using var doc = new Aspose.Pdf.Document(pdfPath, password);

            // Convert each page to image
            var images = new List<string>();
            foreach (Aspose.Pdf.Page page in doc.Pages)
            {
                // Complex conversion logic...
            }

            // Then OCR the images
            var api = new Aspose.OCR.AsposeOcr();
            // ...
            */

            throw new NotImplementedException(
                "Requires Aspose.PDF license for encrypted PDFs");
        }
    }

    /// <summary>
    /// IronOCR PDF processing - native support for all features.
    /// </summary>
    public class IronOcrExample
    {
        public string ExtractFromPdf(string pdfPath)
        {
            // Direct PDF support
            return new IronOcr.IronTesseract().Read(pdfPath).Text;
        }

        public string ExtractPdfPages(string pdfPath, int startPage, int endPage)
        {
            var ocr = new IronOcr.IronTesseract();

            using var input = new IronOcr.OcrInput();
            // Note: 1-based page indexing
            input.LoadPdf(pdfPath);
            // TODO: verify IronOCR API for page range selection

            return ocr.Read(input).Text;
        }

        public string ExtractFromEncryptedPdf(string pdfPath, string password)
        {
            // Built-in password support - no additional license
            using var input = new IronOcr.OcrInput();
            input.LoadPdf(pdfPath, Password: password);

            return new IronOcr.IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 5: Creating Searchable PDFs
// ============================================================================

namespace SearchablePdfCreation
{
    /// <summary>
    /// Aspose.OCR searchable PDF creation.
    /// </summary>
    public class AsposeExample
    {
        public void CreateSearchablePdf(string[] imagePaths, string outputPdf)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var results = new List<Aspose.OCR.RecognitionResult>();

            foreach (var path in imagePaths)
            {
                var result = api.RecognizeImage(path, new Aspose.OCR.RecognitionSettings());
                results.Add(result);
            }

            // Save as PDF with text layer
            api.SaveMultipageDocument(outputPdf, Aspose.OCR.SaveFormat.Pdf, results);
        }

        public void CreateFromScannedPdf(string inputPdf, string outputPdf)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var settings = new Aspose.OCR.DocumentRecognitionSettings();

            var results = api.RecognizePdf(inputPdf, settings);

            // Reconstruct as searchable PDF
            api.SaveMultipageDocument(outputPdf, Aspose.OCR.SaveFormat.Pdf, results.ToList());
        }
    }

    /// <summary>
    /// IronOCR searchable PDF - direct method.
    /// </summary>
    public class IronOcrExample
    {
        public void CreateSearchablePdf(string[] imagePaths, string outputPdf)
        {
            var ocr = new IronOcr.IronTesseract();

            using var input = new IronOcr.OcrInput();
            foreach (var path in imagePaths)
            {
                input.LoadImage(path);
            }

            var result = ocr.Read(input);

            // Direct searchable PDF output
            result.SaveAsSearchablePdf(outputPdf);
        }

        public void CreateFromScannedPdf(string inputPdf, string outputPdf)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadPdf(inputPdf);

            var result = new IronOcr.IronTesseract().Read(input);

            // Original pages + text layer
            result.SaveAsSearchablePdf(outputPdf);
        }
    }
}


// ============================================================================
// EXAMPLE 6: Batch Processing
// ============================================================================

namespace BatchProcessing
{
    /// <summary>
    /// Aspose.OCR batch processing.
    /// </summary>
    public class AsposeExample
    {
        public Dictionary<string, string> ProcessBatch(string[] imagePaths)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var settings = new Aspose.OCR.RecognitionSettings
            {
                ThreadsCount = Environment.ProcessorCount
            };

            var results = new Dictionary<string, string>();

            foreach (var path in imagePaths)
            {
                try
                {
                    var result = api.RecognizeImage(path, settings);
                    results[path] = result.RecognitionText;
                }
                catch (Exception ex)
                {
                    results[path] = $"Error: {ex.Message}";
                }
            }

            return results;
        }

        public Dictionary<string, string> ProcessBatchParallel(string[] imagePaths)
        {
            var results = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            Parallel.ForEach(imagePaths,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                path =>
                {
                    var api = new Aspose.OCR.AsposeOcr(); // Thread-safety considerations
                    try
                    {
                        var result = api.RecognizeImage(path, new Aspose.OCR.RecognitionSettings());
                        results[path] = result.RecognitionText;
                    }
                    catch (Exception ex)
                    {
                        results[path] = $"Error: {ex.Message}";
                    }
                });

            return new Dictionary<string, string>(results);
        }
    }

    /// <summary>
    /// IronOCR batch processing - built-in parallelization.
    /// </summary>
    public class IronOcrExample
    {
        public Dictionary<string, string> ProcessBatch(string[] imagePaths)
        {
            var ocr = new IronOcr.IronTesseract();

            using var input = new IronOcr.OcrInput();
            foreach (var path in imagePaths)
            {
                input.LoadImage(path);
            }

            // IronOCR handles parallelization internally
            var result = ocr.Read(input);

            // Map results back to file paths
            var results = new Dictionary<string, string>();
            for (int i = 0; i < result.Pages.Length && i < imagePaths.Length; i++)
            {
                results[imagePaths[i]] = result.Pages[i].Text;
            }

            return results;
        }

        public Dictionary<string, string> ProcessBatchWithDetails(string[] imagePaths)
        {
            var results = new Dictionary<string, string>();

            // Process each file independently for individual metrics
            foreach (var path in imagePaths)
            {
                var result = new IronOcr.IronTesseract().Read(path);
                results[path] = result.Text;

                Console.WriteLine($"Processed {Path.GetFileName(path)}: " +
                    $"{result.Text.Length} chars, {result.Confidence:F1}% confidence");
            }

            return results;
        }
    }
}


// ============================================================================
// EXAMPLE 7: Region/Area OCR
// ============================================================================

namespace RegionOcr
{
    /// <summary>
    /// Aspose.OCR region-based OCR.
    /// </summary>
    public class AsposeExample
    {
        public string ExtractFromRegion(string imagePath, Rectangle region)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // Define recognition area
            var settings = new Aspose.OCR.RecognitionSettings
            {
                RecognitionAreas = new List<Rectangle> { region }
            };

            var result = api.RecognizeImage(imagePath, settings);
            return result.RecognitionText;
        }

        public Dictionary<string, string> ExtractMultipleRegions(
            string imagePath,
            Dictionary<string, Rectangle> namedRegions)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var results = new Dictionary<string, string>();

            foreach (var kvp in namedRegions)
            {
                var settings = new Aspose.OCR.RecognitionSettings
                {
                    RecognitionAreas = new List<Rectangle> { kvp.Value }
                };

                var result = api.RecognizeImage(imagePath, settings);
                results[kvp.Key] = result.RecognitionText;
            }

            return results;
        }
    }

    /// <summary>
    /// IronOCR region-based OCR.
    /// </summary>
    public class IronOcrExample
    {
        public string ExtractFromRegion(string imagePath, Rectangle region)
        {
            var ocr = new IronOcr.IronTesseract();

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath, region);

            return ocr.Read(input).Text;
        }

        public Dictionary<string, string> ExtractMultipleRegions(
            string imagePath,
            Dictionary<string, Rectangle> namedRegions)
        {
            var ocr = new IronOcr.IronTesseract();
            var results = new Dictionary<string, string>();

            foreach (var kvp in namedRegions)
            {
                using var input = new IronOcr.OcrInput();
                input.LoadImage(imagePath, kvp.Value);

                var result = ocr.Read(input);
                results[kvp.Key] = result.Text;
            }

            return results;
        }
    }
}


// ============================================================================
// EXAMPLE 8: Word and Line Extraction with Positions
// ============================================================================

namespace StructuredExtraction
{
    public class OcrWord
    {
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float Confidence { get; set; }
    }

    /// <summary>
    /// Aspose.OCR structured extraction.
    /// </summary>
    public class AsposeExample
    {
        public List<OcrWord> ExtractWordsWithPositions(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var settings = new Aspose.OCR.RecognitionSettings
            {
                DetectAreasMode = Aspose.OCR.DetectAreasMode.COMBINE
            };

            var result = api.RecognizeImage(imagePath, settings);

            var words = new List<OcrWord>();

            // Accessing word-level data requires iteration through results
            // API varies by version - check documentation
            // This is a simplified representation

            foreach (var area in result.RecognitionAreasRectangles)
            {
                // Area-level data available
                // Word-level extraction may require additional processing
            }

            return words;
        }
    }

    /// <summary>
    /// IronOCR structured extraction - rich result model.
    /// </summary>
    public class IronOcrExample
    {
        public List<OcrWord> ExtractWordsWithPositions(string imagePath)
        {
            var result = new IronOcr.IronTesseract().Read(imagePath);

            // Direct access to word collection
            return result.Words.Select(w => new OcrWord
            {
                Text = w.Text,
                X = w.X,
                Y = w.Y,
                Width = w.Width,
                Height = w.Height,
                Confidence = (float)w.Confidence
            }).ToList();
        }

        public void PrintDocumentStructure(string imagePath)
        {
            var result = new IronOcr.IronTesseract().Read(imagePath);

            Console.WriteLine($"Document: {result.Pages.Length} pages");

            foreach (var page in result.Pages)
            {
                Console.WriteLine($"\nPage {page.PageNumber}:");
                Console.WriteLine($"  Paragraphs: {page.Paragraphs.Length}");
                Console.WriteLine($"  Lines: {page.Lines.Length}");
                Console.WriteLine($"  Words: {page.Words.Length}");
                Console.WriteLine($"  Characters: {page.Characters.Length}");
                Console.WriteLine($"  Confidence: {page.Confidence:F1}%");
            }
        }
    }
}


// ============================================================================
// EXAMPLE 9: Input from Various Sources
// ============================================================================

namespace InputSources
{
    /// <summary>
    /// Aspose.OCR input handling.
    /// </summary>
    public class AsposeExample
    {
        public string FromFile(string path)
        {
            var api = new Aspose.OCR.AsposeOcr();
            return api.RecognizeImage(path, new Aspose.OCR.RecognitionSettings()).RecognitionText;
        }

        public string FromStream(Stream imageStream)
        {
            var api = new Aspose.OCR.AsposeOcr();

            using var ms = new MemoryStream();
            imageStream.CopyTo(ms);

            return api.RecognizeImage(ms, new Aspose.OCR.RecognitionSettings()).RecognitionText;
        }

        public string FromBytes(byte[] imageData)
        {
            var api = new Aspose.OCR.AsposeOcr();

            using var ms = new MemoryStream(imageData);
            return api.RecognizeImage(ms, new Aspose.OCR.RecognitionSettings()).RecognitionText;
        }

        public string FromUrl(string imageUrl)
        {
            // Must download manually
            using var client = new System.Net.Http.HttpClient();
            var bytes = client.GetByteArrayAsync(imageUrl).Result;

            return FromBytes(bytes);
        }
    }

    /// <summary>
    /// IronOCR input handling - unified API.
    /// </summary>
    public class IronOcrExample
    {
        public string FromFile(string path)
        {
            return new IronOcr.IronTesseract().Read(path).Text;
        }

        public string FromStream(Stream imageStream)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imageStream);

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string FromBytes(byte[] imageData)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imageData);

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string FromUrl(string imageUrl)
        {
            using var input = new IronOcr.OcrInput();
            input.AddImage(imageUrl); // Built-in URL support

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string FromBitmap(Bitmap bitmap)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(bitmap);

            return new IronOcr.IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 10: Error Handling
// ============================================================================

namespace ErrorHandling
{
    /// <summary>
    /// Aspose.OCR error handling patterns.
    /// </summary>
    public class AsposeExample
    {
        public (string Text, List<string> Errors) SafeExtract(string imagePath)
        {
            var errors = new List<string>();

            try
            {
                var api = new Aspose.OCR.AsposeOcr();
                var result = api.RecognizeImage(imagePath, new Aspose.OCR.RecognitionSettings());

                // Check for warnings
                if (result.Warnings != null)
                {
                    foreach (var warning in result.Warnings)
                    {
                        errors.Add($"Warning: {warning}");
                    }
                }

                return (result.RecognitionText, errors);
            }
            catch (Aspose.OCR.AsposeOCRException ex)
            {
                errors.Add($"Aspose OCR Error: {ex.Message}");
                return (string.Empty, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"General Error: {ex.Message}");
                return (string.Empty, errors);
            }
        }
    }

    /// <summary>
    /// IronOCR error handling patterns.
    /// </summary>
    public class IronOcrExample
    {
        public (string Text, List<string> Errors) SafeExtract(string imagePath)
        {
            var errors = new List<string>();

            try
            {
                var result = new IronOcr.IronTesseract().Read(imagePath);

                // Check confidence for potential issues
                if (result.Confidence < 50)
                {
                    errors.Add($"Low confidence: {result.Confidence:F1}%");
                }

                // Check for empty result
                if (string.IsNullOrWhiteSpace(result.Text))
                {
                    errors.Add("No text detected in image");
                }

                return (result.Text, errors);
            }
            catch (IronOcr.Exceptions.IronOcrInputException ex)
            {
                errors.Add($"Input Error: {ex.Message}");
                return (string.Empty, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"General Error: {ex.Message}");
                return (string.Empty, errors);
            }
        }

        public void ValidateBeforeProcessing(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image not found", imagePath);
            }

            var extension = Path.GetExtension(imagePath).ToLower();
            var supportedFormats = new[] { ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp", ".gif", ".pdf" };

            if (!supportedFormats.Contains(extension))
            {
                throw new NotSupportedException($"Format not supported: {extension}");
            }
        }
    }
}


// ============================================================================
// EXAMPLE 11: Memory Management for High-Volume Processing
// ============================================================================

namespace HighVolumeProcessing
{
    /// <summary>
    /// Aspose.OCR high-volume considerations.
    /// </summary>
    public class AsposeExample
    {
        public void ProcessLargeVolume(string[] imagePaths)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var settings = new Aspose.OCR.RecognitionSettings();

            int processedCount = 0;

            foreach (var path in imagePaths)
            {
                var result = api.RecognizeImage(path, settings);

                // Process result...

                processedCount++;

                // Manual memory management sometimes needed
                if (processedCount % 100 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Console.WriteLine($"Processed {processedCount} images");
                }
            }
        }
    }

    /// <summary>
    /// IronOCR high-volume - automatic resource management.
    /// </summary>
    public class IronOcrExample
    {
        public void ProcessLargeVolume(string[] imagePaths)
        {
            var ocr = new IronOcr.IronTesseract();

            foreach (var path in imagePaths)
            {
                // IronOCR manages resources internally
                var result = ocr.Read(path);

                // Process result...
            }
        }

        public void ProcessInBatches(string[] imagePaths, int batchSize = 50)
        {
            var ocr = new IronOcr.IronTesseract();

            for (int i = 0; i < imagePaths.Length; i += batchSize)
            {
                using var input = new IronOcr.OcrInput();

                // Load batch
                var batch = imagePaths.Skip(i).Take(batchSize);
                foreach (var path in batch)
                {
                    input.LoadImage(path);
                }

                // Process batch
                var result = ocr.Read(input);

                Console.WriteLine($"Batch {i / batchSize + 1}: " +
                    $"{result.Pages.Length} pages processed");

                // OcrInput disposes automatically, releasing memory
            }
        }
    }
}
