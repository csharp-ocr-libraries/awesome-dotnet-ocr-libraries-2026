/**
 * IronOCR Multi-Language Support: Complete Guide
 *
 * OCR in 125+ languages with simple NuGet package installation.
 * No manual tessdata management required.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 * Languages: https://ironsoftware.com/csharp/ocr/languages/
 */

using System;
using IronOcr;

namespace IronOcrLanguageExamples
{
    // ========================================================================
    // BASIC LANGUAGE USAGE
    // ========================================================================

    public class BasicLanguages
    {
        /// <summary>
        /// English (default) - no additional package needed
        /// </summary>
        public void EnglishOcr()
        {
            var text = new IronTesseract().Read("english-document.jpg").Text;
            Console.WriteLine(text);
        }

        /// <summary>
        /// French
        /// Install: Install-Package IronOcr.Languages.French
        /// </summary>
        public void FrenchOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.French;

            var result = ocr.Read("french-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// German
        /// Install: Install-Package IronOcr.Languages.German
        /// </summary>
        public void GermanOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.German;

            var result = ocr.Read("german-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Spanish
        /// Install: Install-Package IronOcr.Languages.Spanish
        /// </summary>
        public void SpanishOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Spanish;

            var result = ocr.Read("spanish-document.jpg");
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // MULTI-LANGUAGE DOCUMENTS
    // ========================================================================

    public class MultiLanguageDocuments
    {
        /// <summary>
        /// Document with English and French
        /// </summary>
        public void EnglishAndFrench()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);

            var result = ocr.Read("bilingual-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// European multilingual document
        /// </summary>
        public void EuropeanMultilingual()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            ocr.AddSecondaryLanguage(OcrLanguage.German);
            ocr.AddSecondaryLanguage(OcrLanguage.Spanish);
            ocr.AddSecondaryLanguage(OcrLanguage.Italian);

            var result = ocr.Read("european-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Scientific document with Greek symbols
        /// </summary>
        public void ScientificWithGreek()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.Greek);

            var result = ocr.Read("scientific-paper.jpg");
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // ASIAN LANGUAGES
    // ========================================================================

    public class AsianLanguages
    {
        /// <summary>
        /// Chinese Simplified
        /// Install: Install-Package IronOcr.Languages.ChineseSimplified
        /// </summary>
        public void ChineseSimplifiedOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.ChineseSimplified;

            var result = ocr.Read("chinese-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Chinese Traditional
        /// Install: Install-Package IronOcr.Languages.ChineseTraditional
        /// </summary>
        public void ChineseTraditionalOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.ChineseTraditional;

            var result = ocr.Read("traditional-chinese.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Japanese
        /// Install: Install-Package IronOcr.Languages.Japanese
        /// </summary>
        public void JapaneseOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Japanese;

            var result = ocr.Read("japanese-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Korean
        /// Install: Install-Package IronOcr.Languages.Korean
        /// </summary>
        public void KoreanOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Korean;

            var result = ocr.Read("korean-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Mixed CJK and English
        /// </summary>
        public void MixedCjkEnglish()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.ChineseSimplified;
            ocr.AddSecondaryLanguage(OcrLanguage.English);
            ocr.AddSecondaryLanguage(OcrLanguage.Japanese);

            var result = ocr.Read("mixed-asian-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Thai
        /// </summary>
        public void ThaiOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Thai;

            var result = ocr.Read("thai-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Vietnamese
        /// </summary>
        public void VietnameseOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Vietnamese;

            var result = ocr.Read("vietnamese-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Hindi
        /// </summary>
        public void HindiOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Hindi;

            var result = ocr.Read("hindi-document.jpg");
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // RIGHT-TO-LEFT LANGUAGES
    // ========================================================================

    public class RtlLanguages
    {
        /// <summary>
        /// Arabic
        /// Install: Install-Package IronOcr.Languages.Arabic
        /// </summary>
        public void ArabicOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Arabic;

            var result = ocr.Read("arabic-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Hebrew
        /// </summary>
        public void HebrewOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Hebrew;

            var result = ocr.Read("hebrew-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Farsi (Persian)
        /// </summary>
        public void FarsiOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Persian;

            var result = ocr.Read("farsi-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Urdu
        /// </summary>
        public void UrduOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Urdu;

            var result = ocr.Read("urdu-document.jpg");
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // CYRILLIC LANGUAGES
    // ========================================================================

    public class CyrillicLanguages
    {
        /// <summary>
        /// Russian
        /// Install: Install-Package IronOcr.Languages.Russian
        /// </summary>
        public void RussianOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Russian;

            var result = ocr.Read("russian-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Ukrainian
        /// </summary>
        public void UkrainianOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Ukrainian;

            var result = ocr.Read("ukrainian-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Bulgarian
        /// </summary>
        public void BulgarianOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Bulgarian;

            var result = ocr.Read("bulgarian-document.jpg");
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // NORDIC LANGUAGES
    // ========================================================================

    public class NordicLanguages
    {
        /// <summary>
        /// Swedish
        /// </summary>
        public void SwedishOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Swedish;

            var result = ocr.Read("swedish-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Norwegian
        /// </summary>
        public void NorwegianOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Norwegian;

            var result = ocr.Read("norwegian-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Danish
        /// </summary>
        public void DanishOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Danish;

            var result = ocr.Read("danish-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Finnish
        /// </summary>
        public void FinnishOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Finnish;

            var result = ocr.Read("finnish-document.jpg");
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // OTHER LANGUAGES
    // ========================================================================

    public class OtherLanguages
    {
        /// <summary>
        /// Portuguese
        /// </summary>
        public void PortugueseOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Portuguese;

            var result = ocr.Read("portuguese-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Dutch
        /// </summary>
        public void DutchOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Dutch;

            var result = ocr.Read("dutch-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Polish
        /// </summary>
        public void PolishOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Polish;

            var result = ocr.Read("polish-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Turkish
        /// </summary>
        public void TurkishOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Turkish;

            var result = ocr.Read("turkish-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Greek
        /// </summary>
        public void GreekOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Greek;

            var result = ocr.Read("greek-document.jpg");
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // LANGUAGE SELECTION HELPER
    // ========================================================================

    public class LanguageInfo
    {
        public void ShowAvailableLanguages()
        {
            Console.WriteLine("=== IRONOCR SUPPORTED LANGUAGES ===\n");

            Console.WriteLine("EUROPEAN:");
            Console.WriteLine("  English, French, German, Spanish, Italian");
            Console.WriteLine("  Portuguese, Dutch, Polish, Czech, Hungarian");
            Console.WriteLine("  Romanian, Swedish, Norwegian, Danish, Finnish");
            Console.WriteLine("  Greek, Bulgarian, Croatian, Serbian, Slovak");
            Console.WriteLine();

            Console.WriteLine("ASIAN:");
            Console.WriteLine("  Chinese (Simplified & Traditional)");
            Console.WriteLine("  Japanese, Korean, Thai, Vietnamese");
            Console.WriteLine("  Hindi, Bengali, Tamil, Telugu");
            Console.WriteLine("  Indonesian, Malay");
            Console.WriteLine();

            Console.WriteLine("MIDDLE EASTERN:");
            Console.WriteLine("  Arabic, Hebrew, Farsi/Persian, Turkish, Urdu");
            Console.WriteLine();

            Console.WriteLine("CYRILLIC:");
            Console.WriteLine("  Russian, Ukrainian, Bulgarian, Serbian");
            Console.WriteLine();

            Console.WriteLine("And 100+ more!");
            Console.WriteLine();
            Console.WriteLine("Full list: https://ironsoftware.com/csharp/ocr/languages/");
        }
    }
}


// ============================================================================
// 125+ LANGUAGES - SIMPLE INSTALLATION
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Languages: https://ironsoftware.com/csharp/ocr/languages/
// ============================================================================
