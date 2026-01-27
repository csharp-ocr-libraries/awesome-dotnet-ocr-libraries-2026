// =============================================================================
// PaddleOCR Basic OCR Operations
// =============================================================================
// Demonstrates basic text extraction with Sdcb.PaddleOCR deep learning wrapper
//
// Install required packages:
//   dotnet add package Sdcb.PaddleOCR
//   dotnet add package Sdcb.PaddleOCR.Models.Online
//   dotnet add package Sdcb.PaddleInference.runtime.win64.mkl
//   dotnet add package OpenCvSharp4
//   dotnet add package OpenCvSharp4.runtime.win
//
// NOTE: PaddleOCR requires downloading model files (~100MB) on first run
// NOTE: Model download connects to Baidu servers in China
// =============================================================================

using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Online;
using OpenCvSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PaddleOcrExamples
{
    /// <summary>
    /// Basic PaddleOCR operations demonstrating the deep learning OCR workflow
    /// </summary>
    public class BasicOcrOperations
    {
        // =================================================================
        // Example 1: Minimal Text Extraction
        // =================================================================
        public static async Task<string> SimpleTextExtraction(string imagePath)
        {
            // Step 1: Download models (first run only - downloads from Baidu servers)
            // This is a key difference from IronOCR which bundles models in NuGet
            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();

            // Step 2: Create the OCR engine with models
            // PaddleOcrAll combines detection, classification, and recognition
            using PaddleOcrAll ocr = new PaddleOcrAll(models)
            {
                // Enable rotation detection (0, 90, 180, 270 degrees)
                AllowRotateDetection = true,
                // Enable 180-degree classification for upside-down text
                Enable180Classification = true
            };

            // Step 3: Load image using OpenCvSharp (required dependency)
            // Unlike IronOCR which accepts file paths directly, PaddleOCR needs Mat
            using Mat mat = Cv2.ImRead(imagePath);

            if (mat.Empty())
            {
                throw new FileNotFoundException($"Could not load image: {imagePath}");
            }

            // Step 4: Run OCR - this executes three neural networks in sequence:
            // 1. Text detection (DB model)
            // 2. Text direction classification
            // 3. Text recognition (CRNN model)
            PaddleOcrResult result = ocr.Run(mat);

            // Step 5: Return the concatenated text from all regions
            return result.Text;
        }

        // =================================================================
        // Example 2: Processing with Region Details
        // =================================================================
        public static async Task ProcessWithRegionDetails(string imagePath)
        {
            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();

            using PaddleOcrAll ocr = new PaddleOcrAll(models);
            using Mat mat = Cv2.ImRead(imagePath);

            PaddleOcrResult result = ocr.Run(mat);

            Console.WriteLine($"Detected {result.Regions.Length} text regions:\n");

            // Each region contains:
            // - Text: The recognized text
            // - Score: Confidence score (0-1)
            // - Rect: Bounding rectangle
            foreach (PaddleOcrResultRegion region in result.Regions)
            {
                Console.WriteLine($"Text: {region.Text}");
                Console.WriteLine($"  Confidence: {region.Score:P1}");
                Console.WriteLine($"  Location: ({region.Rect.Center.X:F0}, {region.Rect.Center.Y:F0})");
                Console.WriteLine($"  Size: {region.Rect.Size.Width:F0} x {region.Rect.Size.Height:F0}");
                Console.WriteLine();
            }
        }

        // =================================================================
        // Example 3: Confidence-Based Filtering
        // =================================================================
        public static async Task<string> HighConfidenceTextOnly(string imagePath, double minConfidence = 0.8)
        {
            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();

            using PaddleOcrAll ocr = new PaddleOcrAll(models);
            using Mat mat = Cv2.ImRead(imagePath);

            PaddleOcrResult result = ocr.Run(mat);

            // Filter regions by confidence threshold
            // This helps eliminate false positives in noisy images
            var highConfidenceRegions = result.Regions
                .Where(r => r.Score >= minConfidence)
                .OrderBy(r => r.Rect.Center.Y)  // Sort by vertical position
                .ThenBy(r => r.Rect.Center.X);  // Then by horizontal position

            return string.Join("\n", highConfidenceRegions.Select(r => r.Text));
        }

        // =================================================================
        // Example 4: English-Only Model
        // =================================================================
        public static async Task<string> EnglishTextExtraction(string imagePath)
        {
            // Use English model instead of Chinese (smaller, faster)
            // Still limited compared to IronOCR's 125+ languages
            FullOcrModel models = await OnlineFullModels.EnglishV4.DownloadAsync();

            using PaddleOcrAll ocr = new PaddleOcrAll(models);
            using Mat mat = Cv2.ImRead(imagePath);

            PaddleOcrResult result = ocr.Run(mat);

            return result.Text;
        }

        // =================================================================
        // Example 5: Batch Processing Multiple Images
        // =================================================================
        public static async Task BatchProcessImages(string[] imagePaths)
        {
            // Download models once, reuse for all images
            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();

            // PaddleOcrAll is thread-safe (as of v2.7.0.2)
            using PaddleOcrAll ocr = new PaddleOcrAll(models);

            Console.WriteLine($"Processing {imagePaths.Length} images...\n");

            foreach (string path in imagePaths)
            {
                try
                {
                    using Mat mat = Cv2.ImRead(path);

                    if (mat.Empty())
                    {
                        Console.WriteLine($"[SKIP] {Path.GetFileName(path)}: Could not load");
                        continue;
                    }

                    PaddleOcrResult result = ocr.Run(mat);

                    Console.WriteLine($"[OK] {Path.GetFileName(path)}:");
                    Console.WriteLine($"     Regions: {result.Regions.Length}");
                    Console.WriteLine($"     Preview: {result.Text.Substring(0, Math.Min(50, result.Text.Length))}...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {Path.GetFileName(path)}: {ex.Message}");
                }
            }
        }

        // =================================================================
        // Example 6: Local Models (No Download Required)
        // =================================================================
        public static string ProcessWithLocalModels(string imagePath, string modelDirectory)
        {
            // For air-gapped environments or to avoid Baidu server connections,
            // download models manually and load from local directory
            //
            // Expected structure:
            //   {modelDirectory}/
            //     ch_PP-OCRv4_det_infer/
            //       inference.pdmodel
            //       inference.pdiparams
            //     ch_PP-OCRv4_rec_infer/
            //       inference.pdmodel
            //       inference.pdiparams
            //     ch_ppocr_mobile_v2.0_cls_infer/
            //       inference.pdmodel
            //       inference.pdiparams

            FullOcrModel models = new FullOcrModel(
                LocalDetectionModel.FromDirectory(Path.Combine(modelDirectory, "ch_PP-OCRv4_det_infer")),
                LocalClassificationModel.FromDirectory(Path.Combine(modelDirectory, "ch_ppocr_mobile_v2.0_cls_infer")),
                LocalRecognitionModel.FromDirectory(Path.Combine(modelDirectory, "ch_PP-OCRv4_rec_infer"))
            );

            using PaddleOcrAll ocr = new PaddleOcrAll(models);
            using Mat mat = Cv2.ImRead(imagePath);

            PaddleOcrResult result = ocr.Run(mat);

            return result.Text;
        }

        // =================================================================
        // Example 7: Proper Resource Disposal Pattern
        // =================================================================
        public static async Task ProperDisposalPattern(string imagePath)
        {
            // Models don't need disposal but Mat and PaddleOcrAll do
            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();

            // Ensure proper disposal of native resources
            PaddleOcrAll? ocr = null;
            Mat? mat = null;

            try
            {
                ocr = new PaddleOcrAll(models);
                mat = Cv2.ImRead(imagePath);

                if (!mat.Empty())
                {
                    PaddleOcrResult result = ocr.Run(mat);
                    Console.WriteLine(result.Text);
                }
            }
            finally
            {
                // Dispose in reverse order of creation
                mat?.Dispose();
                ocr?.Dispose();
            }
        }

        // =================================================================
        // Usage Example
        // =================================================================
        public static async Task Main(string[] args)
        {
            string testImage = args.Length > 0 ? args[0] : "test-document.png";

            Console.WriteLine("PaddleOCR Basic Operations Demo\n");
            Console.WriteLine("================================\n");

            // Check if test image exists
            if (!File.Exists(testImage))
            {
                Console.WriteLine($"Test image not found: {testImage}");
                Console.WriteLine("Please provide an image path as argument.");
                return;
            }

            Console.WriteLine("Downloading models (first run only)...\n");

            string text = await SimpleTextExtraction(testImage);

            Console.WriteLine("Extracted Text:");
            Console.WriteLine("---------------");
            Console.WriteLine(text);
        }
    }
}
