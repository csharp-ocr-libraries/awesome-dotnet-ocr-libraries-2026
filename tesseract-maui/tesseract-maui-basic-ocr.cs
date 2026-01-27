// TesseractOcrMaui Basic OCR Examples
// Install: dotnet add package TesseractOcrMaui
//
// CRITICAL REQUIREMENTS:
// 1. This library ONLY works in .NET MAUI applications
// 2. You must download traineddata files separately
// 3. Traineddata must be bundled as MauiAsset in your .csproj
//
// This file demonstrates basic TesseractOcrMaui usage patterns including
// initialization, processing, camera integration, and error handling.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Media;
using TesseractOcrMaui;
using TesseractOcrMaui.Results;

namespace TesseractOcrMauiExamples
{
    // ============================================================================
    // MAUI PROGRAM SETUP - Required registration
    // Add this to MauiProgram.cs
    // ============================================================================
    /*
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // REQUIRED: Register TesseractOcrMaui services
            builder.Services.AddTesseractOcr();

            return builder.Build();
        }
    }
    */

    // ============================================================================
    // PROJECT FILE SETUP - Required for traineddata bundling
    // Add this to your .csproj file
    // ============================================================================
    /*
    <ItemGroup>
        <!-- Bundle traineddata files as MAUI assets -->
        <MauiAsset Include="Resources\Raw\tessdata\*.traineddata" />
    </ItemGroup>
    */

    /// <summary>
    /// Basic OCR service using TesseractOcrMaui.
    /// Note: This service ONLY works within .NET MAUI applications.
    /// For cross-platform needs, see IronOCR examples below.
    /// </summary>
    public class BasicMauiOcrService
    {
        private readonly ITesseract _tesseract;

        // Constructor with dependency injection from MAUI service registration
        public BasicMauiOcrService(ITesseract tesseract)
        {
            _tesseract = tesseract ?? throw new ArgumentNullException(nameof(tesseract));
        }

