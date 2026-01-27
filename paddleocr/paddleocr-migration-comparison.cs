// =============================================================================
// PaddleOCR to IronOCR Migration Comparison
// =============================================================================
// Side-by-side examples demonstrating the migration from Sdcb.PaddleOCR to IronOCR
//
// This file shows Before (PaddleOCR) and After (IronOCR) for common scenarios,
// highlighting the paradigm shift from deep learning OCR to optimized traditional OCR.
//
// PaddleOCR packages:
//   dotnet add package Sdcb.PaddleOCR
//   dotnet add package Sdcb.PaddleOCR.Models.Online
//   dotnet add package Sdcb.PaddleInference.runtime.win64.mkl
//   dotnet add package OpenCvSharp4
//   dotnet add package OpenCvSharp4.runtime.win
//
// IronOCR package:
//   dotnet add package IronOcr
// =============================================================================

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

// PaddleOCR namespaces (Before)
// using Sdcb.PaddleOCR;
// using Sdcb.PaddleOCR.Models.Online;
// using OpenCvSharp;

// IronOCR namespaces (After)
// using IronOcr;

namespace PaddleOcrMigrationExamples
{
    /// <summary>
    /// Migration comparison: PaddleOCR deep learning vs IronOCR traditional OCR
    /// Each section shows Before/After code with explanations
    /// </summary>
    public class MigrationComparison
    {
        // =================================================================
        // SCENARIO 1: Basic Text Extraction
        // =================================================================

        /// <summary>
        /// PaddleOCR: Basic text extraction requires model download and OpenCV
        /// </summary>
        public static async Task<string> PaddleOcr_BasicExtraction(string imagePath)
        {
            // BEFORE: PaddleOCR (5 packages, async model download required)
            /*
            using Sdcb.PaddleOCR;
            using Sdcb.PaddleOCR.Models.Online;
            using OpenCvSharp;

            // Step 1: Download models from Baidu servers (~100MB, first run only)
            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();

            // Step 2: Create OCR engine with models
            using PaddleOcrAll ocr = new PaddleOcrAll(models)
            {
                AllowRotateDetection = true,
                Enable180Classification = true
            };

            // Step 3: Load image using OpenCvSharp (required dependency)
            using Mat mat = Cv2.ImRead(imagePath);

            if (mat.Empty())
            {
                throw new FileNotFoundException($"Could not load image: {imagePath}");
            }

            // Step 4: Run deep learning inference (3 neural networks)
            PaddleOcrResult result = ocr.Run(mat);

            return result.Text;
            */

            // Placeholder for compilation
            await Task.CompletedTask;
            return $"[PaddleOCR] Would process: {imagePath}";
        }

        /// <summary>
        /// IronOCR: Same task with simpler API, no model downloads
        /// </summary>
        public static string IronOcr_BasicExtraction(string imagePath)
        {
            // AFTER: IronOCR (1 package, synchronous, no model download)
            /*
            using IronOcr;

            // Step 1: Create OCR engine (no model download needed)
            var ocr = new IronTesseract();

            // Step 2: Load and process image in one step
            using var input = new OcrInput(imagePath);

            // Step 3: Run OCR (optimized Tesseract 5 LSTM)
            var result = ocr.Read(input);

            return result.Text;
            */

            // Placeholder for compilation
            return $"[IronOCR] Would process: {imagePath}";
        }

        /*
         * KEY DIFFERENCES - Basic Extraction:
         *
         * | Aspect              | PaddleOCR                    | IronOCR                    |
         * |---------------------|------------------------------|----------------------------|
         * | Packages            | 5 NuGet packages             | 1 NuGet package            |
         * | Model download      | ~100MB async download        | Bundled in package         |
         * | Image loading       | Requires OpenCvSharp         | Direct file path           |
         * | First run delay     | 10-30 seconds (download)     | None                       |
         * | Lines of code       | 10-15                        | 4-5                        |
         */


        // =================================================================
        // SCENARIO 2: Model Management vs Auto-Initialization
        // =================================================================

        /// <summary>
        /// PaddleOCR: Explicit model management with multiple options
        /// </summary>
        public static async Task PaddleOcr_ModelManagement()
        {
            // BEFORE: PaddleOCR requires explicit model handling
            /*
            using Sdcb.PaddleOCR;
            using Sdcb.PaddleOCR.Models;
            using Sdcb.PaddleOCR.Models.Online;

            // Option A: Online download (connects to Baidu servers)
            FullOcrModel onlineModels = await OnlineFullModels.ChineseV4.DownloadAsync();

            // Option B: Local pre-downloaded models
            FullOcrModel localModels = new FullOcrModel(
                LocalDetectionModel.FromDirectory("models/ch_PP-OCRv4_det_infer"),
                LocalClassificationModel.FromDirectory("models/ch_ppocr_mobile_v2.0_cls_infer"),
                LocalRecognitionModel.FromDirectory("models/ch_PP-OCRv4_rec_infer")
            );

            // Option C: Embedded model package (adds 100MB to deployment)
            // dotnet add package Sdcb.PaddleOCR.Models.LocalV4

            // Different models for different languages
            FullOcrModel englishModels = await OnlineFullModels.EnglishV4.DownloadAsync();
            FullOcrModel japaneseModels = await OnlineFullModels.JapanV4.DownloadAsync();
            */

            await Task.CompletedTask;
        }

