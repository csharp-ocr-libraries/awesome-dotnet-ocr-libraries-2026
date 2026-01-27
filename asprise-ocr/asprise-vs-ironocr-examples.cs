/**
 * Asprise OCR vs IronOCR: Code Examples
 *
 * Compare Asprise OCR with IronOCR.
 * Asprise has limited .NET support; IronOCR is built for .NET.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * Asprise: Java-focused, .NET via interop
 * IronOCR NuGet: https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.Runtime.InteropServices;

// ============================================================================
// ASPRISE OCR IMPLEMENTATION - Java-Focused Library
// ============================================================================

namespace AspriseExamples
{
    /// <summary>
    /// Asprise OCR considerations:
    /// 1. Primarily Java-focused
    /// 2. .NET support via COM interop or wrapper
    /// 3. Separate licensing model
    /// 4. Limited .NET-specific features
    /// </summary>
    public class AspriseOcrService
    {
        /*
        // Asprise uses COM interop or a thin wrapper
        [DllImport("aocr.dll")]
        private static extern IntPtr OCR(string imagePath, int type);

        public string ExtractText(string imagePath)
        {
            // Complex interop setup required
            IntPtr result = OCR(imagePath, 0);
            return Marshal.PtrToStringAnsi(result);
        }
        */

        public void ShowLimitations()
        {
            Console.WriteLine("Asprise OCR for .NET:");
            Console.WriteLine("1. Java SDK is primary focus");
            Console.WriteLine("2. .NET support is secondary");
            Console.WriteLine("3. COM interop may be required");
            Console.WriteLine("4. Limited PDF support");
            Console.WriteLine("5. No NuGet package available");
            Console.WriteLine();
            Console.WriteLine("For native .NET OCR, use IronOCR:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - NATIVE .NET OCR LIBRARY
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR is built specifically for .NET:
    /// - Pure C# API
    /// - NuGet installation
    /// - Full .NET Standard 2.0 support
    /// - Works on .NET Framework, .NET Core, .NET 5+
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Simple text extraction - native .NET
        /// </summary>
        public string ExtractText(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// PDF OCR - no interop, pure .NET
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// With preprocessing - fluent API
        /// </summary>
        public string ExtractWithPreprocessing(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();
            input.Contrast();

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Multi-language support
        /// </summary>
        public string ExtractMultiLanguage(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);

            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Create searchable PDF
        /// </summary>
        public void CreateSearchablePdf(string inputPath, string outputPath)
        {
            var result = new IronTesseract().Read(inputPath);
            result.SaveAsSearchablePdf(outputPath);
        }

        /// <summary>
        /// Read from stream - common .NET pattern
        /// </summary>
        public string ExtractFromStream(System.IO.Stream imageStream)
        {
            using var input = new OcrInput();
            input.LoadImage(imageStream);
            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Read from byte array
        /// </summary>
        public string ExtractFromBytes(byte[] imageBytes)
        {
            using var input = new OcrInput();
            input.LoadImage(imageBytes);
            return new IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// COMPARISON
// ============================================================================

namespace Comparison
{
    public class AspriseVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== ASPRISE vs IRONOCR ===\n");

            Console.WriteLine("Feature          | Asprise        | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("Primary platform | Java           | .NET");
            Console.WriteLine(".NET support     | Secondary      | Native");
            Console.WriteLine("NuGet package    | No             | Yes");
            Console.WriteLine("PDF support      | Limited        | Native");
            Console.WriteLine("Preprocessing    | Manual         | Built-in");
            Console.WriteLine("Searchable PDF   | No             | Yes");
            Console.WriteLine("API style        | Interop        | Fluent C#");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public void WhyChooseNativeDotNet()
        {
            Console.WriteLine("=== WHY NATIVE .NET MATTERS ===\n");

            Console.WriteLine("Benefits of IronOCR's native .NET approach:");
            Console.WriteLine("1. No interop overhead");
            Console.WriteLine("2. Full async/await support");
            Console.WriteLine("3. Works with .NET streams and byte arrays");
            Console.WriteLine("4. Proper exception handling");
            Console.WriteLine("5. IntelliSense and IDE support");
            Console.WriteLine("6. NuGet package management");
            Console.WriteLine("7. Cross-platform (.NET Core, .NET 5+)");
            Console.WriteLine();
            Console.WriteLine("Download: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// FOR NATIVE .NET OCR, CHOOSE IRONOCR
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Install: Install-Package IronOcr
// ============================================================================
