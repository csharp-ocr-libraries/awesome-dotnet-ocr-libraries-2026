/**
 * IronOCR Structured Data Extraction: Complete Guide
 *
 * Extract more than just text - get paragraphs, lines, words,
 * positions, confidence scores, and more.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 * Documentation: https://ironsoftware.com/csharp/ocr/docs/
 */

using System;
using System.Linq;
using IronOcr;

namespace IronOcrStructuredDataExamples
{
    // ========================================================================
    // RESULT OBJECT OVERVIEW
    // ========================================================================

    public class ResultOverview
    {
        /// <summary>
        /// The OcrResult object contains rich structured data
        /// </summary>
        public void ExploreResult()
        {
            var result = new IronTesseract().Read("document.jpg");

            // Full text
            Console.WriteLine($"Full text: {result.Text}");

            // Overall confidence
            Console.WriteLine($"Confidence: {result.Confidence}%");

            // Page count
            Console.WriteLine($"Pages: {result.Pages.Length}");

            // Paragraphs
            Console.WriteLine($"Paragraphs: {result.Paragraphs.Length}");

            // Lines
            Console.WriteLine($"Lines: {result.Lines.Length}");

            // Words
            Console.WriteLine($"Words: {result.Words.Length}");

            // Characters
            Console.WriteLine($"Characters: {result.Characters.Length}");

            // Barcodes (if enabled)
            Console.WriteLine($"Barcodes: {result.Barcodes.Length}");
        }
    }

    // ========================================================================
    // PAGE-LEVEL DATA
    // ========================================================================

