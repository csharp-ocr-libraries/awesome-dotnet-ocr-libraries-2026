/**
 * Dynamsoft to IronOCR Migration: Side-by-Side Comparison
 *
 * This file demonstrates migration patterns from Dynamsoft products to IronOCR.
 * Key theme: Multiple Dynamsoft products vs single IronOCR package.
 *
 * Dynamsoft products required for typical document processing:
 * - Dynamsoft Label Recognizer (MRZ, VIN, labels)
 * - Dynamsoft Barcode Reader (1D/2D barcodes)
 * - Dynamsoft Document Normalizer (edge detection)
 * - Plus another OCR library for general documents!
 *
 * IronOCR: One package handles all of the above.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

// ============================================================================
// SCENARIO 1: PASSPORT PROCESSING APPLICATION
// Dynamsoft requires multiple products; IronOCR needs one package
// ============================================================================

namespace PassportProcessingMigration
{
    /*
    // ===== DYNAMSOFT APPROACH (MULTIPLE PRODUCTS) =====

    using Dynamsoft.DLR;  // Label Recognizer - for MRZ
    using Dynamsoft.DBR;  // Barcode Reader - for any barcodes
    // Plus: Need separate OCR library for full page text!

    /// <summary>
    /// Dynamsoft passport processing requires:
    /// 1. Dynamsoft Label Recognizer ($599+/year) - for MRZ
    /// 2. Dynamsoft Barcode Reader (additional license) - for barcodes
    /// 3. ANOTHER OCR library - for full page text
    ///
    /// Total: 3 separate products with separate licenses
    /// </summary>
    public class DynamsoftPassportProcessor : IDisposable
    {
        private readonly LabelRecognizer _mrzRecognizer;
        private readonly BarcodeReader _barcodeReader;
        // private readonly SomeOtherOcrLibrary _fullTextOcr;

        public DynamsoftPassportProcessor(
            string mrzLicenseKey,
            string barcodeLicenseKey)
            // string fullOcrLicenseKey)
        {
            // Initialize MRZ recognition
            LabelRecognizer.InitLicense(mrzLicenseKey);
            _mrzRecognizer = new LabelRecognizer();
            ConfigureMrzTemplate();

            // Initialize barcode reader (separate product, separate license)
            BarcodeReader.InitLicense(barcodeLicenseKey);
            _barcodeReader = new BarcodeReader();

            // Initialize third OCR library for full page text
            // _fullTextOcr = new SomeOtherOcrLibrary(fullOcrLicenseKey);
        }

        private void ConfigureMrzTemplate()
        {
            // MRZ requires JSON template configuration
            string mrzTemplate = @"{
                ""LabelRecognizerParameterArray"": [{
                    ""Name"": ""MRZ_Passport"",
                    ""ReferenceRegionNameArray"": [""FullImage""],
                    ""CharacterModelName"": ""MRZ""
                }]
            }";
            _mrzRecognizer.AppendSettingsFromString(mrzTemplate);
        }

        public PassportResult ProcessPassport(string imagePath)
        {
            var result = new PassportResult();

            // Step 1: Extract MRZ (Dynamsoft Label Recognizer)
            var mrzResults = _mrzRecognizer.RecognizeFile(imagePath);
            var mrzText = new StringBuilder();
            foreach (var r in mrzResults)
            {
                foreach (var line in r.LineResults)
                {
                    mrzText.AppendLine(line.Text);
                }
            }
            result.RawMrz = mrzText.ToString();
            result.ParsedMrz = ParseMrzManually(result.RawMrz);

            // Step 2: Read any barcodes (Dynamsoft Barcode Reader)
            var barcodeResults = _barcodeReader.DecodeFile(imagePath);
            result.Barcodes = new List<BarcodeData>();
            foreach (var barcode in barcodeResults)
            {
                result.Barcodes.Add(new BarcodeData
                {
                    Format = barcode.BarcodeFormatString,
                    Value = barcode.BarcodeText
                });
            }

            // Step 3: Full page OCR would require THIRD library
            // result.FullPageText = _fullTextOcr.ReadImage(imagePath);

            return result;
        }

        private ParsedMrzData ParseMrzManually(string rawMrz)
        {
            // YOU must implement TD3 format parsing
            // Lines of code: 100+
            // See dynamsoft-mrz-recognition.cs for full implementation
            throw new NotImplementedException("MRZ parsing required");
        }

        public void Dispose()
        {
            _mrzRecognizer?.Dispose();
            _barcodeReader?.Dispose();
        }
    }

    public class PassportResult
    {
        public string RawMrz { get; set; }
        public ParsedMrzData ParsedMrz { get; set; }
        public List<BarcodeData> Barcodes { get; set; }
        public string FullPageText { get; set; }
    }
    */

    // ===== IRONOCR APPROACH (ONE PACKAGE) =====

    using IronOcr;

    /// <summary>
    /// IronOCR passport processing:
    /// - ReadPassport() for MRZ with automatic parsing
    /// - ReadBarCodes = true for barcodes
    /// - Read() for full page text
    ///
    /// ONE package. ONE license. ALL capabilities.
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrPassportProcessor
    {
        public CompletePassportResult ProcessPassport(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Configuration.ReadBarCodes = true;

            // Get structured MRZ data (automatically parsed!)
            var mrzData = ocr.ReadPassport(imagePath);

            // Get full page OCR + barcodes (same package!)
            var fullResult = ocr.Read(imagePath);

            return new CompletePassportResult
            {
                // MRZ fields already parsed
                DocumentType = mrzData.DocumentType,
                IssuingCountry = mrzData.IssuingCountry,
                Surname = mrzData.Surname,
                GivenNames = mrzData.GivenNames,
                PassportNumber = mrzData.DocumentNumber,
                Nationality = mrzData.Nationality,
                DateOfBirth = mrzData.DateOfBirth,
                Sex = mrzData.Sex,
                ExpiryDate = mrzData.ExpiryDate,
                RawMrz = mrzData.MRZ,

                // Full page text
                FullPageText = fullResult.Text,

                // Barcodes
                Barcodes = ExtractBarcodes(fullResult),

                // Confidence
                Confidence = fullResult.Confidence
            };
        }

        private List<BarcodeInfo> ExtractBarcodes(OcrResult result)
        {
            var barcodes = new List<BarcodeInfo>();
            foreach (var barcode in result.Barcodes)
            {
                barcodes.Add(new BarcodeInfo
                {
                    Format = barcode.Format.ToString(),
                    Value = barcode.Value,
                    X = barcode.X,
                    Y = barcode.Y
                });
            }
            return barcodes;
        }
    }

    public class CompletePassportResult
    {
        public string DocumentType { get; set; }
        public string IssuingCountry { get; set; }
        public string Surname { get; set; }
        public string GivenNames { get; set; }
        public string PassportNumber { get; set; }
        public string Nationality { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Sex { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string RawMrz { get; set; }
        public string FullPageText { get; set; }
        public List<BarcodeInfo> Barcodes { get; set; }
        public double Confidence { get; set; }
    }

    public class BarcodeInfo
    {
        public string Format { get; set; }
        public string Value { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}


// ============================================================================
// SCENARIO 2: WAREHOUSE INVENTORY SYSTEM
// Labels + Barcodes + Product Information
// ============================================================================

namespace WarehouseInventoryMigration
{
    /*
    // ===== DYNAMSOFT: THREE PRODUCTS REQUIRED =====

    using Dynamsoft.DLR;  // Label Recognizer - for product labels
    using Dynamsoft.DBR;  // Barcode Reader - for product barcodes
    using Dynamsoft.DDN;  // Document Normalizer - for document capture

    public class DynamsoftWarehouseScanner : IDisposable
    {
        private readonly LabelRecognizer _labelRecognizer;
        private readonly BarcodeReader _barcodeReader;
        private readonly DocumentNormalizer _documentNormalizer;

        // THREE separate license keys required
        public DynamsoftWarehouseScanner(
            string labelLicenseKey,
            string barcodeLicenseKey,
            string documentLicenseKey)
        {
            LabelRecognizer.InitLicense(labelLicenseKey);
            _labelRecognizer = new LabelRecognizer();

            BarcodeReader.InitLicense(barcodeLicenseKey);
            _barcodeReader = new BarcodeReader();

            DocumentNormalizer.InitLicense(documentLicenseKey);
            _documentNormalizer = new DocumentNormalizer();
        }

        public WarehouseItemResult ScanItem(string imagePath)
        {
            // Three separate API calls, three separate result types
            var labelResults = _labelRecognizer.RecognizeFile(imagePath);
            var barcodeResults = _barcodeReader.DecodeFile(imagePath);
            var documentResults = _documentNormalizer.Normalize(imagePath);

            // Merge results manually...
            return new WarehouseItemResult
            {
                // Complex aggregation required
            };
        }

        public void Dispose()
        {
            _labelRecognizer?.Dispose();
            _barcodeReader?.Dispose();
            _documentNormalizer?.Dispose();
        }
    }
    */

    // ===== IRONOCR: ONE UNIFIED API =====

    using IronOcr;

    /// <summary>
    /// IronOCR warehouse scanning:
    /// - Labels: Read() with region cropping
    /// - Barcodes: ReadBarCodes = true
    /// - Documents: Native PDF support
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrWarehouseScanner
    {
        public WarehouseItemResult ScanItem(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Configuration.ReadBarCodes = true;

            // One API call gets everything
            var result = ocr.Read(imagePath);

            return new WarehouseItemResult
            {
                // All text including labels
                AllText = result.Text,

                // Words with positions (for label detection)
                Words = ExtractWordsWithPositions(result),

                // All barcodes in image
                Barcodes = ExtractBarcodes(result),

                // Confidence score
                Confidence = result.Confidence
            };
        }

        /// <summary>
        /// Scan specific label region (like Dynamsoft's targeted recognition).
        /// </summary>
        public string ScanLabelRegion(string imagePath, int x, int y, int width, int height)
        {
            var ocr = new IronTesseract();

            using var input = new OcrInput();
            var region = new CropRectangle(x, y, width, height);
            input.LoadImage(imagePath, region);

            // Apply preprocessing for label quality
            input.Deskew();
            input.Contrast();

            return ocr.Read(input).Text;
        }

        /// <summary>
        /// Batch process inventory images.
        /// </summary>
        public void ProcessInventoryBatch(string[] imagePaths)
        {
            var ocr = new IronTesseract();
            ocr.Configuration.ReadBarCodes = true;

            foreach (var path in imagePaths)
            {
                var result = ScanItem(path);

                Console.WriteLine($"File: {Path.GetFileName(path)}");
                Console.WriteLine($"  Barcodes: {result.Barcodes.Count}");
                Console.WriteLine($"  Text Length: {result.AllText.Length}");
                Console.WriteLine($"  Confidence: {result.Confidence:P}");
            }
        }

        private List<WordInfo> ExtractWordsWithPositions(OcrResult result)
        {
            var words = new List<WordInfo>();
            foreach (var word in result.Words)
            {
                words.Add(new WordInfo
                {
                    Text = word.Text,
                    X = word.X,
                    Y = word.Y,
                    Width = word.Width,
                    Height = word.Height,
                    Confidence = word.Confidence
                });
            }
            return words;
        }

        private List<BarcodeInfo> ExtractBarcodes(OcrResult result)
        {
            var barcodes = new List<BarcodeInfo>();
            foreach (var barcode in result.Barcodes)
            {
                barcodes.Add(new BarcodeInfo
                {
                    Format = barcode.Format.ToString(),
                    Value = barcode.Value
                });
            }
            return barcodes;
        }
    }

    public class WarehouseItemResult
    {
        public string AllText { get; set; }
        public List<WordInfo> Words { get; set; }
        public List<BarcodeInfo> Barcodes { get; set; }
        public double Confidence { get; set; }
    }

    public class WordInfo
    {
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double Confidence { get; set; }
    }

    public class BarcodeInfo
    {
        public string Format { get; set; }
        public string Value { get; set; }
    }
}


// ============================================================================
// SCENARIO 3: ADDING GENERAL OCR TO DYNAMSOFT-BASED APP
// The capability Dynamsoft simply doesn't have
// ============================================================================

namespace GeneralOcrMigration
{
    using IronOcr;

    /// <summary>
    /// When you need general OCR alongside MRZ/barcode reading,
    /// Dynamsoft can't help. You need a second library.
    ///
    /// IronOCR handles everything in one package.
    /// </summary>
    public class DocumentProcessingService
    {
        private readonly IronTesseract _ocr;

        public DocumentProcessingService()
        {
            _ocr = new IronTesseract();
            _ocr.Configuration.ReadBarCodes = true;
        }

        /// <summary>
        /// Process passport (Dynamsoft specialty) + attached documents.
        /// </summary>
        public TravelDocumentPackage ProcessTravelDocuments(
            string passportImagePath,
            string[] supportingDocumentPaths)
        {
            var result = new TravelDocumentPackage();

            // Passport MRZ extraction
            result.PassportData = _ocr.ReadPassport(passportImagePath);

            // Supporting documents (letters, tickets, etc.)
            // Dynamsoft can't do this - it's not general OCR!
            result.SupportingDocuments = new List<DocumentResult>();

            foreach (var docPath in supportingDocumentPaths)
            {
                var docResult = _ocr.Read(docPath);
                result.SupportingDocuments.Add(new DocumentResult
                {
                    FilePath = docPath,
                    Text = docResult.Text,
                    Confidence = docResult.Confidence,
                    PageCount = docResult.Pages.Count()
                });
            }

            return result;
        }

        /// <summary>
        /// Process PDF documents (Dynamsoft has no native PDF support).
        /// </summary>
        public string ProcessPdf(string pdfPath)
        {
            // IronOCR: Native PDF support
            return _ocr.Read(pdfPath).Text;

            // Dynamsoft: You'd need to:
            // 1. Find a PDF rendering library
            // 2. Convert PDF pages to images
            // 3. Process each image through Dynamsoft
            // 4. Aggregate results
            // Total: 50+ lines of code minimum
        }

        /// <summary>
        /// Process password-protected PDF (Dynamsoft can't do this).
        /// </summary>
        public string ProcessEncryptedPdf(string pdfPath, string password)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath, Password: password);

            return _ocr.Read(input).Text;
        }

        /// <summary>
        /// Create searchable PDF from scanned document
        /// (Dynamsoft has no searchable PDF output).
        /// </summary>
        public void CreateSearchablePdf(string scannedPdfPath, string outputPath)
        {
            var result = _ocr.Read(scannedPdfPath);
            result.SaveAsSearchablePdf(outputPath);
        }

        /// <summary>
        /// Multi-language OCR (Dynamsoft has very limited language support).
        /// </summary>
        public string ProcessMultilingualDocument(string imagePath)
        {
            var ocr = new IronTesseract();

            // Add languages as needed
            ocr.AddLanguage(OcrLanguage.German);
            ocr.AddLanguage(OcrLanguage.French);
            ocr.AddLanguage(OcrLanguage.Spanish);
            ocr.AddLanguage(OcrLanguage.ChineseSimplified);
            ocr.AddLanguage(OcrLanguage.Japanese);
            ocr.AddLanguage(OcrLanguage.Arabic);

            return ocr.Read(imagePath).Text;
        }
    }

    public class TravelDocumentPackage
    {
        // TODO: verify IronOCR API for passport-specific result type
        public OcrResult PassportData { get; set; }
        public List<DocumentResult> SupportingDocuments { get; set; }
    }

    public class DocumentResult
    {
        public string FilePath { get; set; }
        public string Text { get; set; }
        public double Confidence { get; set; }
        public int PageCount { get; set; }
    }
}


// ============================================================================
// MIGRATION COST COMPARISON
// ============================================================================

namespace MigrationCostAnalysis
{
    public class CostComparisonReport
    {
        public void GenerateReport()
        {
            Console.WriteLine("=== DYNAMSOFT vs IRONOCR: COST ANALYSIS ===\n");

            Console.WriteLine("SCENARIO: Full Document Processing (MRZ + Barcodes + PDF + General OCR)\n");

            Console.WriteLine("DYNAMSOFT COSTS:");
            Console.WriteLine("  Dynamsoft Label Recognizer:  $599+/year");
            Console.WriteLine("  Dynamsoft Barcode Reader:    $599+/year (separate)");
            Console.WriteLine("  PDF Library (3rd party):     $300+/year");
            Console.WriteLine("  General OCR Library:         $500+/year");
            Console.WriteLine("  ----------------------------------------");
            Console.WriteLine("  Annual Total:                $1,997+/year");
            Console.WriteLine("  5-Year Total:                $9,985+");
            Console.WriteLine();

            Console.WriteLine("IRONOCR COSTS:");
            Console.WriteLine("  IronOCR (includes ALL):      $749 one-time");
            Console.WriteLine("  - MRZ (ReadPassport):        Included");
            Console.WriteLine("  - Barcodes (ReadBarCodes):   Included");
            Console.WriteLine("  - PDF Support:               Included");
            Console.WriteLine("  - General OCR:               Included");
            Console.WriteLine("  - 125+ Languages:            Included");
            Console.WriteLine("  ----------------------------------------");
            Console.WriteLine("  One-Time Total:              $749");
            Console.WriteLine("  5-Year Total:                $749");
            Console.WriteLine();

            Console.WriteLine("SAVINGS: $9,236+ over 5 years\n");

            Console.WriteLine("DEVELOPMENT TIME COMPARISON:");
            Console.WriteLine("  Dynamsoft integration:       40+ hours");
            Console.WriteLine("    - Multiple SDK setups");
            Console.WriteLine("    - MRZ parsing implementation");
            Console.WriteLine("    - PDF handling workarounds");
            Console.WriteLine("    - Result aggregation");
            Console.WriteLine();

            Console.WriteLine("  IronOCR integration:         4-8 hours");
            Console.WriteLine("    - Single NuGet install");
            Console.WriteLine("    - Unified API");
            Console.WriteLine("    - Built-in parsing");
            Console.WriteLine("    - Native PDF support");
            Console.WriteLine();

            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// QUICK MIGRATION REFERENCE
// ============================================================================

namespace MigrationQuickReference
{
    using IronOcr;

    /// <summary>
    /// Quick migration patterns from Dynamsoft to IronOCR.
    /// </summary>
    public class MigrationPatterns
    {
        public void ShowPatterns()
        {
            Console.WriteLine("=== QUICK MIGRATION PATTERNS ===\n");

            Console.WriteLine("1. MRZ EXTRACTION");
            Console.WriteLine("   Before (Dynamsoft):");
            Console.WriteLine("   var rec = new LabelRecognizer();");
            Console.WriteLine("   var results = rec.RecognizeFile(path);");
            Console.WriteLine("   // + 100 lines MRZ parsing");
            Console.WriteLine();
            Console.WriteLine("   After (IronOCR):");
            Console.WriteLine("   var data = new IronTesseract().ReadPassport(path);");
            Console.WriteLine("   // Parsed fields: data.Surname, data.DocumentNumber, etc.\n");

            Console.WriteLine("2. BARCODE READING");
            Console.WriteLine("   Before (Dynamsoft Barcode Reader - separate product):");
            Console.WriteLine("   BarcodeReader.InitLicense(\"KEY\");");
            Console.WriteLine("   var reader = new BarcodeReader();");
            Console.WriteLine("   var barcodes = reader.DecodeFile(path);");
            Console.WriteLine();
            Console.WriteLine("   After (IronOCR - same package):");
            Console.WriteLine("   var ocr = new IronTesseract();");
            Console.WriteLine("   ocr.Configuration.ReadBarCodes = true;");
            Console.WriteLine("   var barcodes = ocr.Read(path).Barcodes;\n");

            Console.WriteLine("3. PDF PROCESSING");
            Console.WriteLine("   Before (Dynamsoft): Not supported natively");
            Console.WriteLine("   After (IronOCR):");
            Console.WriteLine("   var text = new IronTesseract().Read(\"doc.pdf\").Text;\n");

            Console.WriteLine("4. FULL DOCUMENT OCR");
            Console.WriteLine("   Before (Dynamsoft): Requires different library");
            Console.WriteLine("   After (IronOCR):");
            Console.WriteLine("   var text = new IronTesseract().Read(imagePath).Text;\n");

            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// NEED UNIFIED OCR CAPABILITIES?
//
// Stop juggling multiple Dynamsoft products and third-party libraries.
// IronOCR provides everything in ONE package:
//
// - MRZ extraction (ReadPassport) - Parsed output, no manual parsing
// - Barcode reading (ReadBarCodes) - All formats supported
// - PDF processing - Native, including password-protected
// - General OCR - 125+ languages, complex layouts
// - Searchable PDF - Create from scanned documents
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
//
// Install: Install-Package IronOcr
// ============================================================================
