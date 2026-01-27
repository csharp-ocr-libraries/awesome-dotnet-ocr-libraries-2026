// =============================================================================
// XImage.OCR Basic OCR Examples
// =============================================================================
// Install: Install-Package RasterEdge.XImage.OCR
// Additional: Install-Package XImage.OCR.Language.English (per language)
// License: Commercial (RasterEdge)
// Platform: Windows primarily, .NET Standard 2.0, .NET Framework 4.5+
//
// CRITICAL: XImage.OCR requires SEPARATE NuGet packages for each language!
// Unlike IronOCR (125+ languages bundled), you must install:
//   - RasterEdge.XImage.OCR (core package)
//   - XImage.OCR.Language.English (for English)
//   - XImage.OCR.Language.German (for German)
//   - XImage.OCR.Language.French (for French)
//   - etc.
//
// NOTE: XImage.OCR wraps the open-source Tesseract engine. The same
// OCR technology is available free via charlesw/tesseract wrapper.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// RasterEdge XImage.OCR namespaces (conceptual - actual namespaces may vary)
// using RasterEdge.XImage.OCR;
// using RasterEdge.Imaging.Basic;

namespace XImageOcrExamples
{
    /// <summary>
    /// Demonstrates basic OCR operations with XImage.OCR.
    ///
    /// KEY CONSIDERATION: XImage.OCR is a commercial wrapper around the free
    /// Tesseract engine. Before purchasing, evaluate whether the commercial
    /// license provides sufficient value over free alternatives.
    /// </summary>
    public class BasicOcrExamples
    {
        /// <summary>
        /// Simple text extraction from an image file.
        ///
        /// PREREQUISITE: Install XImage.OCR.Language.English NuGet package
        /// This adds ~15-50MB to your deployment per language.
        /// </summary>
        public string ExtractTextSimple(string imagePath)
        {
            // License activation required before any OCR operations
            // RasterEdge.XImage.OCR.License.LicenseManager.SetLicense("your-license-key");

            /* XImage.OCR conceptual code:

            // Verify image file exists
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image file not found", imagePath);
            }

            // Create OCR handler instance
            var ocrHandler = new OCRHandler();

            // Set language (requires corresponding NuGet package installed)
            ocrHandler.Language = "eng";  // English

            // Perform OCR
            string result = ocrHandler.Process(imagePath);

            return result;
            */

            throw new NotImplementedException("XImage.OCR code - see comments");
        }

        /// <summary>
        /// Multi-language OCR demonstrating package fragmentation issue.
        ///
        /// CRITICAL: Each language requires a SEPARATE NuGet package!
        /// For 5 European languages, you need 6 NuGet packages:
        ///   - RasterEdge.XImage.OCR
        ///   - XImage.OCR.Language.English
        ///   - XImage.OCR.Language.German
        ///   - XImage.OCR.Language.French
        ///   - XImage.OCR.Language.Spanish
        ///   - XImage.OCR.Language.Italian
        ///
        /// Compare to IronOCR: Single package includes 125+ languages.
        /// </summary>
        public string ExtractMultiLanguage(string imagePath, string[] languages)
        {
            /* XImage.OCR multi-language setup:

            // FIRST: Ensure all language packages are installed via NuGet
            // Package Manager Console:
            // Install-Package RasterEdge.XImage.OCR
            // Install-Package XImage.OCR.Language.English
            // Install-Package XImage.OCR.Language.German
            // Install-Package XImage.OCR.Language.French
            // Install-Package XImage.OCR.Language.Spanish
            // Install-Package XImage.OCR.Language.Italian

            var ocrHandler = new OCRHandler();

            // Set multiple languages
            ocrHandler.Languages = languages;  // e.g., ["eng", "deu", "fra", "spa", "ita"]

            // Process image
            string result = ocrHandler.Process(imagePath);

            return result;
            */

            throw new NotImplementedException("XImage.OCR code - see comments");
        }

