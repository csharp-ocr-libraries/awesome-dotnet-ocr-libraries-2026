/**
 * IronOCR Image Preprocessing: Complete Guide
 *
 * Improve OCR accuracy with IronOCR's built-in preprocessing filters.
 * No external libraries needed - everything is included.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 * Documentation: https://ironsoftware.com/csharp/ocr/docs/
 */

using System;
using IronOcr;

namespace IronOcrPreprocessingExamples
{
    // ========================================================================
    // AUTOMATIC PREPROCESSING
    // ========================================================================

    public class AutomaticPreprocessing
    {
        /// <summary>
        /// IronOCR applies basic preprocessing automatically.
        /// For most documents, this is sufficient.
        /// </summary>
        public void AutomaticMode()
        {
            // Automatic preprocessing applied by default
            var text = new IronTesseract().Read("document.jpg").Text;
            Console.WriteLine(text);
        }
    }

    // ========================================================================
    // ROTATION AND ORIENTATION
    // ========================================================================

    public class RotationFilters
    {
        /// <summary>
        /// Deskew - automatically correct slight rotation
        /// Perfect for scanned documents that are slightly tilted.
        /// </summary>
        public void DeskewExample()
        {
            using var input = new OcrInput();
            input.LoadImage("tilted-scan.jpg");
            input.Deskew();  // Auto-detect and correct rotation

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Rotate - manual rotation by specific degrees
        /// </summary>
        public void ManualRotation()
        {
            using var input = new OcrInput();
            input.LoadImage("sideways-document.jpg");
            input.Rotate(90);  // Rotate 90 degrees clockwise

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Process upside-down document
        /// </summary>
        public void FlipUpsideDown()
        {
            using var input = new OcrInput();
            input.LoadImage("upside-down.jpg");
            input.Rotate(180);

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // NOISE REDUCTION
    // ========================================================================

    public class NoiseFilters
    {
        /// <summary>
        /// DeNoise - remove specks and noise
        /// Essential for faxes and low-quality scans.
        /// </summary>
        public void DeNoiseExample()
        {
            using var input = new OcrInput();
            input.LoadImage("noisy-fax.jpg");
            input.DeNoise();  // Remove noise/specks

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Sharpen - enhance edge definition
        /// Good for blurry images.
        /// </summary>
        public void SharpenExample()
        {
            using var input = new OcrInput();
            input.LoadImage("blurry-photo.jpg");
            input.Sharpen();

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // CONTRAST AND COLOR
    // ========================================================================

    public class ContrastFilters
    {
        /// <summary>
        /// Contrast - enhance text/background contrast
        /// Improves recognition of faded documents.
        /// </summary>
        public void ContrastExample()
        {
            using var input = new OcrInput();
            input.LoadImage("faded-document.jpg");
            input.Contrast();

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Binarize - convert to pure black and white
        /// Best for documents with complex backgrounds.
        /// </summary>
        public void BinarizeExample()
        {
            using var input = new OcrInput();
            input.LoadImage("colored-background.jpg");
            input.Binarize();  // Black and white only

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Invert - for white text on dark background
        /// </summary>
        public void InvertExample()
        {
            using var input = new OcrInput();
            input.LoadImage("white-on-black.jpg");
            input.Invert();  // Swap colors

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// ToGrayScale - convert color to grayscale
        /// </summary>
        public void GrayscaleExample()
        {
            using var input = new OcrInput();
            input.LoadImage("color-document.jpg");
            input.ToGrayScale();

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // RESOLUTION ENHANCEMENT
    // ========================================================================

    public class ResolutionFilters
    {
        /// <summary>
        /// EnhanceResolution - upscale to target DPI
        /// 300 DPI is optimal for OCR.
        /// </summary>
        public void EnhanceResolutionExample()
        {
            using var input = new OcrInput();
            input.LoadImage("low-res-72dpi.jpg");
            input.EnhanceResolution(300);  // Scale to 300 DPI

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Process very low resolution image
        /// </summary>
        public void ProcessLowResImage()
        {
            using var input = new OcrInput();
            input.LoadImage("thumbnail-50dpi.jpg");
            input.EnhanceResolution(300);
            input.Sharpen();  // Sharpen after upscaling

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // COMBINED PREPROCESSING PIPELINES
    // ========================================================================

    public class PreprocessingPipelines
    {
        /// <summary>
        /// Standard scanned document
        /// </summary>
        public string StandardScan(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Low-quality fax
        /// </summary>
        public string FaxDocument(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.DeNoise();
            input.Binarize();
            input.Contrast();
            input.EnhanceResolution(300);

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Photo of document (camera capture)
        /// </summary>
        public string PhotoOfDocument(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.Contrast();
            input.Sharpen();
            input.EnhanceResolution(300);

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Old/faded document
        /// </summary>
        public string FadedDocument(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Contrast();
            input.DeNoise();
            input.Binarize();

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Screenshot or digital image
        /// </summary>
        public string ScreenCapture(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            // Usually clean - minimal processing needed

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Inverted text (white on black)
        /// </summary>
        public string InvertedText(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Invert();
            input.DeNoise();

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Maximum quality processing
        /// Use when accuracy is critical.
        /// </summary>
        public string MaximumQuality(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();
            input.Contrast();
            input.Binarize();
            input.EnhanceResolution(300);

            return new IronTesseract().Read(input).Text;
        }
    }

    // ========================================================================
    // PREPROCESSING FOR PDFs
    // ========================================================================

    public class PdfPreprocessing
    {
        /// <summary>
        /// Preprocess scanned PDF
        /// </summary>
        public string ProcessScannedPdf(string pdfPath)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);
            input.Deskew();
            input.DeNoise();
            input.Contrast();

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Process specific PDF pages with preprocessing
        /// </summary>
        public string ProcessPdfPages(string pdfPath, int startPage, int endPage)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);
            // TODO: verify IronOCR API for page range selection (startPage to endPage)
            input.Deskew();
            input.DeNoise();

            return new IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// BETTER OCR STARTS WITH PREPROCESSING
//
// IronOCR includes everything you need - no external libraries.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Preprocessing Guide: https://ironsoftware.com/csharp/ocr/tutorials/
// ============================================================================
