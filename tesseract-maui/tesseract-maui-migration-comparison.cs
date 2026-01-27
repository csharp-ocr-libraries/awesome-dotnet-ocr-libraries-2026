// TesseractOcrMaui vs IronOCR Migration Comparison
// This file demonstrates side-by-side comparisons for migrating from
// TesseractOcrMaui to IronOCR, highlighting key differences and improvements.
//
// KEY MIGRATION BENEFITS:
// 1. Cross-platform support (not MAUI-only)
// 2. Built-in PDF processing
// 3. Automatic image preprocessing
// 4. No traineddata management required
// 5. Commercial support available

using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

// TesseractOcrMaui namespaces (MAUI-only)
using TesseractOcrMaui;
using TesseractOcrMaui.Results;

// IronOCR namespaces (works everywhere)
using IronOcr;

namespace MigrationComparison
{
    // ============================================================================
    // COMPARISON 1: BASIC TEXT EXTRACTION
    // TesseractOcrMaui requires initialization, traineddata, MAUI context
    // IronOCR works standalone with 3 lines of code
    // ============================================================================

    /// <summary>
    /// TesseractOcrMaui basic extraction - complex setup required.
    /// </summary>
    public class TesseractMauiBasicExtraction
    {
        private readonly ITesseract _tesseract;
        private bool _isInitialized = false;

        // Requires DI from MAUI service registration
        public TesseractMauiBasicExtraction(ITesseract tesseract)
        {
            _tesseract = tesseract ?? throw new ArgumentNullException(nameof(tesseract));
        }

        public async Task<string> ExtractTextAsync(string imagePath)
        {
            // Must initialize first (traineddata must exist)
            if (!_isInitialized)
            {
                await _tesseract.InitAsync("eng");
                _isInitialized = true;
            }

            // Process image
            var result = await _tesseract.RecognizeTextAsync(imagePath);

            if (!result.Success)
            {
                throw new Exception($"OCR failed: {result.Status}");
            }

            return result.RecognizedText ?? string.Empty;
        }
    }

    /// <summary>
    /// IronOCR basic extraction - simple, works everywhere.
    /// </summary>
    public class IronOcrBasicExtraction
    {
        // No constructor dependencies needed!

        public string ExtractText(string imagePath)
        {
            // Just 3 lines - no initialization, no traineddata management
            var ocr = new IronTesseract();
            using var input = new OcrInput(imagePath);
            return ocr.Read(input).Text;
        }

        // Async version if needed
        public async Task<string> ExtractTextAsync(string imagePath)
        {
            return await Task.Run(() => ExtractText(imagePath));
        }
    }

    // ============================================================================
    // COMPARISON 2: PDF PROCESSING
    // TesseractOcrMaui: NOT SUPPORTED (requires external PDF library)
    // IronOCR: Built-in PDF support
    // ============================================================================

    /// <summary>
    /// TesseractOcrMaui PDF handling - requires external library and manual work.
    /// </summary>
    public class TesseractMauiPdfProcessing
    {
        private readonly ITesseract _tesseract;

        public TesseractMauiPdfProcessing(ITesseract tesseract)
        {
            _tesseract = tesseract;
        }

        public async Task<List<string>> ProcessPdfAsync(string pdfPath)
        {
            // TesseractOcrMaui cannot process PDFs directly
            // You must:
            // 1. Install a separate PDF library (PDFium, iTextSharp, etc.)
            // 2. Extract each page as an image
            // 3. Process each image individually
            // 4. Clean up temporary files

            throw new NotSupportedException(
                "TesseractOcrMaui does not support PDF processing. " +
                "You must manually convert PDF pages to images first using an external library.");

            /*
            // Example of what you'd have to do:
            var results = new List<string>();

            // Using some PDF library (not included in TesseractOcrMaui)
            using var pdfDocument = PdfLibrary.Open(pdfPath);

            for (int i = 0; i < pdfDocument.PageCount; i++)
            {
                // Render page to image
                var tempImagePath = Path.Combine(FileSystem.CacheDirectory, $"page_{i}.png");
                pdfDocument.RenderPage(i, tempImagePath, dpi: 300);

                // OCR the image
                await _tesseract.InitAsync("eng");
                var result = await _tesseract.RecognizeTextAsync(tempImagePath);

                if (result.Success)
                    results.Add(result.RecognizedText);

                // Clean up
                File.Delete(tempImagePath);
            }

            return results;
            */
        }
    }