        /// <summary>
        /// IronOCR: No model management needed - languages auto-download
        /// </summary>
        public static void IronOcr_AutoInitialization()
        {
            // AFTER: IronOCR handles everything automatically
            /*
            using IronOcr;

            var ocr = new IronTesseract();

            // Languages auto-download on first use (from Iron Software, not Baidu)
            // Or pre-install via NuGet:
            // dotnet add package IronOcr.Languages.Japanese
            // dotnet add package IronOcr.Languages.Chinese

            ocr.Language = OcrLanguage.English;
            // or: OcrLanguage.English + OcrLanguage.Japanese + OcrLanguage.French

            // No model path configuration needed
            // No async initialization required
            // 125+ languages available
            */
        }

        /*
         * KEY DIFFERENCES - Model Management:
         *
         * PaddleOCR:
         * - Must choose online/local/embedded models
         * - Model versions must match wrapper version
         * - Model files not bundled (separate download)
         * - 14 languages available
         * - Downloads from Baidu servers in China
         *
         * IronOCR:
         * - Models bundled in NuGet package
         * - Auto-download from Iron Software servers
         * - 125+ languages available
         * - No version matching concerns
         * - US-based infrastructure
         */


        // =================================================================
        // SCENARIO 3: GPU Processing vs CPU-Optimized
        // =================================================================

        /// <summary>
        /// PaddleOCR: GPU configuration for performance
        /// </summary>
        public static async Task PaddleOcr_GpuProcessing(string imagePath)
        {
            // BEFORE: PaddleOCR GPU requires extensive setup
            /*
            // Prerequisites:
            // 1. NVIDIA GPU with CUDA capability
            // 2. CUDA Toolkit 11.8 installed
            // 3. cuDNN 8.6+ installed
            // 4. GPU runtime package:
            //    dotnet add package Sdcb.PaddleInference.runtime.win64.cuda118

            using Sdcb.PaddleOCR;
            using Sdcb.PaddleOCR.Models.Online;
            using Sdcb.PaddleInference;
            using OpenCvSharp;

            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();

            // Configure GPU device
            using PaddleOcrAll ocr = new PaddleOcrAll(models, PaddleDevice.Gpu(deviceId: 0))
            {
                AllowRotateDetection = true,
                Enable180Classification = true
            };

            using Mat mat = Cv2.ImRead(imagePath);
            PaddleOcrResult result = ocr.Run(mat);

            // GPU provides 5-10x speedup:
            // CPU: ~300ms per image
            // GPU: ~60ms per image
            */

            await Task.CompletedTask;
        }

        /// <summary>
        /// IronOCR: CPU-optimized performance without GPU complexity
        /// </summary>
        public static void IronOcr_CpuOptimized(string imagePath)
        {
            // AFTER: IronOCR optimized for CPU, no GPU setup needed
            /*
            using IronOcr;

            var ocr = new IronTesseract();

            // Enable multi-threading for CPU optimization
            ocr.MultiThreaded = true;

            using var input = new OcrInput(imagePath);
            var result = ocr.Read(input);

            // IronOCR on CPU: ~150-300ms per image
            // Comparable to PaddleOCR CPU, no GPU complexity
            */
        }

        /*
         * KEY DIFFERENCES - Performance:
         *
         * | Configuration       | PaddleOCR CPU | PaddleOCR GPU | IronOCR CPU |
         * |---------------------|---------------|---------------|-------------|
         * | Setup time          | 30 minutes    | 4-8 hours     | 5 minutes   |
         * | Per-image time      | 300-500ms     | 50-100ms      | 150-300ms   |
         * | Cold start          | 3-5 seconds   | 5-10 seconds  | <1 second   |
         * | Memory usage        | 500MB-1GB     | 1-2GB         | 100-200MB   |
         * | Maintenance         | Medium        | High          | Low         |
         */


        // =================================================================
        // SCENARIO 4: Language Support
        // =================================================================

