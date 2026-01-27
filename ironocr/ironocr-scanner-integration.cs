/**
 * IronOCR Scanner Integration with TWAIN: Complete Guide
 *
 * Connect IronOCR to document scanners using TWAIN or WIA.
 * Works with Aspose.Imaging TWAIN, NTwain, and other scanner libraries.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * For TWAIN: Install-Package NTwain
 * Alternative: Aspose.Imaging (commercial) or WIA (Windows built-in)
 */

using System;
using System.Drawing;
using System.IO;
using IronOcr;

namespace IronOcrScannerIntegration
{
    // ========================================================================
    // NTWAIN SCANNER INTEGRATION (Open Source)
    // https://github.com/soukoku/ntwain
    // ========================================================================

    /*
    using NTwain;
    using NTwain.Data;

    public class TwainScannerService
    {
        private readonly IronTesseract _ocr = new IronTesseract();

        /// <summary>
        /// Scan and OCR using NTwain (open source TWAIN library)
        /// </summary>
        public string ScanAndOcr()
        {
            // Initialize TWAIN session
            var appId = TWAINAppId.Create(
                "Your Company",
                "1.0",
                "Your Application"
            );

            using var session = new TwainSession(appId);
            session.Open();

            // Select scanner
            var source = session.ShowSourceSelector();
            if (source == null) return null;

            source.Open();

            // Configure scanner settings
            source.Capabilities.ICapXResolution.SetValue(300);
            source.Capabilities.ICapYResolution.SetValue(300);
            source.Capabilities.ICapPixelType.SetValue(PixelType.Gray);

            // Capture image
            Bitmap scannedImage = null;
            source.DataTransferred += (s, e) =>
            {
                if (e.NativeData != IntPtr.Zero)
                {
                    scannedImage = Bitmap.FromHbitmap(e.NativeData);
                }
            };

            source.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero);

            // OCR the scanned image
            if (scannedImage != null)
            {
                using var ms = new MemoryStream();
                scannedImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                using var input = new OcrInput();
                input.LoadImage(ms.ToArray());
                input.Deskew();  // Auto-correct any rotation
                input.DeNoise(); // Remove scanner noise

                return _ocr.Read(input).Text;
            }

            return null;
        }

        /// <summary>
        /// Scan multiple pages and create searchable PDF
        /// </summary>
        public void ScanToSearchablePdf(string outputPath)
        {
            using var input = new OcrInput();

            // Scan multiple pages
            while (HasMorePages())
            {
                var pageImage = ScanPage();
                if (pageImage != null)
                {
                    using var ms = new MemoryStream();
                    pageImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    input.LoadImage(ms.ToArray());
                }
            }

            // Apply preprocessing
            input.Deskew();
            input.DeNoise();

            // Create searchable PDF
            var result = _ocr.Read(input);
            result.SaveAsSearchablePdf(outputPath);
        }

        private bool HasMorePages() => false; // Implement based on scanner
        private Bitmap ScanPage() => null;    // Implement scan logic
    }
    */

    // ========================================================================
    // WIA (WINDOWS IMAGE ACQUISITION) INTEGRATION
    // Built into Windows - no additional packages needed
    // ========================================================================

    /*
    using WIA;

    public class WiaScannerService
    {
        private readonly IronTesseract _ocr = new IronTesseract();

        /// <summary>
        /// Scan using Windows Image Acquisition (WIA)
        /// Built into Windows - no extra packages needed
        /// </summary>
        public string ScanAndOcr()
        {
            // Create WIA device manager
            var deviceManager = new DeviceManager();

            // Find scanner
            DeviceInfo scannerDevice = null;
            foreach (DeviceInfo info in deviceManager.DeviceInfos)
            {
                if (info.Type == WiaDeviceType.ScannerDeviceType)
                {
                    scannerDevice = info;
                    break;
                }
            }

            if (scannerDevice == null)
            {
                throw new Exception("No scanner found");
            }

            // Connect to scanner
            var device = scannerDevice.Connect();
            var scannerItem = device.Items[1];

            // Set properties for 300 DPI grayscale
            SetProperty(scannerItem.Properties, "6147", 300); // Horizontal DPI
            SetProperty(scannerItem.Properties, "6148", 300); // Vertical DPI

            // Scan image
            var imageFile = (ImageFile)scannerItem.Transfer();

            // Convert to byte array for IronOCR
            var imageBytes = (byte[])imageFile.FileData.get_BinaryData();

            // OCR with preprocessing
            using var input = new OcrInput();
            input.LoadImage(imageBytes);
            input.Deskew();
            input.DeNoise();

            return _ocr.Read(input).Text;
        }

        private void SetProperty(IProperties properties, string propId, object value)
        {
            foreach (Property prop in properties)
            {
                if (prop.PropertyID.ToString() == propId)
                {
                    prop.set_Value(value);
                    return;
                }
            }
        }
    }
    */

