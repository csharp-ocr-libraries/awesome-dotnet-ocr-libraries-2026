/**
 * Tesseract Multi-Language OCR: Setup and Limitations
 *
 * Setting up multiple languages with Tesseract requires
 * manual tessdata file management.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 */

using System;
using System.IO;

// ============================================================================
// TESSERACT LANGUAGE SETUP - MANUAL PROCESS
// ============================================================================

namespace TesseractLanguageExamples
{
    using Tesseract;

    /// <summary>
    /// Tesseract multi-language requirements:
    /// 1. Download traineddata files for each language
    /// 2. Place in tessdata folder
    /// 3. Reference all languages in initialization
    /// 4. File sizes: 1-100MB per language
    /// </summary>
    public class TesseractLanguageService
    {
        private const string TessDataPath = @"./tessdata";

        /// <summary>
        /// Before this works, you must:
        /// 1. Download eng.traineddata (~15MB)
        /// 2. Download fra.traineddata (~15MB)
        /// 3. Download deu.traineddata (~15MB)
        /// 4. Place all in ./tessdata folder
        /// </summary>
        public string MultiLanguageOcr(string imagePath)
        {
            // Languages separated by +
            using var engine = new TesseractEngine(TessDataPath, "eng+fra+deu", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);

            return page.GetText();
        }

        /// <summary>
        /// Common language codes and file sizes
        /// </summary>
        public void ShowLanguageFiles()
        {
            Console.WriteLine("Tesseract Language Files Required:");
            Console.WriteLine();
            Console.WriteLine("Language     | File                  | Size");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("English      | eng.traineddata       | ~15MB");
            Console.WriteLine("French       | fra.traineddata       | ~15MB");
            Console.WriteLine("German       | deu.traineddata       | ~15MB");
            Console.WriteLine("Spanish      | spa.traineddata       | ~15MB");
            Console.WriteLine("Chinese Simp | chi_sim.traineddata   | ~45MB");
            Console.WriteLine("Chinese Trad | chi_tra.traineddata   | ~45MB");
            Console.WriteLine("Japanese     | jpn.traineddata       | ~40MB");
            Console.WriteLine("Korean       | kor.traineddata       | ~35MB");
            Console.WriteLine("Arabic       | ara.traineddata       | ~20MB");
            Console.WriteLine("Russian      | rus.traineddata       | ~20MB");
            Console.WriteLine();
            Console.WriteLine("Download from: https://github.com/tesseract-ocr/tessdata");
            Console.WriteLine();
            Console.WriteLine("10 languages = ~200-300MB to manage");
        }

        /// <summary>
        /// Check if language is available
        /// </summary>
        public bool IsLanguageAvailable(string langCode)
        {
            var filePath = Path.Combine(TessDataPath, $"{langCode}.traineddata");
            return File.Exists(filePath);
        }

        /// <summary>
        /// Languages must be installed beforehand
        /// </summary>
        public string SafeMultiLanguageOcr(string imagePath, string[] languages)
        {
            // Check if all languages are available
            foreach (var lang in languages)
            {
                if (!IsLanguageAvailable(lang))
                {
                    throw new FileNotFoundException(
                        $"Missing {lang}.traineddata in {TessDataPath}. " +
                        $"Download from https://github.com/tesseract-ocr/tessdata");
                }
            }

            var langString = string.Join("+", languages);
            using var engine = new TesseractEngine(TessDataPath, langString, EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);

            return page.GetText();
        }
    }
}


// ============================================================================
// IRONOCR - LANGUAGES VIA NUGET
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrLanguageExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR language support:
    /// - 125+ languages available
    /// - Install via NuGet (no file management)
    /// - Add secondary languages at runtime
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrLanguageService
    {
        /// <summary>
        /// Default English - works immediately
        /// </summary>
        public string EnglishOcr(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// Single language - just set property
        /// (Install: Install-Package IronOcr.Languages.French)
        /// </summary>
        public string FrenchOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.French;
            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Multiple languages - add secondary languages
        /// </summary>
        public string MultiLanguageOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            ocr.AddSecondaryLanguage(OcrLanguage.German);
            ocr.AddSecondaryLanguage(OcrLanguage.Spanish);

            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Chinese Simplified
        /// (Install: Install-Package IronOcr.Languages.ChineseSimplified)
        /// </summary>
        public string ChineseOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.ChineseSimplified;
            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Japanese
        /// </summary>
        public string JapaneseOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Japanese;
            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Arabic (right-to-left)
        /// </summary>
        public string ArabicOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Arabic;
            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Mixed CJK and European
        /// </summary>
        public string MixedDocumentOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.ChineseSimplified;
            ocr.AddSecondaryLanguage(OcrLanguage.English);
            ocr.AddSecondaryLanguage(OcrLanguage.Japanese);

            return ocr.Read(imagePath).Text;
        }
    }
}


// ============================================================================
// COMPARISON
// ============================================================================

namespace Comparison
{
    public class LanguageComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== LANGUAGE SUPPORT COMPARISON ===\n");

            Console.WriteLine("TESSERACT:");
            Console.WriteLine("  1. Find traineddata URL");
            Console.WriteLine("  2. Download file (15-100MB)");
            Console.WriteLine("  3. Place in tessdata folder");
            Console.WriteLine("  4. Reference in code");
            Console.WriteLine("  5. Repeat for each language");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  1. Install-Package IronOcr.Languages.French");
            Console.WriteLine("  2. ocr.Language = OcrLanguage.French;");
            Console.WriteLine("  Done.");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// 125+ LANGUAGES - EASY INSTALLATION
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Languages: https://ironsoftware.com/csharp/ocr/languages/
// ============================================================================