        /// <summary>
        /// Initialize the OCR engine with specified language.
        /// Must be called before processing any images.
        /// </summary>
        public async Task<bool> InitializeAsync(string language = "eng")
        {
            try
            {
                // Initialize Tesseract with language
                // Traineddata file must exist in app bundle
                await _tesseract.InitAsync(language);
                return true;
            }
            catch (Exception ex)
            {
                // Common errors:
                // - Traineddata file not found
                // - Traineddata file corrupted
                // - Language not supported
                Console.WriteLine($"OCR initialization failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extract text from an image file.
        /// </summary>
        public async Task<OcrResultData> ExtractTextAsync(string imagePath)
        {
            var result = new OcrResultData();

            try
            {
                // Validate image exists
                if (!File.Exists(imagePath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Image not found: {imagePath}";
                    return result;
                }

                // Process image with Tesseract
                var ocrResult = await _tesseract.RecognizeTextAsync(imagePath);

                if (ocrResult.Success)
                {
                    result.Success = true;
                    result.Text = ocrResult.RecognizedText ?? string.Empty;
                    result.Confidence = ocrResult.Confidence;
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"OCR failed with status: {ocrResult.Status}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Exception during OCR: {ex.Message}";
            }

            return result;
        }
    }

    /// <summary>
    /// Data class for OCR results with error handling.
    /// </summary>
    public class OcrResultData
    {
        public bool Success { get; set; }
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    // ============================================================================
    // CAMERA INTEGRATION EXAMPLE
    // Capture photo and process with OCR
    // ============================================================================

    /// <summary>
    /// Camera-integrated OCR service for mobile document capture.
    /// </summary>
    public class CameraOcrService
    {
        private readonly ITesseract _tesseract;

        public CameraOcrService(ITesseract tesseract)
        {
            _tesseract = tesseract;
        }

        /// <summary>
        /// Capture photo from camera and extract text.
        /// Uses MAUI MediaPicker for camera access.
        /// </summary>
        public async Task<OcrResultData> CaptureAndProcessAsync()
        {
            var result = new OcrResultData();

            try
            {
                // Check if camera is available
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    result.Success = false;
                    result.ErrorMessage = "Camera capture not supported on this device";
                    return result;
                }

                // Capture photo
                var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "Capture document for OCR"
                });

                if (photo == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "No photo captured";
                    return result;
                }

                // Save photo to temporary file
                var tempPath = Path.Combine(FileSystem.CacheDirectory, $"ocr_{Guid.NewGuid()}.jpg");

                using (var sourceStream = await photo.OpenReadAsync())
                using (var destStream = File.Create(tempPath))
                {
                    await sourceStream.CopyToAsync(destStream);
                }

                // Initialize and process
                await _tesseract.InitAsync("eng");
                var ocrResult = await _tesseract.RecognizeTextAsync(tempPath);

                // Clean up temp file
                try { File.Delete(tempPath); } catch { }

                if (ocrResult.Success)
                {
                    result.Success = true;
                    result.Text = ocrResult.RecognizedText ?? string.Empty;
                    result.Confidence = ocrResult.Confidence;
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"OCR processing failed: {ocrResult.Status}";
                }
            }
            catch (FeatureNotSupportedException)
            {
                result.Success = false;
                result.ErrorMessage = "Camera feature not available";
            }
            catch (PermissionException)
            {
                result.Success = false;
                result.ErrorMessage = "Camera permission denied";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Pick existing photo from gallery and process.
        /// </summary>
        public async Task<OcrResultData> PickAndProcessAsync()
        {
            var result = new OcrResultData();

            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Select document for OCR"
                });

                if (photo == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "No photo selected";
                    return result;
                }

                // Get file path
                var filePath = photo.FullPath;

                // Initialize and process
                await _tesseract.InitAsync("eng");
                var ocrResult = await _tesseract.RecognizeTextAsync(filePath);

                result.Success = ocrResult.Success;
                result.Text = ocrResult.RecognizedText ?? string.Empty;
                result.Confidence = ocrResult.Confidence;

                if (!ocrResult.Success)
                {
                    result.ErrorMessage = $"OCR failed: {ocrResult.Status}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error: {ex.Message}";
            }

            return result;
        }
    }

    // ============================================================================
    // MULTI-LANGUAGE SUPPORT
    // Processing documents in different languages
    // ============================================================================

    /// <summary>
    /// Multi-language OCR service.
    /// Each language requires its own traineddata file bundled with the app.
    /// </summary>
    public class MultiLanguageOcrService
    {
        private readonly ITesseract _tesseract;

        // Supported languages must have traineddata bundled
        private readonly string[] _availableLanguages = { "eng", "fra", "deu", "spa", "ita" };

        public MultiLanguageOcrService(ITesseract tesseract)
        {
            _tesseract = tesseract;
        }

        /// <summary>
        /// Extract text in a specific language.
        /// </summary>
        public async Task<OcrResultData> ExtractInLanguageAsync(string imagePath, string language)
        {
            var result = new OcrResultData();

            // Validate language is available
            if (!_availableLanguages.Contains(language.ToLower()))
            {
                result.Success = false;
                result.ErrorMessage = $"Language '{language}' not available. " +
                    $"Supported: {string.Join(", ", _availableLanguages)}";
                return result;
            }

            try
            {
                // Initialize with specified language
                await _tesseract.InitAsync(language);

                var ocrResult = await _tesseract.RecognizeTextAsync(imagePath);

                result.Success = ocrResult.Success;
                result.Text = ocrResult.RecognizedText ?? string.Empty;
                result.Confidence = ocrResult.Confidence;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error processing {language}: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Attempt OCR with multiple languages and return best result.
        /// Useful when document language is unknown.
        /// </summary>
        public async Task<OcrResultData> ExtractBestLanguageAsync(string imagePath)
        {
            var bestResult = new OcrResultData { Confidence = -1 };

            foreach (var language in _availableLanguages)
            {
                var langResult = await ExtractInLanguageAsync(imagePath, language);

                if (langResult.Success && langResult.Confidence > bestResult.Confidence)
                {
                    bestResult = langResult;
                }
            }

            if (bestResult.Confidence < 0)
            {
                bestResult.Success = false;
                bestResult.ErrorMessage = "No language produced valid results";
            }

            return bestResult;
        }
    }

    // ============================================================================
    // TRAINEDDATA VALIDATION HELPER
    // Verify traineddata files are correctly bundled
    // ============================================================================

    /// <summary>
    /// Helper to validate traineddata files are properly bundled.
    /// Call on app startup to detect deployment issues early.
    /// </summary>
    public class TraineddataValidator
    {
        /// <summary>
        /// Validate traineddata files exist in expected locations.
        /// Platform-specific paths are handled by MAUI asset system.
        /// </summary>
        public async Task<ValidationResult> ValidateAsync(params string[] languages)
        {
            var result = new ValidationResult { IsValid = true };

            foreach (var lang in languages)
            {
                try
                {
                    // Attempt to open traineddata file via MAUI file system
                    var fileName = $"tessdata/{lang}.traineddata";

                    using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);

                    if (stream == null || stream.Length == 0)
                    {
                        result.IsValid = false;
                        result.MissingFiles.Add(fileName);
                    }
                }
                catch (FileNotFoundException)
                {
                    result.IsValid = false;
                    result.MissingFiles.Add($"{lang}.traineddata");
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.Errors.Add($"{lang}: {ex.Message}");
                }
            }

            return result;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> MissingFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    // ============================================================================
    // ERROR HANDLING PATTERNS
    // Common TesseractOcrMaui errors and how to handle them
    // ============================================================================

    /// <summary>
    /// Error-resilient OCR service with comprehensive error handling.
    /// </summary>
    public class ResilientOcrService
    {
        private readonly ITesseract _tesseract;
        private bool _initialized = false;

        public ResilientOcrService(ITesseract tesseract)
        {
            _tesseract = tesseract;
        }

        /// <summary>
        /// Safe initialization with detailed error reporting.
        /// </summary>
        public async Task<(bool success, string error)> SafeInitializeAsync(string language = "eng")
        {
            if (_initialized)
                return (true, string.Empty);

            try
            {
                await _tesseract.InitAsync(language);
                _initialized = true;
                return (true, string.Empty);
            }
            catch (FileNotFoundException)
            {
                return (false, $"Traineddata file not found for language '{language}'. " +
                    $"Ensure {language}.traineddata is bundled in Resources/Raw/tessdata/");
            }
            catch (DllNotFoundException)
            {
                return (false, "Native Tesseract library not found. " +
                    "Ensure TesseractOcrMaui is correctly installed for your platform.");
            }
            catch (InvalidOperationException ex)
            {
                return (false, $"Tesseract initialization failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.GetType().Name} - {ex.Message}");
            }
        }

        /// <summary>
        /// Safe text extraction with detailed error categorization.
        /// </summary>
        public async Task<OcrResultData> SafeExtractAsync(string imagePath)
        {
            var result = new OcrResultData();

            // Ensure initialized
            if (!_initialized)
            {
                var (success, error) = await SafeInitializeAsync();
                if (!success)
                {
                    result.Success = false;
                    result.ErrorMessage = error;
                    return result;
                }
            }

            // Validate input file
            if (string.IsNullOrEmpty(imagePath))
            {
                result.Success = false;
                result.ErrorMessage = "Image path is empty";
                return result;
            }

            if (!File.Exists(imagePath))
            {
                result.Success = false;
                result.ErrorMessage = $"Image file not found: {imagePath}";
                return result;
            }

            // Check file size (very large images may cause memory issues)
            var fileInfo = new FileInfo(imagePath);
            if (fileInfo.Length > 50 * 1024 * 1024) // 50MB limit
            {
                result.Success = false;
                result.ErrorMessage = "Image file too large (max 50MB)";
                return result;
            }

            try
            {
                var ocrResult = await _tesseract.RecognizeTextAsync(imagePath);

                result.Success = ocrResult.Success;
                result.Text = ocrResult.RecognizedText ?? string.Empty;
                result.Confidence = ocrResult.Confidence;

                if (!ocrResult.Success)
                {
                    result.ErrorMessage = $"OCR processing failed: {ocrResult.Status}";
                }
            }
            catch (OutOfMemoryException)
            {
                result.Success = false;
                result.ErrorMessage = "Out of memory processing image. Try a smaller image.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Processing error: {ex.Message}";
            }

            return result;
        }
    }
}
