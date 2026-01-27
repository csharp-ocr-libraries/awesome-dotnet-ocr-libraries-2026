// =============================================================================
// Tesseract.Net.SDK Basic OCR Examples
// =============================================================================
// Install: Install-Package Tesseract.Net.SDK
// License: Commercial (Patagames)
// Platform: Windows ONLY (.NET Framework 2.0-4.5)
// Requirements:
//   - tessdata folder must exist in bin/Debug/ or bin/Release/
//   - Download traineddata files from https://github.com/tesseract-ocr/tessdata
//   - Windows operating system (no Linux/macOS support)
// =============================================================================

using System;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using Patagames.Ocr;

namespace TesseractNetSdkExamples
{
    /// <summary>
    /// Demonstrates basic OCR operations with Tesseract.Net.SDK.
    /// WARNING: This library is Windows-only and .NET Framework 2.0-4.5 only.
    /// For cross-platform or modern .NET support, see IronOCR examples.
    /// </summary>
    public class BasicOcrExamples
    {
        // tessdata must be in bin/Debug/tessdata/ or bin/Release/tessdata/
        private const string TessdataPath = @".\tessdata";

        /// <summary>
        /// Simple text extraction from an image file.
        /// Requires: eng.traineddata in tessdata folder.
        /// </summary>
        public string ExtractTextSimple(string imagePath)
        {
            // Platform check - Tesseract.Net.SDK is Windows-only
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException(
                    "Tesseract.Net.SDK only supports Windows. " +
                    "For cross-platform OCR, consider IronOCR.");
            }

            // Verify tessdata exists
            if (!Directory.Exists(TessdataPath))
            {
                throw new DirectoryNotFoundException(
                    $"tessdata folder not found at {TessdataPath}. " +
                    "Download traineddata files from GitHub.");
            }

            // Create OCR API instance
            using (var api = OcrApi.Create())
            {
                // Initialize with English language
                // This loads eng.traineddata (~40MB) into memory
                api.Init(Languages.English);

                // Perform OCR on image
                string text = api.GetTextFromImage(imagePath);

                return text;
            }
        }