        /// <summary>
        /// Demonstrates the package version synchronization requirement.
        ///
        /// WARNING: All XImage.OCR packages must be on the same version!
        /// Version mismatch between core and language packages causes runtime errors.
        ///
        /// Example problematic configuration:
        /// <PackageReference Include="RasterEdge.XImage.OCR" Version="12.4.0" />
        /// <PackageReference Include="XImage.OCR.Language.English" Version="12.3.0" /> <!-- MISMATCH! -->
        /// </summary>
        public void DemonstrateVersionRequirement()
        {
            Console.WriteLine("XImage.OCR Package Version Requirements:");
            Console.WriteLine("=========================================");
            Console.WriteLine("");
            Console.WriteLine("All packages MUST be on the same version:");
            Console.WriteLine("  RasterEdge.XImage.OCR           12.4.0");
            Console.WriteLine("  XImage.OCR.Language.English     12.4.0");
            Console.WriteLine("  XImage.OCR.Language.German      12.4.0");
            Console.WriteLine("  XImage.OCR.Language.French      12.4.0");
            Console.WriteLine("");
            Console.WriteLine("Version mismatch causes:");
            Console.WriteLine("  - Runtime initialization errors");
            Console.WriteLine("  - OCR failures with unclear messages");
            Console.WriteLine("  - Deployment issues in CI/CD");
            Console.WriteLine("");
            Console.WriteLine("Compare to IronOCR: Single package, no version sync needed.");
        }

        /// <summary>
        /// Extract text with confidence score tracking.
        /// Confidence indicates reliability of OCR result (0.0 to 1.0).
        /// </summary>
        public OcrResultWithConfidence ExtractWithConfidence(string imagePath)
        {
            /* XImage.OCR conceptual code:

            var ocrHandler = new OCRHandler();
            ocrHandler.Language = "eng";

            // Some Tesseract wrappers expose confidence via the result
            var result = ocrHandler.ProcessWithMetadata(imagePath);

            return new OcrResultWithConfidence
            {
                Text = result.Text,
                Confidence = result.MeanConfidence,
                SourceFile = imagePath
            };
            */

            throw new NotImplementedException("XImage.OCR code - see comments");
        }

        /// <summary>
        /// Process image from byte array (web upload scenario).
        /// Useful when images arrive via HTTP request body.
        /// </summary>
        public string ExtractFromBytes(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                throw new ArgumentException("Image data cannot be null or empty");
            }

            /* XImage.OCR conceptual code:

            var ocrHandler = new OCRHandler();
            ocrHandler.Language = "eng";

            // Create image from bytes
            using var stream = new MemoryStream(imageData);
            using var image = Image.Load(stream);

            string result = ocrHandler.Process(image);
            return result;
            */

            throw new NotImplementedException("XImage.OCR code - see comments");
        }

        /// <summary>
        /// Extract text from specific region (zone OCR).
        /// Useful when document layout is known/fixed.
        /// </summary>
        public string ExtractFromRegion(string imagePath, int x, int y, int width, int height)
        {
            /* XImage.OCR conceptual code:

            var ocrHandler = new OCRHandler();
            ocrHandler.Language = "eng";

            // Define region of interest
            var region = new Rectangle(x, y, width, height);

            // Extract from region only
            string result = ocrHandler.ProcessRegion(imagePath, region);
            return result;
            */

            throw new NotImplementedException("XImage.OCR code - see comments");
        }

