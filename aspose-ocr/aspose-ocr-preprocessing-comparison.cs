/**
 * Aspose.OCR vs IronOCR: Image Preprocessing Comparison
 *
 * This file demonstrates the fundamental difference in preprocessing philosophy:
 * - Aspose.OCR: Requires explicit manual configuration of preprocessing filters
 * - IronOCR: Applies intelligent preprocessing automatically (fire and forget)
 *
 * Key insight: Aspose requires you to know what preprocessing is needed.
 * IronOCR analyzes images and applies appropriate corrections automatically.
 *
 * NuGet Packages:
 * - Aspose.OCR for Aspose examples
 * - IronOcr for IronOCR examples
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

// ============================================================================
// EXAMPLE 1: The Fundamental Philosophy Difference
// ============================================================================

namespace PreprocessingPhilosophy
{
    /// <summary>
    /// Aspose.OCR: YOU decide what preprocessing is needed.
    /// </summary>
    public class AsposeExample
    {
        public string ProcessImageWithPreprocessing(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // You must decide which filters to apply
            // Wrong filters = worse results
            // Missing filters = poor accuracy
            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // Add each filter manually - order matters!
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoSkew());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoDenoising());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.ContrastCorrectionFilter());

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters,
                Language = Aspose.OCR.Language.Eng
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }
    }

    /// <summary>
    /// IronOCR: The library decides what preprocessing is needed.
    /// </summary>
    public class IronOcrExample
    {
        public string ProcessImageWithPreprocessing(string imagePath)
        {
            // IronOCR automatically:
            // - Detects skew and corrects it
            // - Identifies noise patterns and removes them
            // - Adjusts contrast based on image characteristics
            // - Handles many other corrections intelligently

            return new IronOcr.IronTesseract().Read(imagePath).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 2: Manual Deskewing vs Automatic
// ============================================================================

namespace DeskewComparison
{
    /// <summary>
    /// Aspose.OCR deskewing - must be explicitly enabled.
    /// </summary>
    public class AsposeExample
    {
        public string DeskewAndOcr(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // AutoSkew filter attempts automatic correction
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoSkew());

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }

        public string DeskewWithKnownAngle(string imagePath, float angle)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // If you know the angle, specify it directly
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Rotate(angle));

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }

        public float DetectSkewAngle(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // Aspose provides skew detection
            float angle = api.CalculateSkew(imagePath);

            return angle;
        }
    }

    /// <summary>
    /// IronOCR deskewing - automatic or explicit.
    /// </summary>
    public class IronOcrExample
    {
        public string DeskewAndOcr(string imagePath)
        {
            // Automatic deskewing is built into default processing
            return new IronOcr.IronTesseract().Read(imagePath).Text;
        }

        public string ExplicitDeskew(string imagePath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Explicit deskew if you want to ensure it happens
            input.Deskew();

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string DeskewWithAngle(string imagePath, float angle)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Rotate by specific angle if known
            input.Rotate(angle);

            return new IronOcr.IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 3: Noise Removal Comparison
// ============================================================================

namespace NoiseRemoval
{
    /// <summary>
    /// Aspose.OCR noise removal - multiple filter options.
    /// </summary>
    public class AsposeExample
    {
        public string RemoveNoise(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // AutoDenoising for general noise
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoDenoising());

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }

        public string RemoveNoiseAggressive(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // Median filter for salt-and-pepper noise
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Median());

            // Optional additional smoothing
            // filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Smooth());

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }

        public string RemoveSpecificNoise(string imagePath, int noiseLevel)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // No built-in noise level parameter
            // Must choose appropriate filter based on image analysis
            if (noiseLevel > 50)
            {
                filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Median());
            }
            else
            {
                filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoDenoising());
            }

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }
    }

    /// <summary>
    /// IronOCR noise removal - automatic or fluent.
    /// </summary>
    public class IronOcrExample
    {
        public string RemoveNoise(string imagePath)
        {
            // IronOCR detects and removes noise automatically
            return new IronOcr.IronTesseract().Read(imagePath).Text;
        }

        public string RemoveNoiseExplicit(string imagePath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Explicit denoise if you want to ensure it
            input.DeNoise();

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string RemoveNoiseAggressive(string imagePath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Multiple passes for heavily degraded images
            input.DeNoise();
            input.Sharpen();  // Restore edge definition after denoising

            return new IronOcr.IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 4: Custom Filter Chains / Pipelines
// ============================================================================

namespace FilterPipelines
{
    /// <summary>
    /// Aspose.OCR filter chain - explicit configuration.
    /// </summary>
    public class AsposeExample
    {
        public string StandardScannedDocumentPipeline(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // Build complete pipeline for typical scanned documents
            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // 1. Fix rotation first
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoSkew());

            // 2. Remove noise
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoDenoising());

            // 3. Improve contrast
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.ContrastCorrectionFilter());

            // 4. Convert to binary (optional but often helps)
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Binarize());

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }

        public string LowQualityPhotoPipeline(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // For camera photos (not scans)
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoSkew());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.ContrastCorrectionFilter());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Median());
            // Upscale if too small
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Scale(2.0f));

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }

        public string ReceiptPipeline(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // Receipts often have thermal paper issues
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.ContrastCorrectionFilter());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoDenoising());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Threshold(100)); // Lower threshold for faded text

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters,
                RecognizeSingleLine = false
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }
    }

    /// <summary>
    /// IronOCR filter chain - fluent configuration.
    /// </summary>
    public class IronOcrExample
    {
        public string StandardScannedDocumentPipeline(string imagePath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Preprocessing operations (not chainable)
            input.Deskew();          // Fix rotation
            input.DeNoise();         // Remove noise
            input.Contrast();        // Improve contrast
            input.Binarize();        // Convert to black/white

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string LowQualityPhotoPipeline(string imagePath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            input.Deskew();
            input.Contrast();
            input.DeNoise();
            input.Scale(200);        // Upscale to 200% for better recognition

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string ReceiptPipeline(string imagePath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Thermal receipts often need special handling
            input.Contrast();
            input.DeNoise();
            input.Sharpen();         // Sharpen faded text

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string AutomaticPipeline(string imagePath)
        {
            // No explicit preprocessing - IronOCR handles it
            // This often produces excellent results without configuration
            return new IronOcr.IronTesseract().Read(imagePath).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 5: Binarization (Thresholding)
// ============================================================================

namespace BinarizationComparison
{
    /// <summary>
    /// Aspose.OCR binarization options.
    /// </summary>
    public class AsposeExample
    {
        public string AutoBinarize(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // Automatic binarization
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Binarize());

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }

        public string ManualThreshold(string imagePath, int threshold)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // Manual threshold value (0-255)
            // Lower values keep more pixels as foreground (good for faded text)
            // Higher values keep fewer pixels (good for noisy backgrounds)
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Threshold(threshold));

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }

        public (string text, int usedThreshold) FindOptimalThreshold(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();
            string bestText = "";
            int bestThreshold = 128;
            int maxTextLength = 0;

            // Try different thresholds to find best
            for (int thresh = 80; thresh <= 180; thresh += 20)
            {
                var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();
                filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Threshold(thresh));

                var settings = new Aspose.OCR.RecognitionSettings
                {
                    PreprocessingFilters = filters
                };

                var result = api.RecognizeImage(imagePath, settings);
                if (result.RecognitionText.Length > maxTextLength)
                {
                    maxTextLength = result.RecognitionText.Length;
                    bestText = result.RecognitionText;
                    bestThreshold = thresh;
                }
            }

            return (bestText, bestThreshold);
        }
    }

    /// <summary>
    /// IronOCR binarization - automatic by default.
    /// </summary>
    public class IronOcrExample
    {
        public string AutoBinarize(string imagePath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Automatic binarization with intelligent threshold selection
            input.Binarize();

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string NoExplicitBinarize(string imagePath)
        {
            // IronOCR internally handles binarization when beneficial
            return new IronOcr.IronTesseract().Read(imagePath).Text;
        }

        public string InvertColors(string imagePath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // For white text on dark background
            input.Invert();

            return new IronOcr.IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 6: Scaling / Resolution Control
// ============================================================================

namespace ScalingComparison
{
    /// <summary>
    /// Aspose.OCR scaling options.
    /// </summary>
    public class AsposeExample
    {
        public string UpscaleSmallImage(string imagePath, float scaleFactor)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // Scale filter for resizing
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Scale(scaleFactor));

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }

        public string ResizeToSpecificDimensions(string imagePath, int width, int height)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // Resize to specific dimensions
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Resize(width, height));

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }
    }

    /// <summary>
    /// IronOCR scaling - flexible options.
    /// </summary>
    public class IronOcrExample
    {
        public string UpscaleSmallImage(string imagePath, int percentage)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Scale by percentage (200 = 2x size)
            input.Scale(percentage);

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string SetTargetDpi(string imagePath, int dpi)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Set target DPI for consistent processing
            // TODO: verify IronOCR API for setting target DPI
            input.EnhanceResolution(dpi);

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string AutoEnhanceSmallImages(string imagePath)
        {
            // IronOCR automatically handles small images
            // by upscaling when needed for better recognition
            return new IronOcr.IronTesseract().Read(imagePath).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 7: Performance Impact of Preprocessing
// ============================================================================

namespace PerformanceComparison
{
    /// <summary>
    /// Aspose.OCR preprocessing performance considerations.
    /// </summary>
    public class AsposeExample
    {
        public (string text, long milliseconds) MeasurePreprocessingImpact(
            string imagePath,
            bool usePreprocessing)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Aspose.OCR.RecognitionSettings settings;

            if (usePreprocessing)
            {
                var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();
                filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoSkew());
                filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoDenoising());
                filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.ContrastCorrectionFilter());

                settings = new Aspose.OCR.RecognitionSettings
                {
                    PreprocessingFilters = filters
                };
            }
            else
            {
                settings = new Aspose.OCR.RecognitionSettings();
            }

            var result = api.RecognizeImage(imagePath, settings);
            sw.Stop();

            return (result.RecognitionText, sw.ElapsedMilliseconds);
        }

        public void ComparePreprocessingVsRaw(string imagePath)
        {
            var (rawText, rawTime) = MeasurePreprocessingImpact(imagePath, false);
            var (processedText, processedTime) = MeasurePreprocessingImpact(imagePath, true);

            Console.WriteLine($"Without preprocessing: {rawTime}ms, {rawText.Length} chars");
            Console.WriteLine($"With preprocessing: {processedTime}ms, {processedText.Length} chars");
            Console.WriteLine($"Preprocessing overhead: {processedTime - rawTime}ms");
        }
    }

    /// <summary>
    /// IronOCR performance - automatic preprocessing is optimized.
    /// </summary>
    public class IronOcrExample
    {
        public (string text, long milliseconds) MeasureAutomatic(string imagePath)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Automatic preprocessing is included in standard timing
            var result = new IronOcr.IronTesseract().Read(imagePath);

            sw.Stop();
            return (result.Text, sw.ElapsedMilliseconds);
        }

        public (string text, long milliseconds) MeasureWithExplicitPreprocessing(string imagePath)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();
            input.Contrast();

            var result = new IronOcr.IronTesseract().Read(input);

            sw.Stop();
            return (result.Text, sw.ElapsedMilliseconds);
        }

        public void CompareAutoVsExplicit(string imagePath)
        {
            var (autoText, autoTime) = MeasureAutomatic(imagePath);
            var (explicitText, explicitTime) = MeasureWithExplicitPreprocessing(imagePath);

            Console.WriteLine($"Automatic: {autoTime}ms, {autoText.Length} chars");
            Console.WriteLine($"Explicit: {explicitTime}ms, {explicitText.Length} chars");

            // Note: Automatic is often just as fast because
            // IronOCR only applies preprocessing when beneficial
        }
    }
}


// ============================================================================
// EXAMPLE 8: Specific Image Type Handling
// ============================================================================

namespace ImageTypeHandling
{
    /// <summary>
    /// Aspose.OCR specific image type handling.
    /// </summary>
    public class AsposeExample
    {
        public string ProcessScreenshot(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // Screenshots usually don't need preprocessing
            // but may need specific settings
            var settings = new Aspose.OCR.RecognitionSettings
            {
                DetectAreasMode = Aspose.OCR.DetectAreasMode.COMBINE
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }

        public string ProcessHandwriting(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.ContrastCorrectionFilter());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Binarize());

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters,
                // Handwriting mode if available
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }

        public string ProcessNegativeImage(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();

            // Invert colors for white-on-black text
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.Invert());

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }
    }

    /// <summary>
    /// IronOCR specific image type handling.
    /// </summary>
    public class IronOcrExample
    {
        public string ProcessScreenshot(string imagePath)
        {
            // Screenshots work great automatically
            return new IronOcr.IronTesseract().Read(imagePath).Text;
        }

        public string ProcessHandwriting(string imagePath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Preprocessing helps with handwriting
            input.Contrast();
            input.DeNoise();

            // Note: OCR accuracy on handwriting is limited
            // by all current OCR engines
            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string ProcessNegativeImage(string imagePath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Invert white-on-black to black-on-white
            input.Invert();

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string ProcessTableOrForm(string imagePath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // IronOCR detects tables and forms automatically
            var result = new IronOcr.IronTesseract().Read(input);

            // Tables expose structured data
            // TODO: verify IronOCR API for table properties
            if (result.Tables != null)
            {
                foreach (var table in result.Tables)
                {
                    Console.WriteLine($"Found table with text: {table.Text}");
                    // Console.WriteLine($"Found table: {table.RowCount}x{table.ColumnCount}");
                }
            }

            return result.Text;
        }
    }
}


// ============================================================================
// EXAMPLE 9: Combining Preprocessing with Language Settings
// ============================================================================

namespace PreprocessingWithLanguage
{
    /// <summary>
    /// Aspose.OCR preprocessing + language.
    /// </summary>
    public class AsposeExample
    {
        public string ProcessMultilingualDocument(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoSkew());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoDenoising());

            var settings = new Aspose.OCR.RecognitionSettings
            {
                PreprocessingFilters = filters,
                Language = Aspose.OCR.Language.Eng // Single language at a time
            };

            return api.RecognizeImage(imagePath, settings).RecognitionText;
        }
    }

    /// <summary>
    /// IronOCR preprocessing + language.
    /// </summary>
    public class IronOcrExample
    {
        public string ProcessMultilingualDocument(string imagePath)
        {
            var ocr = new IronOcr.IronTesseract();

            // Multiple languages can be combined
            ocr.Language = IronOcr.OcrLanguage.English;
            ocr.AddSecondaryLanguage(IronOcr.OcrLanguage.French);
            ocr.AddSecondaryLanguage(IronOcr.OcrLanguage.German);

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();

            return ocr.Read(input).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 10: Preprocessing Preview / Debug
// ============================================================================

namespace PreprocessingDebug
{
    /// <summary>
    /// Aspose.OCR preprocessing preview.
    /// </summary>
    public class AsposeExample
    {
        public void SavePreprocessedImage(string inputPath, string outputPath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoSkew());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.ContrastCorrectionFilter());

            // Apply preprocessing and save result
            // Useful for debugging what the OCR engine "sees"
            using var ms = api.PreprocessImage(inputPath, filters);

            using var fs = new FileStream(outputPath, FileMode.Create);
            ms.CopyTo(fs);

            Console.WriteLine($"Preprocessed image saved to: {outputPath}");
        }

        public void CompareBeforeAfter(string imagePath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // Without preprocessing
            var rawResult = api.RecognizeImage(imagePath, new Aspose.OCR.RecognitionSettings());

            // With preprocessing
            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoSkew());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoDenoising());

            var processedResult = api.RecognizeImage(imagePath,
                new Aspose.OCR.RecognitionSettings { PreprocessingFilters = filters });

            Console.WriteLine($"Raw: {rawResult.RecognitionText.Length} chars");
            Console.WriteLine($"Processed: {processedResult.RecognitionText.Length} chars");
        }
    }

    /// <summary>
    /// IronOCR preprocessing debug options.
    /// </summary>
    public class IronOcrExample
    {
        public void SavePreprocessedImage(string inputPath, string outputPath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadImage(inputPath);

            // Apply preprocessing
            input.Deskew();
            input.DeNoise();
            input.Contrast();

            // Save preprocessed pages for debugging
            foreach (var page in input.GetPages())
            {
                page.SaveAsImage(outputPath);
            }

            Console.WriteLine($"Preprocessed image saved to: {outputPath}");
        }

        public void DebugAllStages(string imagePath)
        {
            var tempDir = Path.GetTempPath();

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Save after each step to see progression
            input.GetPages()[0].SaveAsImage(Path.Combine(tempDir, "1_original.png"));

            input.Deskew();
            input.GetPages()[0].SaveAsImage(Path.Combine(tempDir, "2_deskewed.png"));

            input.DeNoise();
            input.GetPages()[0].SaveAsImage(Path.Combine(tempDir, "3_denoised.png"));

            input.Contrast();
            input.GetPages()[0].SaveAsImage(Path.Combine(tempDir, "4_contrast.png"));

            var result = new IronOcr.IronTesseract().Read(input);

            Console.WriteLine($"Final text: {result.Text.Length} chars");
            Console.WriteLine($"Debug images saved to: {tempDir}");
        }
    }
}