        /// <summary>
        /// Extract text with confidence score.
        /// Confidence indicates how reliable the OCR result is (0.0 to 1.0).
        /// </summary>
        public OcrResult ExtractWithConfidence(string imagePath)
        {
            // Validate input
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image file not found", imagePath);
            }

            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English);

                // Load image explicitly for more control
                using (var img = OcrImage.FromFile(imagePath))
                {
                    api.SetImage(img);

                    // Get text and confidence
                    string text = api.GetText();
                    float confidence = api.GetMeanConfidence();

                    return new OcrResult
                    {
                        Text = text,
                        Confidence = confidence,
                        SourceFile = imagePath
                    };
                }
            }
        }

        /// <summary>
        /// Multi-language OCR for documents with mixed text.
        /// CRITICAL: All required traineddata files must be in tessdata folder.
        /// </summary>
        public string ExtractMultiLanguage(string imagePath, params Languages[] languages)
        {
            // Verify each language file exists
            // Common traineddata files:
            // - eng.traineddata (English)
            // - deu.traineddata (German)
            // - fra.traineddata (French)
            // - spa.traineddata (Spanish)

            using (var api = OcrApi.Create())
            {
                // Combine languages with bitwise OR
                Languages combined = Languages.English; // Start with default
                foreach (var lang in languages)
                {
                    combined |= lang;
                }

                api.Init(combined);

                string text = api.GetTextFromImage(imagePath);
                return text;
            }
        }

        /// <summary>
        /// Process image from byte array instead of file path.
        /// Useful for web applications receiving image uploads.
        /// </summary>
        public string ExtractFromBytes(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                throw new ArgumentException("Image data cannot be null or empty");
            }

            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English);

                // Create OcrImage from byte array
                using (var stream = new MemoryStream(imageData))
                using (var bitmap = new Bitmap(stream))
                using (var img = OcrImage.FromBitmap(bitmap))
                {
                    api.SetImage(img);
                    return api.GetText();
                }
            }
        }

        /// <summary>
        /// Extract text from a specific region of the image.
        /// Useful when you know where text is located.
        /// </summary>
        public string ExtractFromRegion(string imagePath, Rectangle region)
        {
            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English);

                using (var img = OcrImage.FromFile(imagePath))
                {
                    // Set the region of interest
                    api.SetImage(img);
                    api.SetRectangle(region.X, region.Y, region.Width, region.Height);

                    return api.GetText();
                }
            }
        }

        /// <summary>
        /// Configure OCR for digits only (receipts, invoices).
        /// Restricts recognition to numeric characters.
        /// </summary>
        public string ExtractDigitsOnly(string imagePath)
        {
            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English);

                // Set character whitelist to digits only
                api.SetVariable("tessedit_char_whitelist", "0123456789");

                string text = api.GetTextFromImage(imagePath);
                return text;
            }
        }

        /// <summary>
        /// Configure OCR to exclude specific characters.
        /// Useful when certain characters are known to cause errors.
        /// </summary>
        public string ExtractWithBlacklist(string imagePath, string blacklistChars)
        {
            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English);

                // Characters to never return (common OCR confusions)
                // Example: "|" often confused with "l" or "1"
                api.SetVariable("tessedit_char_blacklist", blacklistChars);

                string text = api.GetTextFromImage(imagePath);
                return text;
            }
        }
    }

    /// <summary>
    /// Simple result container for OCR output.
    /// </summary>
    public class OcrResult
    {
        public string Text { get; set; }
        public float Confidence { get; set; }
        public string SourceFile { get; set; }

        public bool IsHighConfidence => Confidence >= 0.85f;
        public bool IsLowConfidence => Confidence < 0.60f;
    }

    /// <summary>
    /// Demonstrates proper resource management with Tesseract.Net.SDK.
    /// Memory leaks are common when disposal is not handled correctly.
    /// </summary>
    public class ResourceManagementExamples
    {
        /// <summary>
        /// INCORRECT: Memory leak - OcrApi not disposed.
        /// </summary>
        public string BadExample_MemoryLeak(string imagePath)
        {
            // WARNING: This leaks memory!
            var api = OcrApi.Create();
            api.Init(Languages.English);
            return api.GetTextFromImage(imagePath);
            // api is never disposed - memory leak!
        }

        /// <summary>
        /// CORRECT: Using statement ensures disposal.
        /// </summary>
        public string GoodExample_ProperDisposal(string imagePath)
        {
            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English);
                return api.GetTextFromImage(imagePath);
            }
            // api is automatically disposed here
        }

        /// <summary>
        /// CORRECT: Try-finally pattern for older .NET versions.
        /// </summary>
        public string GoodExample_TryFinally(string imagePath)
        {
            OcrApi api = null;
            try
            {
                api = OcrApi.Create();
                api.Init(Languages.English);
                return api.GetTextFromImage(imagePath);
            }
            finally
            {
                if (api != null)
                {
                    api.Dispose();
                }
            }
        }

        /// <summary>
        /// IMPORTANT: Creating multiple engines multiplies memory usage.
        /// Each engine loads ~40-100MB per language.
        /// </summary>
        public void MemoryWarning_MultipleEngines()
        {
            // WARNING: This uses 160-400MB of memory for English alone!
            var api1 = OcrApi.Create();
            var api2 = OcrApi.Create();
            var api3 = OcrApi.Create();
            var api4 = OcrApi.Create();

            // Each Init() loads the full language model
            api1.Init(Languages.English);
            api2.Init(Languages.English);
            api3.Init(Languages.English);
            api4.Init(Languages.English);

            // Always dispose when done
            api1.Dispose();
            api2.Dispose();
            api3.Dispose();
            api4.Dispose();
        }
    }
}

// =============================================================================
// IronOCR Comparison: Basic Text Extraction
// =============================================================================
// The equivalent IronOCR code is significantly simpler and cross-platform:
//
// using IronOcr;
//
// var ocr = new IronTesseract();
// using var input = new OcrInput("document.png");
// var result = ocr.Read(input);
// Console.WriteLine(result.Text);
// Console.WriteLine($"Confidence: {result.Confidence:P0}");
//
// Key differences:
// - No tessdata management (languages auto-download)
// - Cross-platform (Windows, Linux, macOS, Docker)
// - Modern .NET support (.NET Core, .NET 5+)
// - Thread-safe (single instance for all threads)
// =============================================================================