        /// <summary>
        /// Configure character whitelist for digit-only extraction.
        /// Useful for invoice numbers, amounts, dates.
        /// </summary>
        public string ExtractDigitsOnly(string imagePath)
        {
            /* XImage.OCR conceptual code (uses underlying Tesseract variables):

            var ocrHandler = new OCRHandler();
            ocrHandler.Language = "eng";

            // Restrict recognition to digits only
            ocrHandler.SetVariable("tessedit_char_whitelist", "0123456789");

            string result = ocrHandler.Process(imagePath);
            return result;
            */

            throw new NotImplementedException("XImage.OCR code - see comments");
        }
    }

    /// <summary>
    /// Result container with confidence score.
    /// </summary>
    public class OcrResultWithConfidence
    {
        public string Text { get; set; }
        public float Confidence { get; set; }
        public string SourceFile { get; set; }

        public bool IsHighConfidence => Confidence >= 0.85f;
        public bool IsLowConfidence => Confidence < 0.60f;
    }

    /// <summary>
    /// Demonstrates resource management with XImage.OCR.
    /// As a Tesseract wrapper, proper disposal is critical.
    /// </summary>
    public class ResourceManagementExamples
    {
        /// <summary>
        /// Proper disposal pattern for OCR handler.
        /// Failure to dispose causes memory leaks.
        /// </summary>
        public string ExtractWithProperDisposal(string imagePath)
        {
            /* XImage.OCR proper disposal:

            using (var ocrHandler = new OCRHandler())
            {
                ocrHandler.Language = "eng";
                return ocrHandler.Process(imagePath);
            }
            // Handler automatically disposed here
            */

            throw new NotImplementedException("XImage.OCR code - see comments");
        }

        /// <summary>
        /// Memory warning: each language loads ~40-100MB.
        /// Multiple simultaneous handlers multiply memory usage.
        /// </summary>
        public void MemoryConsiderations()
        {
            Console.WriteLine("XImage.OCR Memory Considerations:");
            Console.WriteLine("==================================");
            Console.WriteLine("");
            Console.WriteLine("Memory per language loaded: ~40-100MB");
            Console.WriteLine("");
            Console.WriteLine("Example - 5 languages loaded:");
            Console.WriteLine("  English:  ~50MB");
            Console.WriteLine("  German:   ~50MB");
            Console.WriteLine("  French:   ~45MB");
            Console.WriteLine("  Spanish:  ~45MB");
            Console.WriteLine("  Italian:  ~45MB");
            Console.WriteLine("  TOTAL:   ~235MB");
            Console.WriteLine("");
            Console.WriteLine("XImage.OCR (Tesseract-based) is NOT thread-safe.");
            Console.WriteLine("Each thread needs its own handler instance.");
            Console.WriteLine("4 threads x 235MB = 940MB minimum.");
        }
    }

    /// <summary>
    /// Demonstrates the package installation complexity.
    /// </summary>
    public static class PackageInstallationGuide
    {
        public static void DisplayInstallationSteps()
        {
            Console.WriteLine("XImage.OCR Package Installation:");
            Console.WriteLine("=================================");
            Console.WriteLine("");
            Console.WriteLine("Step 1: Install core package");
            Console.WriteLine("  dotnet add package RasterEdge.XImage.OCR");
            Console.WriteLine("");
            Console.WriteLine("Step 2: Install EACH required language");
            Console.WriteLine("  dotnet add package XImage.OCR.Language.English");
            Console.WriteLine("  dotnet add package XImage.OCR.Language.German");
            Console.WriteLine("  dotnet add package XImage.OCR.Language.French");
            Console.WriteLine("  dotnet add package XImage.OCR.Language.Spanish");
            Console.WriteLine("  dotnet add package XImage.OCR.Language.Italian");
            Console.WriteLine("  dotnet add package XImage.OCR.Language.Portuguese");
            Console.WriteLine("  dotnet add package XImage.OCR.Language.ChineseSimplified");
            Console.WriteLine("  dotnet add package XImage.OCR.Language.ChineseTraditional");
            Console.WriteLine("  dotnet add package XImage.OCR.Language.Japanese");
            Console.WriteLine("  dotnet add package XImage.OCR.Language.Korean");
            Console.WriteLine("  dotnet add package XImage.OCR.Language.Arabic");
            Console.WriteLine("");
            Console.WriteLine("Step 3: Activate license");
            Console.WriteLine("  RasterEdge.XImage.OCR.License.LicenseManager.SetLicense(\"key\");");
            Console.WriteLine("");
            Console.WriteLine("---");
            Console.WriteLine("");
            Console.WriteLine("Compare to IronOCR installation:");
            Console.WriteLine("  dotnet add package IronOcr");
            Console.WriteLine("  (125+ languages included automatically)");
        }
    }
}

// =============================================================================
// IronOCR Comparison: Single Package, 125+ Languages
// =============================================================================
// The equivalent IronOCR code demonstrates the simplicity advantage:
//
// using IronOcr;
//
// // Single package includes 125+ languages - no additional installs
// var ocr = new IronTesseract();
// ocr.Language = OcrLanguage.English + OcrLanguage.German + OcrLanguage.French;
//
// using var input = new OcrInput("document.png");
// input.Deskew();   // Built-in preprocessing
// input.DeNoise();  // Built-in preprocessing
//
// var result = ocr.Read(input);
// Console.WriteLine(result.Text);
//
// Key differences:
// - Single NuGet package (vs 10+ for XImage.OCR multi-language)
// - 125+ languages bundled (vs ~15 available for XImage.OCR)
// - Built-in preprocessing (XImage.OCR has none)
// - Thread-safe design (XImage.OCR requires per-thread instances)
// - Cross-platform (Windows, Linux, macOS, Docker)
// =============================================================================