        /// <summary>
        /// PaddleOCR: Limited to 14 pre-trained languages
        /// </summary>
        public static async Task PaddleOcr_LanguageSupport()
        {
            // BEFORE: PaddleOCR has 14 language models
            /*
            using Sdcb.PaddleOCR.Models.Online;

            // Available languages (complete list):
            var chinese = await OnlineFullModels.ChineseV4.DownloadAsync();
            var english = await OnlineFullModels.EnglishV4.DownloadAsync();
            var french = await OnlineFullModels.FrenchV4.DownloadAsync();
            var german = await OnlineFullModels.GermanV4.DownloadAsync();
            var korean = await OnlineFullModels.KoreanV4.DownloadAsync();
            var japanese = await OnlineFullModels.JapanV4.DownloadAsync();
            // ... plus Italian, Spanish, Portuguese, Russian, Arabic, Hindi, Tamil

            // NOT AVAILABLE in PaddleOCR:
            // Polish, Dutch, Swedish, Norwegian, Finnish, Greek, Turkish,
            // Vietnamese, Thai, Hebrew, and 100+ other languages
            */

            await Task.CompletedTask;
        }

        /// <summary>
        /// IronOCR: 125+ languages with auto-download
        /// </summary>
        public static void IronOcr_LanguageSupport()
        {
            // AFTER: IronOCR supports 125+ languages
            /*
            using IronOcr;

            var ocr = new IronTesseract();

            // All major languages supported:
            ocr.Language = OcrLanguage.Polish;
            ocr.Language = OcrLanguage.Dutch;
            ocr.Language = OcrLanguage.Vietnamese;
            ocr.Language = OcrLanguage.Greek;
            ocr.Language = OcrLanguage.Turkish;
            ocr.Language = OcrLanguage.Hebrew;
            ocr.Language = OcrLanguage.Thai;
            // ... 118+ more languages

            // Multi-language detection:
            ocr.Language = OcrLanguage.English + OcrLanguage.French + OcrLanguage.German;
            */
        }


        // =================================================================
        // SCENARIO 5: PDF Processing
        // =================================================================

        /// <summary>
        /// PaddleOCR: No native PDF support - requires manual conversion
        /// </summary>
        public static async Task<string> PaddleOcr_PdfProcessing(string pdfPath)
        {
            // BEFORE: PaddleOCR requires manual PDF-to-image conversion
            /*
            using Sdcb.PaddleOCR;
            using Sdcb.PaddleOCR.Models.Online;
            using OpenCvSharp;
            using PdfiumViewer;  // Additional NuGet package needed
            using System.Drawing.Imaging;
            using System.Text;

            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();
            using PaddleOcrAll ocr = new PaddleOcrAll(models);

            var results = new StringBuilder();

            // Step 1: Load PDF with external library
            using PdfDocument pdf = PdfDocument.Load(pdfPath);

            // Step 2: Convert each page to image
            for (int i = 0; i < pdf.PageCount; i++)
            {
                // Render page to bitmap (200 DPI)
                using var pageImage = pdf.Render(i, 200, 200, PdfRenderFlags.CorrectFromDpi);

                // Save to temp file (OpenCvSharp can't load from memory easily)
                string tempPath = Path.GetTempFileName() + ".png";
                pageImage.Save(tempPath, ImageFormat.Png);

                try
                {
                    // Load with OpenCV and process
                    using Mat mat = Cv2.ImRead(tempPath);
                    var result = ocr.Run(mat);
                    results.AppendLine($"--- Page {i + 1} ---");
                    results.AppendLine(result.Text);
                }
                finally
                {
                    File.Delete(tempPath);
                }
            }

            return results.ToString();
            */

            await Task.CompletedTask;
            return $"[PaddleOCR] Would process PDF: {pdfPath}";
        }

        /// <summary>
        /// IronOCR: Native PDF support
        /// </summary>
        public static string IronOcr_PdfProcessing(string pdfPath)
        {
            // AFTER: IronOCR has native PDF support
            /*
            using IronOcr;

            var ocr = new IronTesseract();

            // Direct PDF input - no external libraries needed
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);

            var result = ocr.Read(input);

            // Access individual pages if needed
            foreach (var page in result.Pages)
            {
                Console.WriteLine($"Page {page.PageNumber}: {page.Text.Length} characters");
            }

            return result.Text;
            */

            return $"[IronOCR] Would process PDF: {pdfPath}";
        }

        /*
         * KEY DIFFERENCES - PDF Processing:
         *
         * PaddleOCR:
         * - No native PDF support
         * - Requires PdfiumViewer or similar library
         * - Manual page-by-page conversion
         * - Temp file management
         * - 30+ lines of code
         *
         * IronOCR:
         * - Native PDF support
         * - Single method call
         * - Page-level results available
         * - No temp files
         * - 5 lines of code
         */


        // =================================================================
        // SCENARIO 6: Deployment Comparison
        // =================================================================