    public class PageLevelData
    {
        /// <summary>
        /// Access data per page
        /// </summary>
        public void ProcessByPage()
        {
            var result = new IronTesseract().Read("multi-page.pdf");

            foreach (var page in result.Pages)
            {
                Console.WriteLine($"=== Page {page.PageNumber} ===");
                Console.WriteLine($"Text: {page.Text.Substring(0, Math.Min(100, page.Text.Length))}...");
                Console.WriteLine($"Lines: {page.Lines.Length}");
                Console.WriteLine($"Words: {page.Words.Length}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Find specific page content
        /// </summary>
        public void FindPageWithKeyword(string keyword)
        {
            var result = new IronTesseract().Read("document.pdf");

            foreach (var page in result.Pages)
            {
                if (page.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Found '{keyword}' on page {page.PageNumber}");
                }
            }
        }
    }

    // ========================================================================
    // PARAGRAPH EXTRACTION
    // ========================================================================

    public class ParagraphExtraction
    {
        /// <summary>
        /// Extract paragraphs
        /// </summary>
        public void ExtractParagraphs()
        {
            var result = new IronTesseract().Read("document.jpg");

            int paraNum = 1;
            foreach (var paragraph in result.Paragraphs)
            {
                Console.WriteLine($"Paragraph {paraNum}:");
                Console.WriteLine(paragraph.Text);
                Console.WriteLine($"  Position: ({paragraph.X}, {paragraph.Y})");
                Console.WriteLine($"  Size: {paragraph.Width}x{paragraph.Height}");
                Console.WriteLine();
                paraNum++;
            }
        }

        /// <summary>
        /// Find paragraphs by content
        /// </summary>
        public void FindParagraphContaining(string searchText)
        {
            var result = new IronTesseract().Read("document.jpg");

            var matchingParas = result.Paragraphs
                .Where(p => p.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var para in matchingParas)
            {
                Console.WriteLine($"Found at ({para.X}, {para.Y}): {para.Text}");
            }
        }
    }

    // ========================================================================
    // LINE EXTRACTION
    // ========================================================================

    public class LineExtraction
    {
        /// <summary>
        /// Extract lines with positions
        /// </summary>
        public void ExtractLines()
        {
            var result = new IronTesseract().Read("document.jpg");

            foreach (var line in result.Lines)
            {
                Console.WriteLine($"Y={line.Y}: {line.Text}");
            }
        }

        /// <summary>
        /// Get lines sorted by position
        /// </summary>
        public void LinesSortedByPosition()
        {
            var result = new IronTesseract().Read("document.jpg");

            var sortedLines = result.Lines
                .OrderBy(l => l.Y)    // Top to bottom
                .ThenBy(l => l.X);     // Left to right

            foreach (var line in sortedLines)
            {
                Console.WriteLine($"({line.X},{line.Y}): {line.Text}");
            }
        }

        /// <summary>
        /// Extract header (first line)
        /// </summary>
        public string ExtractHeader()
        {
            var result = new IronTesseract().Read("document.jpg");
            return result.Lines.FirstOrDefault()?.Text ?? "";
        }

        /// <summary>
        /// Extract lines in a specific region
        /// </summary>
        public void ExtractLinesInRegion(int minY, int maxY)
        {
            var result = new IronTesseract().Read("document.jpg");

            var linesInRegion = result.Lines
                .Where(l => l.Y >= minY && l.Y <= maxY)
                .OrderBy(l => l.Y);

            foreach (var line in linesInRegion)
            {
                Console.WriteLine(line.Text);
            }
        }
    }

    // ========================================================================
    // WORD EXTRACTION
    // ========================================================================

    public class WordExtraction
    {
        /// <summary>
        /// Extract words with all metadata
        /// </summary>
        public void ExtractWordsWithMetadata()
        {
            var result = new IronTesseract().Read("document.jpg");

            foreach (var word in result.Words)
            {
                Console.WriteLine($"Word: '{word.Text}'");
                Console.WriteLine($"  Position: ({word.X}, {word.Y})");
                Console.WriteLine($"  Size: {word.Width}x{word.Height}");
                Console.WriteLine($"  Confidence: {word.Confidence}%");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Find low-confidence words
        /// </summary>
        public void FindLowConfidenceWords(double threshold = 80)
        {
            var result = new IronTesseract().Read("document.jpg");

            var lowConfidence = result.Words
                .Where(w => w.Confidence < threshold)
                .OrderBy(w => w.Confidence);

            Console.WriteLine($"Words with confidence < {threshold}%:");
            foreach (var word in lowConfidence)
            {
                Console.WriteLine($"  '{word.Text}' - {word.Confidence}%");
            }
        }

        /// <summary>
        /// Get word count
        /// </summary>
        public int GetWordCount()
        {
            var result = new IronTesseract().Read("document.jpg");
            return result.Words.Length;
        }

        /// <summary>
        /// Find word positions for highlighting
        /// </summary>
        public void FindWordPositions(string searchWord)
        {
            var result = new IronTesseract().Read("document.jpg");

            var matches = result.Words
                .Where(w => w.Text.Equals(searchWord, StringComparison.OrdinalIgnoreCase));

            foreach (var word in matches)
            {
                Console.WriteLine($"Found at ({word.X}, {word.Y}) - {word.Width}x{word.Height}");
            }
        }
    }

    // ========================================================================
    // CHARACTER EXTRACTION
    // ========================================================================

    public class CharacterExtraction
    {
        /// <summary>
        /// Access individual characters
        /// </summary>
        public void ExtractCharacters()
        {
            var result = new IronTesseract().Read("document.jpg");

            // First 50 characters
            var chars = result.Characters.Take(50);

            foreach (var character in chars)
            {
                Console.WriteLine($"'{character.Text}' at ({character.X},{character.Y}) - {character.Confidence}%");
            }
        }

        /// <summary>
        /// Find problematic characters
        /// </summary>
        public void FindProblematicCharacters()
        {
            var result = new IronTesseract().Read("document.jpg");

            var problematic = result.Characters
                .Where(c => c.Confidence < 50)
                .Take(20);

            foreach (var c in problematic)
            {
                Console.WriteLine($"Low confidence char: '{c.Text}' at ({c.X},{c.Y}) - {c.Confidence}%");
            }
        }
    }

    // ========================================================================
    // REGION-BASED OCR
    // ========================================================================

    public class RegionBasedOcr
    {
        /// <summary>
        /// OCR specific region of image
        /// </summary>
        public string ExtractRegion(string imagePath, int x, int y, int width, int height)
        {
            using var input = new OcrInput();
            var region = new System.Drawing.Rectangle(x, y, width, height);
            input.LoadImage(imagePath, region);

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Extract header region
        /// </summary>
        public string ExtractHeader(string imagePath, int height = 100)
        {
            using var input = new OcrInput();
            var region = new System.Drawing.Rectangle(0, 0, 2000, height);
            input.LoadImage(imagePath, region);

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Extract multiple regions
        /// </summary>
        public void ExtractMultipleRegions(string imagePath)
        {
            // Header
            using var headerInput = new OcrInput();
            headerInput.LoadImage(imagePath, new System.Drawing.Rectangle(0, 0, 2000, 100));
            var header = new IronTesseract().Read(headerInput).Text;

            // Footer
            using var footerInput = new OcrInput();
            footerInput.LoadImage(imagePath, new System.Drawing.Rectangle(0, 1000, 2000, 100));
            var footer = new IronTesseract().Read(footerInput).Text;

            Console.WriteLine($"Header: {header}");
            Console.WriteLine($"Footer: {footer}");
        }
    }

    // ========================================================================
    // BARCODE READING
    // ========================================================================

    public class BarcodeReading
    {
        /// <summary>
        /// Read barcodes during OCR
        /// </summary>
        public void ReadBarcodes()
        {
            var ocr = new IronTesseract();
            ocr.Configuration.ReadBarCodes = true;

            var result = ocr.Read("document-with-barcode.jpg");

            Console.WriteLine($"Text: {result.Text}");

            foreach (var barcode in result.Barcodes)
            {
                Console.WriteLine($"Barcode: {barcode.Value}");
                Console.WriteLine($"  Format: {barcode.Format}");
                Console.WriteLine($"  Position: ({barcode.X}, {barcode.Y})");
            }
        }

        /// <summary>
        /// Extract only barcodes
        /// </summary>
        public string[] ExtractBarcodesOnly(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Configuration.ReadBarCodes = true;

            var result = ocr.Read(imagePath);

            return result.Barcodes.Select(b => b.Value).ToArray();
        }
    }

    // ========================================================================
    // CONFIDENCE ANALYSIS
    // ========================================================================

    public class ConfidenceAnalysis
    {
        /// <summary>
        /// Analyze OCR quality
        /// </summary>
        public void AnalyzeConfidence(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);

            Console.WriteLine($"Overall Confidence: {result.Confidence}%");

            // Word confidence distribution
            var highConfidence = result.Words.Count(w => w.Confidence >= 90);
            var mediumConfidence = result.Words.Count(w => w.Confidence >= 70 && w.Confidence < 90);
            var lowConfidence = result.Words.Count(w => w.Confidence < 70);

            Console.WriteLine($"High confidence (90%+): {highConfidence} words");
            Console.WriteLine($"Medium confidence (70-90%): {mediumConfidence} words");
            Console.WriteLine($"Low confidence (<70%): {lowConfidence} words");

            // Average word confidence
            var avgConfidence = result.Words.Average(w => w.Confidence);
            Console.WriteLine($"Average word confidence: {avgConfidence:F1}%");
        }

        /// <summary>
        /// Quality check - is OCR reliable?
        /// </summary>
        public bool IsOcrReliable(string imagePath, double minConfidence = 85)
        {
            var result = new IronTesseract().Read(imagePath);
            return result.Confidence >= minConfidence;
        }
    }
}


// ============================================================================
// EXTRACT STRUCTURED DATA, NOT JUST TEXT
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Tutorials: https://ironsoftware.com/csharp/ocr/tutorials/
// ============================================================================
