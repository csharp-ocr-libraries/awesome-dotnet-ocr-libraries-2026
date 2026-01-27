/**
 * Windows.Media.Ocr vs IronOCR: Code Examples
 *
 * Compare Windows built-in OCR with IronOCR.
 * Windows OCR is free but limited; IronOCR is feature-complete.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * Windows.Media.Ocr: Built into Windows 10/11
 * IronOCR NuGet: https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.Threading.Tasks;

// ============================================================================
// WINDOWS.MEDIA.OCR IMPLEMENTATION
// Built into Windows 10/11, but limited
// ============================================================================

namespace WindowsOcrExamples
{
    /*
    using Windows.Media.Ocr;
    using Windows.Graphics.Imaging;
    using Windows.Storage;

    /// <summary>
    /// Windows.Media.Ocr limitations:
    /// 1. Windows 10/11 only
    /// 2. UWP/WinRT API (complex for desktop apps)
    /// 3. Limited language support
    /// 4. No PDF support
    /// 5. No preprocessing
    /// 6. No searchable PDF output
    /// </summary>
    public class WindowsOcrService
    {
        public async Task<string> ExtractTextAsync(string imagePath)
        {
            var engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));
            if (engine == null)
            {
                throw new Exception("OCR engine not available for this language");
            }

            // Complex image loading via WinRT
            var file = await StorageFile.GetFileFromPathAsync(imagePath);
            using var stream = await file.OpenAsync(FileAccessMode.Read);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = await decoder.GetSoftwareBitmapAsync();

            var result = await engine.RecognizeAsync(bitmap);
            return result.Text;
        }
    }
    */

    public class WindowsOcrLimitations
    {
        public void ShowLimitations()
        {
            Console.WriteLine("Windows.Media.Ocr Limitations:");
            Console.WriteLine("1. Windows 10/11 only - no cross-platform");
            Console.WriteLine("2. UWP/WinRT API - complex for .NET apps");
            Console.WriteLine("3. Languages based on installed Windows packs");
            Console.WriteLine("4. No PDF support at all");
            Console.WriteLine("5. No image preprocessing");
            Console.WriteLine("6. No searchable PDF creation");
            Console.WriteLine("7. No confidence scores per word");
            Console.WriteLine("8. Limited accuracy on low-quality images");
            Console.WriteLine();
            Console.WriteLine("For a complete solution, use IronOCR:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - CROSS-PLATFORM, FEATURE-COMPLETE
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR advantages over Windows.Media.Ocr:
    /// - Works on Windows, Linux, macOS, Docker
    /// - Simple .NET API (no WinRT complexity)
    /// - Native PDF support
    /// - 125+ languages
    /// - Built-in preprocessing
    /// - Searchable PDF output
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Simple API - no WinRT complexity
        /// </summary>
        public string ExtractText(string imagePath)
        {
            // One line - works on any platform
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// PDF OCR - not possible with Windows.Media.Ocr
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Password-protected PDF
        /// </summary>
        public string ExtractFromEncryptedPdf(string pdfPath, string password)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath, Password: password);
            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// 125+ languages - not dependent on Windows language packs
        /// </summary>
        public string ExtractWithLanguage(string imagePath, OcrLanguage language)
        {
            var ocr = new IronTesseract();
            ocr.Language = language;
            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Multi-language documents
        /// </summary>
        public string ExtractMultiLanguage(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            ocr.AddSecondaryLanguage(OcrLanguage.German);

            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Preprocessing for better accuracy
        /// </summary>
        public string ExtractWithPreprocessing(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();
            input.Contrast();
            input.EnhanceResolution(300);

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Create searchable PDF - not possible with Windows OCR
        /// </summary>
        public void CreateSearchablePdf(string inputPath, string outputPath)
        {
            var result = new IronTesseract().Read(inputPath);
            result.SaveAsSearchablePdf(outputPath);
        }

        /// <summary>
        /// Word-level confidence scores
        /// </summary>
        public void ExtractWithConfidence(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);

            Console.WriteLine($"Overall confidence: {result.Confidence}%");

            foreach (var word in result.Words)
            {
                Console.WriteLine($"'{word.Text}' - {word.Confidence}% confidence");
            }
        }

        /// <summary>
        /// Read barcodes during OCR
        /// </summary>
        public void ExtractTextAndBarcodes(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Configuration.ReadBarCodes = true;

            var result = ocr.Read(imagePath);

            Console.WriteLine($"Text: {result.Text}");
            foreach (var barcode in result.Barcodes)
            {
                Console.WriteLine($"Barcode: {barcode.Value}");
            }
        }
    }
}


// ============================================================================
// DETAILED COMPARISON
// ============================================================================

namespace Comparison
{
    public class WindowsVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== WINDOWS.MEDIA.OCR vs IRONOCR ===\n");

            Console.WriteLine("Feature          | Windows OCR    | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("Platform         | Windows only   | Cross-platform");
            Console.WriteLine("API complexity   | WinRT (complex)| Simple .NET");
            Console.WriteLine("PDF support      | No             | Native");
            Console.WriteLine("Languages        | OS-dependent   | 125+");
            Console.WriteLine("Preprocessing    | No             | Built-in");
            Console.WriteLine("Searchable PDF   | No             | Yes");
            Console.WriteLine("Barcode reading  | No             | Yes");
            Console.WriteLine("Word confidence  | Limited        | Full");
            Console.WriteLine("Password PDFs    | No             | Yes");
            Console.WriteLine("Price            | Free           | One-time license");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public void ApiComparison()
        {
            Console.WriteLine("=== API COMPLEXITY ===\n");

            Console.WriteLine("WINDOWS.MEDIA.OCR (20+ lines):");
            Console.WriteLine("  var engine = OcrEngine.TryCreateFromLanguage(...);");
            Console.WriteLine("  var file = await StorageFile.GetFileFromPathAsync(...);");
            Console.WriteLine("  using var stream = await file.OpenAsync(...);");
            Console.WriteLine("  var decoder = await BitmapDecoder.CreateAsync(...);");
            Console.WriteLine("  var bitmap = await decoder.GetSoftwareBitmapAsync();");
            Console.WriteLine("  var result = await engine.RecognizeAsync(bitmap);");
            Console.WriteLine("  // Plus error handling, null checks, etc.");
            Console.WriteLine();

            Console.WriteLine("IRONOCR (1 line):");
            Console.WriteLine("  var text = new IronTesseract().Read(imagePath).Text;");
            Console.WriteLine();
            Console.WriteLine("Download: https://ironsoftware.com/csharp/ocr/");
        }

        public void PlatformSupport()
        {
            Console.WriteLine("=== PLATFORM SUPPORT ===\n");

            Console.WriteLine("WINDOWS.MEDIA.OCR:");
            Console.WriteLine("  ✓ Windows 10");
            Console.WriteLine("  ✓ Windows 11");
            Console.WriteLine("  ✗ Windows Server");
            Console.WriteLine("  ✗ Linux");
            Console.WriteLine("  ✗ macOS");
            Console.WriteLine("  ✗ Docker");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  ✓ Windows 10/11");
            Console.WriteLine("  ✓ Windows Server");
            Console.WriteLine("  ✓ Linux (Ubuntu, Debian, etc.)");
            Console.WriteLine("  ✓ macOS");
            Console.WriteLine("  ✓ Docker");
            Console.WriteLine("  ✓ Azure/AWS/GCP");
            Console.WriteLine();
            Console.WriteLine("Download: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// NEED MORE THAN WINDOWS OCR?
//
// IronOCR provides everything Windows.Media.Ocr lacks:
// PDF support, cross-platform, preprocessing, and more.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
//
// Install: Install-Package IronOcr
// ============================================================================