        /// <summary>
        /// Shows deployment requirements for both libraries
        /// </summary>
        public static void DeploymentComparison()
        {
            Console.WriteLine(@"
=================================================================
DEPLOYMENT COMPARISON
=================================================================

PaddleOCR Deployment Files:
---------------------------
YourApp.dll
YourApp.deps.json
paddle_inference.dll            (~200MB)
paddle2onnx.dll                 (~5MB)
opencv_world490.dll             (~30MB)
opencv_videoio_ffmpeg*.dll      (~20MB)
models/
  ch_PP-OCRv4_det_infer/
    inference.pdmodel           (~3MB)
    inference.pdiparams         (~2MB)
  ch_PP-OCRv4_rec_infer/
    inference.pdmodel           (~10MB)
    inference.pdiparams         (~5MB)
  ch_ppocr_mobile_v2.0_cls_infer/
    inference.pdmodel           (~1MB)
    inference.pdiparams         (~1MB)

TOTAL: 300-500MB per deployment


IronOCR Deployment Files:
-------------------------
YourApp.dll
YourApp.deps.json
IronOcr.dll                     (~20MB)
IronOcr.Native.*                (~60MB, platform-specific)

TOTAL: ~80MB per deployment


Docker Image Comparison:
------------------------

PaddleOCR Dockerfile:
  FROM mcr.microsoft.com/dotnet/aspnet:8.0
  RUN apt-get update && apt-get install -y \
      libgdiplus \
      libopencv-dev
  COPY models/ /app/models/
  COPY . /app
  # Final image: ~1.5GB

IronOCR Dockerfile:
  FROM mcr.microsoft.com/dotnet/aspnet:8.0
  COPY . /app
  # Final image: ~400MB

=================================================================
");
        }


        // =================================================================
        // SCENARIO 7: Full Migration Checklist
        // =================================================================

        public static void MigrationChecklist()
        {
            Console.WriteLine(@"
=================================================================
PADDLEOCR TO IRONOCR MIGRATION CHECKLIST
=================================================================

PACKAGE CHANGES:
----------------
[ ] Remove: Sdcb.PaddleOCR
[ ] Remove: Sdcb.PaddleOCR.Models.Online (or .LocalV4)
[ ] Remove: Sdcb.PaddleInference.runtime.*
[ ] Remove: OpenCvSharp4
[ ] Remove: OpenCvSharp4.runtime.*
[ ] Add: IronOcr

CODE CHANGES:
-------------
[ ] Replace 'using Sdcb.PaddleOCR' with 'using IronOcr'
[ ] Remove 'using OpenCvSharp'
[ ] Replace 'PaddleOcrAll' with 'IronTesseract'
[ ] Replace 'Mat mat = Cv2.ImRead(path)' with 'new OcrInput(path)'
[ ] Remove async model download (OnlineFullModels.*.DownloadAsync())
[ ] Replace 'ocr.Run(mat)' with 'ocr.Read(input)'
[ ] Replace 'result.Regions' with 'result.Words' or 'result.Lines'
[ ] Replace 'region.Score' with 'word.Confidence'
[ ] Replace 'region.Rect' with 'word.X, Y, Width, Height'
[ ] Add 'input.LoadPdf()' for PDF files (replaces PdfiumViewer)

CONFIGURATION:
--------------
[ ] Remove model path configuration
[ ] Add language configuration if needed: ocr.Language = OcrLanguage.*
[ ] Enable multi-threading if needed: ocr.MultiThreaded = true

DEPLOYMENT:
-----------
[ ] Delete models/ directory
[ ] Delete paddle_inference.dll
[ ] Delete opencv_*.dll files
[ ] Update Dockerfile (remove apt-get, model copies)
[ ] Update CI/CD (remove model download steps)

TESTING:
--------
[ ] Test with representative documents
[ ] Verify accuracy meets requirements
[ ] Compare processing speed
[ ] Validate in Docker/production environment

=================================================================
");
        }


        // =================================================================
        // Usage Example
        // =================================================================
        public static async Task Main(string[] args)
        {
            Console.WriteLine("PaddleOCR to IronOCR Migration Guide\n");
            Console.WriteLine("====================================\n");

            // Show deployment comparison
            DeploymentComparison();

            // Show migration checklist
            MigrationChecklist();

            // If image provided, show both approaches
            if (args.Length > 0 && File.Exists(args[0]))
            {
                string imagePath = args[0];

                Console.WriteLine("\nProcessing with both libraries:\n");

                string paddleResult = await PaddleOcr_BasicExtraction(imagePath);
                Console.WriteLine(paddleResult);

                string ironResult = IronOcr_BasicExtraction(imagePath);
                Console.WriteLine(ironResult);
            }
        }
    }
}