    // ========================================================================
    // GENERIC SCANNER WORKFLOW WITH IRONOCR
    // ========================================================================

    public class ScannerOcrWorkflow
    {
        private readonly IronTesseract _ocr = new IronTesseract();

        /// <summary>
        /// Process scanned image from any source
        /// </summary>
        public string ProcessScannedImage(byte[] imageBytes)
        {
            using var input = new OcrInput();
            input.LoadImage(imageBytes);

            // Scanner-optimized preprocessing
            input.Deskew();           // Fix rotation from scanner
            input.DeNoise();          // Remove scanner artifacts
            input.Contrast();         // Enhance faded documents
            input.EnhanceResolution(300); // Ensure optimal DPI

            return _ocr.Read(input).Text;
        }

        /// <summary>
        /// Process scanned image file
        /// </summary>
        public string ProcessScannedFile(string filePath)
        {
            using var input = new OcrInput();
            input.LoadImage(filePath);

            input.Deskew();
            input.DeNoise();

            return _ocr.Read(input).Text;
        }

        /// <summary>
        /// Batch process scanned folder
        /// </summary>
        public void ProcessScannedFolder(string folderPath, string outputFolder)
        {
            var imageFiles = Directory.GetFiles(folderPath, "*.tif")
                .Concat(Directory.GetFiles(folderPath, "*.jpg"))
                .Concat(Directory.GetFiles(folderPath, "*.png"));

            foreach (var file in imageFiles)
            {
                Console.WriteLine($"Processing: {Path.GetFileName(file)}");

                using var input = new OcrInput();
                input.LoadImage(file);
                input.Deskew();
                input.DeNoise();

                var result = _ocr.Read(input);

                // Save as searchable PDF
                var outputPath = Path.Combine(outputFolder,
                    Path.GetFileNameWithoutExtension(file) + ".pdf");
                result.SaveAsSearchablePdf(outputPath);

                // Also save text
                var textPath = Path.Combine(outputFolder,
                    Path.GetFileNameWithoutExtension(file) + ".txt");
                File.WriteAllText(textPath, result.Text);

                Console.WriteLine($"  Confidence: {result.Confidence}%");
            }
        }

        /// <summary>
        /// Watch folder for new scans
        /// </summary>
        public void WatchScanFolder(string folderPath, Action<string> onTextExtracted)
        {
            var watcher = new FileSystemWatcher(folderPath)
            {
                Filter = "*.*",
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) =>
            {
                // Wait for file to be fully written
                System.Threading.Thread.Sleep(1000);

                if (IsImageFile(e.FullPath))
                {
                    try
                    {
                        var text = ProcessScannedFile(e.FullPath);
                        onTextExtracted(text);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {e.Name}: {ex.Message}");
                    }
                }
            };

            Console.WriteLine($"Watching {folderPath} for new scans...");
            Console.ReadLine();
        }

        private bool IsImageFile(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            return ext == ".jpg" || ext == ".png" || ext == ".tif" || ext == ".bmp";
        }
    }

    // ========================================================================
    // SCANNER BEST PRACTICES
    // ========================================================================

    public class ScannerBestPractices
    {
        public void ShowRecommendations()
        {
            Console.WriteLine("=== SCANNER + IRONOCR BEST PRACTICES ===\n");

            Console.WriteLine("SCAN SETTINGS:");
            Console.WriteLine("  - Resolution: 300 DPI (optimal for OCR)");
            Console.WriteLine("  - Color mode: Grayscale (faster, better OCR)");
            Console.WriteLine("  - Format: TIFF or PNG (lossless)");
            Console.WriteLine();

            Console.WriteLine("PREPROCESSING:");
            Console.WriteLine("  - Always use Deskew() for scanned documents");
            Console.WriteLine("  - Use DeNoise() to remove scanner artifacts");
            Console.WriteLine("  - Use Contrast() for faded documents");
            Console.WriteLine();

            Console.WriteLine("TWAIN LIBRARIES FOR .NET:");
            Console.WriteLine("  - NTwain (open source): github.com/soukoku/ntwain");
            Console.WriteLine("  - Dynamsoft TWAIN SDK (commercial)");
            Console.WriteLine("  - Aspose.Imaging (commercial)");
            Console.WriteLine("  - WIA (built into Windows)");
            Console.WriteLine();

            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// SCANNER INTEGRATION MADE EASY
//
// Connect any TWAIN/WIA scanner to IronOCR for seamless document capture.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// ============================================================================
