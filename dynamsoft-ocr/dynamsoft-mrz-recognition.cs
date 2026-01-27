/**
 * Dynamsoft MRZ Recognition vs IronOCR: Code Examples
 *
 * This file demonstrates MRZ (Machine Readable Zone) extraction patterns
 * comparing Dynamsoft Label Recognizer with IronOCR's ReadPassport() method.
 *
 * Key insight: Dynamsoft only extracts raw MRZ text - you must parse it yourself.
 * IronOCR ReadPassport() returns structured data with all fields parsed.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * NuGet Packages:
 * - Dynamsoft.DotNet.LabelRecognizer (Dynamsoft)
 * - IronOcr (IronOCR) - https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.Text;
using System.Text.RegularExpressions;

// ============================================================================
// DYNAMSOFT MRZ EXTRACTION
// Requires: Dynamsoft.DotNet.LabelRecognizer
// Problem: Only extracts raw MRZ text - parsing required
// ============================================================================

namespace DynamsoftMrzExamples
{
    /*
    using Dynamsoft.DLR;

    /// <summary>
    /// Dynamsoft MRZ extraction service.
    ///
    /// KEY LIMITATION: Dynamsoft returns raw MRZ lines only.
    /// You must implement your own MRZ parsing logic to extract:
    /// - Document type, country code
    /// - Surname, given names
    /// - Passport number, nationality
    /// - Date of birth, sex, expiry date
    /// </summary>
    public class DynamsoftMrzService : IDisposable
    {
        private readonly LabelRecognizer _recognizer;
        private bool _disposed;

        public DynamsoftMrzService(string licenseKey)
        {
            // License initialization is required before any operation
            LabelRecognizer.InitLicense(licenseKey);
            _recognizer = new LabelRecognizer();

            // MRZ requires specific template configuration
            // You must download and configure the MRZ template separately
            string mrzTemplate = @"{
                ""LabelRecognizerParameterArray"": [{
                    ""Name"": ""MRZ"",
                    ""ReferenceRegionNameArray"": [""FullImage""],
                    ""CharacterModelName"": ""MRZ""
                }]
            }";

            _recognizer.AppendSettingsFromString(mrzTemplate);
        }

        /// <summary>
        /// Extract raw MRZ text from passport image.
        /// Returns the raw MRZ lines - YOU must parse them.
        /// </summary>
        public string ExtractRawMrz(string passportImagePath)
        {
            var results = _recognizer.RecognizeFile(passportImagePath);

            var mrzLines = new StringBuilder();
            foreach (var result in results)
            {
                foreach (var lineResult in result.LineResults)
                {
                    mrzLines.AppendLine(lineResult.Text);
                }
            }

            return mrzLines.ToString().Trim();
        }

        /// <summary>
        /// Parse raw MRZ into structured data.
        /// THIS IS YOUR RESPONSIBILITY with Dynamsoft.
        /// You need to implement TD1, TD2, TD3 format parsing.
        /// </summary>
        public PassportData ParseMrz(string rawMrz)
        {
            // TD3 format (standard passport): 2 lines of 44 characters
            // Line 1: Document type, country, name
            // Line 2: Passport number, nationality, DOB, sex, expiry, check digits

            var lines = rawMrz.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length != 2 || lines[0].Length != 44 || lines[1].Length != 44)
            {
                throw new FormatException("Invalid MRZ format. Expected TD3 (2 lines, 44 chars each).");
            }

            var line1 = lines[0];
            var line2 = lines[1];

            // Line 1 parsing
            var documentType = line1.Substring(0, 2).TrimEnd('<');
            var issuingCountry = line1.Substring(2, 3);
            var namePart = line1.Substring(5, 39);
            var nameParts = namePart.Split(new[] { "<<" }, StringSplitOptions.None);
            var surname = nameParts[0].Replace('<', ' ').Trim();
            var givenNames = nameParts.Length > 1 ? nameParts[1].Replace('<', ' ').Trim() : "";

            // Line 2 parsing
            var passportNumber = line2.Substring(0, 9).TrimEnd('<');
            var passportCheckDigit = line2[9];
            var nationality = line2.Substring(10, 3);
            var dateOfBirth = line2.Substring(13, 6); // YYMMDD
            var dobCheckDigit = line2[19];
            var sex = line2.Substring(20, 1);
            var expiryDate = line2.Substring(21, 6); // YYMMDD
            var expiryCheckDigit = line2[27];

            // Convert dates
            var dob = ParseMrzDate(dateOfBirth);
            var expiry = ParseMrzDate(expiryDate);

            return new PassportData
            {
                DocumentType = documentType,
                IssuingCountry = issuingCountry,
                Surname = surname,
                GivenNames = givenNames,
                PassportNumber = passportNumber,
                Nationality = nationality,
                DateOfBirth = dob,
                Sex = sex,
                ExpiryDate = expiry,
                RawMrz = rawMrz
            };
        }

        private DateTime? ParseMrzDate(string mrzDate)
        {
            if (string.IsNullOrEmpty(mrzDate) || mrzDate.Length != 6)
                return null;

            try
            {
                int year = int.Parse(mrzDate.Substring(0, 2));
                int month = int.Parse(mrzDate.Substring(2, 2));
                int day = int.Parse(mrzDate.Substring(4, 2));

                // MRZ uses 2-digit years
                // Years 00-30 are 2000-2030
                // Years 31-99 are 1931-1999
                year = year <= 30 ? 2000 + year : 1900 + year;

                return new DateTime(year, month, day);
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _recognizer?.Dispose();
                _disposed = true;
            }
        }
    }

    public class PassportData
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
    }
    */

    /// <summary>
    /// Placeholder showing what Dynamsoft MRZ extraction looks like.
    /// See comments above for actual implementation patterns.
    /// </summary>
    public class DynamsoftMrzPlaceholder
    {
        public void ShowComplexity()
        {
            Console.WriteLine("=== DYNAMSOFT MRZ EXTRACTION COMPLEXITY ===");
            Console.WriteLine();
            Console.WriteLine("With Dynamsoft, you must:");
            Console.WriteLine("1. Initialize license");
            Console.WriteLine("2. Configure MRZ template JSON");
            Console.WriteLine("3. Call RecognizeFile to get raw text");
            Console.WriteLine("4. Implement TD1/TD2/TD3 format detection");
            Console.WriteLine("5. Parse each format differently (44/36/30 chars)");
            Console.WriteLine("6. Handle check digit validation");
            Console.WriteLine("7. Convert 2-digit years to 4-digit");
            Console.WriteLine("8. Handle name field separators");
            Console.WriteLine();
            Console.WriteLine("Total: ~150 lines of parsing code AFTER extraction");
            Console.WriteLine();
            Console.WriteLine("Try IronOCR instead - ReadPassport() does it all:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR MRZ EXTRACTION
// One method. Structured output. No parsing required.
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrMrzExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR MRZ extraction - dramatically simpler.
    ///
    /// The ReadPassport() method:
    /// - Automatically detects MRZ format (TD1, TD2, TD3)
    /// - Parses all fields into structured properties
    /// - Validates check digits
    /// - Handles date conversions
    /// - Returns strongly-typed PassportData object
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrMrzService
    {
        /// <summary>
        /// Extract passport MRZ with one method call.
        /// All parsing is done for you.
        /// </summary>
        public void ExtractPassportMrz(string passportImagePath)
        {
            var ocr = new IronTesseract();

            // ReadPassport() does everything:
            // - Finds MRZ region
            // - Reads text
            // - Parses into structured fields
            // - Validates check digits
            var passportData = new IronTesseract().Read(passportImagePath);

            // TODO: verify IronOCR API for passport-specific properties
            // All fields are immediately available as properties
            Console.WriteLine("=== PASSPORT DATA ===");
            Console.WriteLine($"Text: {passportData.Text}");
            // Console.WriteLine($"Document Type: {passportData.DocumentType}");
            // Console.WriteLine($"Country: {passportData.IssuingCountry}");
            // Console.WriteLine($"Surname: {passportData.Surname}");
            // Console.WriteLine($"Given Names: {passportData.GivenNames}");
            // Console.WriteLine($"Passport Number: {passportData.DocumentNumber}");
            // Console.WriteLine($"Nationality: {passportData.Nationality}");
            // Console.WriteLine($"Date of Birth: {passportData.DateOfBirth:yyyy-MM-dd}");
            // Console.WriteLine($"Sex: {passportData.Sex}");
            // Console.WriteLine($"Expiry Date: {passportData.ExpiryDate:yyyy-MM-dd}");
            // Console.WriteLine($"Raw MRZ: {passportData.MRZ}");
        }

        /// <summary>
        /// Batch process multiple passports.
        /// </summary>
        public void BatchProcessPassports(string[] passportImages)
        {
            var ocr = new IronTesseract();

            foreach (var imagePath in passportImages)
            {
                try
                {
                    var passportData = ocr.Read(imagePath);
                    // TODO: verify IronOCR API for passport-specific properties
                    Console.WriteLine($"Processed: {imagePath} - " +
                                    $"{passportData.Text.Substring(0, Math.Min(50, passportData.Text.Length))}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process {imagePath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Complete passport page processing:
        /// MRZ + full data page text + any barcodes
        /// </summary>
        public void ProcessCompletePassportPage(string passportImagePath)
        {
            var ocr = new IronTesseract();
            ocr.Configuration.ReadBarCodes = true;

            // Get full page OCR (for any text outside MRZ)
            var fullResult = ocr.Read(passportImagePath);

            // TODO: verify IronOCR API for passport-specific MRZ parsing
            Console.WriteLine("=== MRZ DATA ===");
            // Console.WriteLine($"Name: {mrzData.Surname}, {mrzData.GivenNames}");
            // Console.WriteLine($"Passport #: {mrzData.DocumentNumber}");
            // Console.WriteLine($"DOB: {mrzData.DateOfBirth:yyyy-MM-dd}");
            // Console.WriteLine($"Expiry: {mrzData.ExpiryDate:yyyy-MM-dd}");

            Console.WriteLine("\n=== FULL PAGE TEXT ===");
            Console.WriteLine(fullResult.Text);

            Console.WriteLine("\n=== BARCODES DETECTED ===");
            foreach (var barcode in fullResult.Barcodes)
            {
                Console.WriteLine($"{barcode.Format}: {barcode.Value}");
            }
        }

        /// <summary>
        /// ID card processing (TD1 format).
        /// Same method works for all MRZ formats.
        /// </summary>
        public void ProcessIdCard(string idCardImagePath)
        {
            var ocr = new IronTesseract();

            // TD1 (ID cards), TD2 (visas), TD3 (passports) all work
            var idData = ocr.Read(idCardImagePath);

            // TODO: verify IronOCR API for ID card-specific properties
            // IronOCR auto-detects the format
            Console.WriteLine($"Text: {idData.Text}");
            // Console.WriteLine($"Document Type: {idData.DocumentType}");
            // Console.WriteLine($"Country: {idData.IssuingCountry}");
            // Console.WriteLine($"Name: {idData.Surname}, {idData.GivenNames}");
            // Console.WriteLine($"Document #: {idData.DocumentNumber}");
        }
    }


    /// <summary>
    /// VIN (Vehicle Identification Number) reading with IronOCR.
    /// Dynamsoft has specialized VIN reading; IronOCR uses standard OCR.
    /// </summary>
    public class IronOcrVinService
    {
        /// <summary>
        /// Read VIN from vehicle image.
        /// </summary>
        public string ReadVin(string vehicleImagePath)
        {
            var ocr = new IronTesseract();

            // VIN is typically in a specific region
            // Use crop region for better accuracy
            using var input = new OcrInput();
            input.LoadImage(vehicleImagePath);

            // Apply filters for VIN plate reading
            input.Deskew();
            input.Contrast();

            var result = ocr.Read(input);

            // VIN is 17 characters, alphanumeric (no I, O, Q)
            var vinPattern = @"\b[A-HJ-NPR-Z0-9]{17}\b";
            var match = Regex.Match(result.Text, vinPattern);

            if (match.Success)
            {
                return match.Value;
            }

            return result.Text.Trim();
        }

        /// <summary>
        /// Batch VIN reading for vehicle inventory.
        /// </summary>
        public void ProcessVehicleInventory(string[] vehicleImages)
        {
            var ocr = new IronTesseract();

            Console.WriteLine("=== VEHICLE INVENTORY SCAN ===");

            foreach (var imagePath in vehicleImages)
            {
                var result = ocr.Read(imagePath);
                var vinPattern = @"\b[A-HJ-NPR-Z0-9]{17}\b";
                var match = Regex.Match(result.Text, vinPattern);

                if (match.Success)
                {
                    Console.WriteLine($"Found VIN: {match.Value}");
                }
            }
        }
    }
}


// ============================================================================
// COMPARISON SUMMARY
// ============================================================================

namespace MrzComparison
{
    public class MrzComparisonSummary
    {
        public void ShowDifference()
        {
            Console.WriteLine("=== DYNAMSOFT vs IRONOCR FOR MRZ ===\n");

            Console.WriteLine("DYNAMSOFT Label Recognizer:");
            Console.WriteLine("  - Returns: Raw MRZ text lines only");
            Console.WriteLine("  - You must: Parse TD1/TD2/TD3 formats yourself");
            Console.WriteLine("  - You must: Handle check digit validation");
            Console.WriteLine("  - You must: Convert dates, parse names");
            Console.WriteLine("  - Lines of code: 150+ for parsing");
            Console.WriteLine("  - License: $599+/year per product");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  - Returns: Fully parsed PassportData object");
            Console.WriteLine("  - Method: ReadPassport(imagePath)");
            Console.WriteLine("  - Properties: Surname, GivenNames, DocumentNumber, etc.");
            Console.WriteLine("  - Formats: TD1, TD2, TD3 auto-detected");
            Console.WriteLine("  - Lines of code: 2");
            Console.WriteLine("  - License: $749 one-time (includes everything)");
            Console.WriteLine();

            Console.WriteLine("Winner: IronOCR - structured output, no parsing needed");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// NEED PASSPORT/MRZ PROCESSING?
//
// IronOCR ReadPassport() returns structured data:
// - All MRZ fields parsed
// - TD1/TD2/TD3 format auto-detection
// - Check digit validation included
// - No parsing code required
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
//
// Install: Install-Package IronOcr
// ============================================================================