    /// <summary>
    /// IronOCR PDF processing - built-in, simple.
    /// </summary>
    public class IronOcrPdfProcessing
    {
        public string ProcessPdf(string pdfPath)
        {
            // PDF support is built-in - just load and read
            var ocr = new IronTesseract();
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);
            return ocr.Read(input).Text;
        }

        public string ProcessPdfPages(string pdfPath, int startPage, int endPage)
        {
            // Process specific page range
            var ocr = new IronTesseract();
            using var input = new OcrInput();
            input.LoadPdfPages(pdfPath, startPage, endPage);
            return ocr.Read(input).Text;
        }

        public List<string> ProcessPdfByPage(string pdfPath)
        {
            // Get text from each page separately
            var results = new List<string>();
            var ocr = new IronTesseract();

            using var input = new OcrInput();
            input.LoadPdf(pdfPath);

            var result = ocr.Read(input);

            foreach (var page in result.Pages)
            {
                results.Add(page.Text);
            }

            return results;
        }
    }

    // ============================================================================
    // COMPARISON 3: IMAGE PREPROCESSING
    // TesseractOcrMaui: No preprocessing (you get raw Tesseract accuracy)
    // IronOCR: 40+ automatic image corrections
    // ============================================================================

    /// <summary>
    /// TesseractOcrMaui with preprocessing - YOU must implement it.
    /// </summary>
    public class TesseractMauiWithPreprocessing
    {
        private readonly ITesseract _tesseract;

        public TesseractMauiWithPreprocessing(ITesseract tesseract)
        {
            _tesseract = tesseract;
        }

        public async Task<string> ProcessWithPreprocessingAsync(string imagePath)
        {
            // TesseractOcrMaui has NO built-in preprocessing
            // For better accuracy, you would need to:

            // 1. Add a third-party image processing library (SkiaSharp, ImageSharp, etc.)
            // 2. Implement deskewing algorithms manually
            // 3. Implement denoising algorithms manually
            // 4. Implement contrast enhancement manually
            // 5. Implement resolution scaling manually
            // 6. Save processed image to temp file
            // 7. Process with Tesseract
            // 8. Clean up temp file

            // Most teams skip this due to complexity,
            // resulting in poor accuracy on real-world images

            await _tesseract.InitAsync("eng");
            var result = await _tesseract.RecognizeTextAsync(imagePath);

            return result.RecognizedText ?? string.Empty;
        }

        /*
        // Example of manual preprocessing you'd need to implement:
        private async Task<string> ManualPreprocess(string imagePath)
        {
            // This requires adding SkiaSharp or similar library
            using var originalBitmap = SKBitmap.Decode(imagePath);

            // Deskew - requires implementing Hough transform or similar
            var deskewAngle = CalculateDeskewAngle(originalBitmap);
            var deskewed = RotateImage(originalBitmap, deskewAngle);

            // Denoise - requires implementing Gaussian blur or median filter
            var denoised = ApplyDenoising(deskewed);

            // Enhance contrast - requires implementing histogram equalization
            var enhanced = EnhanceContrast(denoised);

            // Save to temp file
            var tempPath = Path.GetTempFileName() + ".png";
            enhanced.Encode(SKEncodedImageFormat.Png, 100);

            return tempPath;
        }

        // Each of these would be dozens of lines of complex image processing code
        private float CalculateDeskewAngle(SKBitmap bitmap) { ... }
        private SKBitmap RotateImage(SKBitmap bitmap, float angle) { ... }
        private SKBitmap ApplyDenoising(SKBitmap bitmap) { ... }
        private SKBitmap EnhanceContrast(SKBitmap bitmap) { ... }
        */
    }

    /// <summary>
    /// IronOCR with preprocessing - built-in filters.
    /// </summary>
    public class IronOcrWithPreprocessing
    {
        public string ProcessWithPreprocessing(string imagePath)
        {
            var ocr = new IronTesseract();
            using var input = new OcrInput(imagePath);

            // Built-in preprocessing - one line each
            input.Deskew();                    // Fix tilted scans
            input.DeNoise();                   // Remove image noise
            input.EnhanceResolution();         // Improve low-res images

            return ocr.Read(input).Text;
        }

        public string ProcessMobilePhoto(string imagePath)
        {
            // Optimized for mobile camera captures
            var ocr = new IronTesseract();
            using var input = new OcrInput(imagePath);

            // Apply all relevant mobile photo corrections
            input.Deskew();                    // Phone camera angle
            input.DeNoise();                   // Sensor noise
            input.Sharpen();                   // Focus issues
            input.EnhanceResolution(300);      // Ensure minimum DPI

            return ocr.Read(input).Text;
        }

        public string ProcessPoorQualityDocument(string imagePath)
        {
            var ocr = new IronTesseract();
            using var input = new OcrInput(imagePath);

            // Full preprocessing pipeline for difficult images
            input.Deskew();
            input.DeNoise();
            input.Binarize();                  // Convert to black/white
            input.EnhanceResolution(300);
            input.Dilate();                    // Thicken thin text
            input.Invert();                    // Handle white text on dark

            return ocr.Read(input).Text;
        }
    }

    // ============================================================================
    // COMPARISON 4: CROSS-PLATFORM CODE SHARING
    // TesseractOcrMaui: MAUI-only, cannot share with backend
    // IronOCR: Same code works everywhere
    // ============================================================================

    /// <summary>
    /// TesseractOcrMaui - locked to MAUI platform.
    /// </summary>
    public class TesseractMauiPlatformLimitation
    {
        // This code ONLY works in .NET MAUI apps
        // Cannot be used in:
        // - ASP.NET Core Web APIs
        // - Console applications
        // - Windows Forms/WPF
        // - Azure Functions
        // - AWS Lambda
        // - Docker containers

        private readonly ITesseract _tesseract; // MAUI DI only

        public TesseractMauiPlatformLimitation(ITesseract tesseract)
        {
            _tesseract = tesseract;
        }

        public async Task<string> ProcessAsync(string imagePath)
        {
            await _tesseract.InitAsync("eng");
            var result = await _tesseract.RecognizeTextAsync(imagePath);
            return result.RecognizedText ?? string.Empty;
        }
    }

    /// <summary>
    /// IronOCR - works on all .NET platforms.
    /// </summary>
    public class IronOcrCrossPlatform
    {
        // This SAME code works in:
        // - .NET MAUI (iOS, Android, Windows, Mac)
        // - ASP.NET Core Web APIs
        // - Console applications
        // - Windows Forms/WPF
        // - Azure Functions
        // - AWS Lambda
        // - Docker containers (Linux/Windows)
        // - Blazor Server

        public string Process(string imagePath)
        {
            var ocr = new IronTesseract();
            using var input = new OcrInput(imagePath);
            return ocr.Read(input).Text;
        }
    }

    /// <summary>
    /// Example: Shared OCR library for enterprise applications.
    /// This is IMPOSSIBLE with TesseractOcrMaui but trivial with IronOCR.
    /// </summary>
    public class SharedOcrLibrary
    {
        // This class can be in a shared .NET Standard or .NET 8 class library
        // and used by ALL your applications:

        private readonly IronTesseract _ocr;

        public SharedOcrLibrary()
        {
            _ocr = new IronTesseract();
        }

        public string ExtractText(string imagePath)
        {
            using var input = new OcrInput(imagePath);
            return _ocr.Read(input).Text;
        }

        public string ExtractTextFromPdf(string pdfPath)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);
            return _ocr.Read(input).Text;
        }

        public string ExtractTextFromBytes(byte[] imageData)
        {
            using var input = new OcrInput(imageData);
            return _ocr.Read(input).Text;
        }
    }

    /*
    // USAGE EXAMPLE - Same SharedOcrLibrary used everywhere:

    // In MAUI mobile app:
    var mobileService = new SharedOcrLibrary();
    var text = mobileService.ExtractText(photoPath);

    // In ASP.NET Core API:
    [HttpPost("ocr")]
    public IActionResult ProcessDocument(IFormFile file)
    {
        var apiService = new SharedOcrLibrary();
        using var stream = file.OpenReadStream();
        var bytes = ReadAllBytes(stream);
        return Ok(apiService.ExtractTextFromBytes(bytes));
    }

    // In Azure Function:
    [FunctionName("ProcessDocument")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function)] HttpRequest req)
    {
        var functionService = new SharedOcrLibrary();
        // Process...
    }
    */

    // ============================================================================
    // COMPARISON 5: MOBILE PACKAGES
    // TesseractOcrMaui: MAUI wrapper only
    // IronOCR: Dedicated IronOcr.Android and IronOcr.iOS packages
    // ============================================================================

    /// <summary>
    /// IronOCR mobile-specific packages for optimal mobile performance.
    /// </summary>
    public class IronOcrMobilePackages
    {
        /*
        INSTALLATION FOR MOBILE:

        For Android:
        dotnet add package IronOcr.Android

        For iOS:
        dotnet add package IronOcr.iOS

        These packages:
        - Are optimized for mobile memory constraints
        - Include native libraries for each platform
        - Work in MAUI, Xamarin, and native .NET Android/iOS
        - Provide full IronOCR feature set on mobile
        */

        public string ProcessOnMobile(string imagePath)
        {
            // Same API whether using IronOcr.Android or IronOcr.iOS
            var ocr = new IronTesseract();
            using var input = new OcrInput(imagePath);

            // Full preprocessing available on mobile too
            input.Deskew();
            input.DeNoise();

            return ocr.Read(input).Text;
        }

        public string ProcessPdfOnMobile(string pdfPath)
        {
            // PDF support works on mobile with IronOcr.iOS/Android
            // NOT available with TesseractOcrMaui
            var ocr = new IronTesseract();
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);
            return ocr.Read(input).Text;
        }
    }

    // ============================================================================
    // COMPARISON 6: TRAINEDDATA MANAGEMENT
    // TesseractOcrMaui: Manual download, bundle, path management
    // IronOCR: Everything built-in, nothing to manage
    // ============================================================================

    /// <summary>
    /// TesseractOcrMaui traineddata burden.
    /// </summary>
    public class TesseractMauiTraineddataManagement
    {
        // With TesseractOcrMaui, you must:

        // 1. Download traineddata files manually
        //    curl -L -o eng.traineddata https://github.com/tesseract-ocr/tessdata_best/raw/main/eng.traineddata

        // 2. Add to project in correct location
        //    Resources/Raw/tessdata/eng.traineddata

        // 3. Configure bundling in .csproj
        //    <MauiAsset Include="Resources\Raw\tessdata\*.traineddata" />

        // 4. Handle platform-specific paths
        //    iOS bundle path differs from Android assets

        // 5. Verify files exist at runtime
        //    Common source of crashes

        // 6. Update when Tesseract version changes
        //    Version mismatch causes failures

        // 7. Manage app size for each language
        //    ~50MB per language for "best" quality

        public async Task<bool> ValidateTraineddataAsync(ITesseract tesseract)
        {
            try
            {
                // Attempt to initialize - will fail if traineddata missing
                await tesseract.InitAsync("eng");
                return true;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("ERROR: eng.traineddata not found!");
                Console.WriteLine("Download from: https://github.com/tesseract-ocr/tessdata_best");
                Console.WriteLine("Place in: Resources/Raw/tessdata/");
                Console.WriteLine("Add to .csproj: <MauiAsset Include=\"Resources\\Raw\\tessdata\\*.traineddata\" />");
                return false;
            }
        }
    }

    /// <summary>
    /// IronOCR - nothing to manage.
    /// </summary>
    public class IronOcrNoTraineddataManagement
    {
        public string Process(string imagePath)
        {
            // No traineddata downloads needed
            // No file bundling required
            // No path configuration
            // No version management
            // Just install and use

            var ocr = new IronTesseract();
            using var input = new OcrInput(imagePath);
            return ocr.Read(input).Text;
        }

        public string ProcessMultipleLanguages(string imagePath)
        {
            // Multiple languages just work
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.EnglishBest + OcrLanguage.FrenchBest;

            using var input = new OcrInput(imagePath);
            return ocr.Read(input).Text;
        }
    }

    // ============================================================================
    // COMPARISON 7: ERROR HANDLING
    // TesseractOcrMaui: Many failure points require handling
    // IronOCR: Simpler error surface
    // ============================================================================

    /// <summary>
    /// TesseractOcrMaui error handling complexity.
    /// </summary>
    public class TesseractMauiErrorHandling
    {
        private readonly ITesseract _tesseract;

        public TesseractMauiErrorHandling(ITesseract tesseract)
        {
            _tesseract = tesseract;
        }

        public async Task<(bool success, string result, string error)> SafeProcessAsync(string imagePath)
        {
            try
            {
                // May fail: traineddata not found
                await _tesseract.InitAsync("eng");
            }
            catch (FileNotFoundException)
            {
                return (false, "", "Traineddata file not found");
            }
            catch (DllNotFoundException)
            {
                return (false, "", "Native Tesseract library missing");
            }

            try
            {
                // May fail: image format not supported
                // May fail: image too large
                // May fail: out of memory
                // May fail: native library crash
                var result = await _tesseract.RecognizeTextAsync(imagePath);

                if (!result.Success)
                {
                    return (false, "", $"OCR failed: {result.Status}");
                }

                return (true, result.RecognizedText ?? "", "");
            }
            catch (OutOfMemoryException)
            {
                return (false, "", "Image too large for available memory");
            }
            catch (Exception ex)
            {
                return (false, "", $"Unexpected error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// IronOCR error handling - simpler.
    /// </summary>
    public class IronOcrErrorHandling
    {
        public (bool success, string result, string error) SafeProcess(string imagePath)
        {
            try
            {
                var ocr = new IronTesseract();
                using var input = new OcrInput(imagePath);
                var result = ocr.Read(input);

                return (true, result.Text, "");
            }
            catch (Exception ex)
            {
                return (false, "", ex.Message);
            }
        }
    }

    // ============================================================================
    // MIGRATION SUMMARY
    // ============================================================================

    /// <summary>
    /// Summary of migration benefits.
    /// </summary>
    public static class MigrationSummary
    {
        /*
        MIGRATION FROM TesseractOcrMaui TO IronOCR:

        BEFORE (TesseractOcrMaui):
        - MAUI-only deployment
        - Manual traineddata management
        - No PDF support
        - No preprocessing
        - Complex initialization
        - Community support only
        - Single developer maintenance

        AFTER (IronOCR):
        - All .NET platforms supported
        - Everything built-in
        - Native PDF support
        - 40+ preprocessing filters
        - 3-line initialization
        - Commercial support SLA
        - Company-backed development

        CODE REDUCTION:
        - Setup code: 100% eliminated
        - Processing code: 85% reduction
        - Error handling: 70% simpler
        - Total codebase: 60-80% smaller

        MIGRATION STEPS:
        1. Remove TesseractOcrMaui package
        2. Add IronOcr.Android and/or IronOcr.iOS
        3. Delete tessdata folder and files
        4. Remove MAUI service registration
        5. Replace API calls (see examples above)
        6. Remove traineddata validation code
        7. Add preprocessing for better accuracy
        */

        public static void PrintMigrationGuide()
        {
            Console.WriteLine("=== TesseractOcrMaui to IronOCR Migration ===");
            Console.WriteLine();
            Console.WriteLine("Step 1: Remove old package");
            Console.WriteLine("  dotnet remove package TesseractOcrMaui");
            Console.WriteLine();
            Console.WriteLine("Step 2: Add new packages");
            Console.WriteLine("  dotnet add package IronOcr.Android  # or IronOcr.iOS");
            Console.WriteLine();
            Console.WriteLine("Step 3: Delete tessdata folder");
            Console.WriteLine("  rm -rf Resources/Raw/tessdata");
            Console.WriteLine();
            Console.WriteLine("Step 4: Update code");
            Console.WriteLine("  - Replace ITesseract with IronTesseract");
            Console.WriteLine("  - Remove InitAsync calls");
            Console.WriteLine("  - Use OcrInput instead of file paths");
            Console.WriteLine("  - Add preprocessing filters");
            Console.WriteLine();
            Console.WriteLine("Step 5: Remove from MauiProgram.cs");
            Console.WriteLine("  - Delete: builder.Services.AddTesseractOcr();");
            Console.WriteLine();
            Console.WriteLine("Done! Your OCR now works cross-platform.");
        }
    }
}
