/**
 * Asprise OCR vs IronOCR: Thread Limitations and Migration Comparison
 *
 * CRITICAL DIFFERENCE: Asprise implements license-tier thread restrictions.
 * - LITE/STANDARD: Single-thread, single-process ONLY
 * - ENTERPRISE: Multi-threading permitted
 *
 * IronOCR has NO such restrictions - all license tiers support multi-threading.
 *
 * This file demonstrates:
 * 1. Asprise thread limitation impact on .NET server applications
 * 2. Side-by-side migration patterns
 * 3. Why IronOCR is better suited for .NET production workloads
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspriseMigrationComparison
{
    // ========================================================================
    // THREAD LIMITATIONS: THE CRITICAL ASPRISE RESTRICTION
    // ========================================================================

    /// <summary>
    /// Demonstrates Asprise's license-tier thread restrictions.
    /// This is the single most important limitation for .NET developers.
    ///
    /// ASPRISE THREAD RESTRICTIONS BY TIER:
    /// - LITE (~$299): Single-thread, single-process only
    /// - STANDARD (~$699): Single-thread, single-process only
    /// - ENTERPRISE (contact sales): Multi-threading permitted
    ///
    /// IRONOCR: No thread restrictions on any tier
    /// </summary>
    public class AspriseThreadLimitations
    {
        /// <summary>
        /// ASPRISE LITE/STANDARD: This code VIOLATES license terms!
        /// Multi-threading is NOT permitted on lower tiers.
        /// </summary>
        public void AspriseProhibitedMultiThreading()
        {
            Console.WriteLine("=== ASPRISE LITE/STANDARD: PROHIBITED MULTI-THREADING ===");
            Console.WriteLine();
            Console.WriteLine("The following code VIOLATES Asprise LITE/STANDARD license:");
            Console.WriteLine();
            Console.WriteLine("  // LICENSE VIOLATION on LITE/STANDARD!");
            Console.WriteLine("  Parallel.ForEach(documents, document =>");
            Console.WriteLine("  {");
            Console.WriteLine("      var ocr = new Ocr();");
            Console.WriteLine("      ocr.StartEngine(\"eng\", SPEED_FAST);");
            Console.WriteLine("      var text = ocr.Recognize(document, ...);");
            Console.WriteLine("      ocr.StopEngine();");
            Console.WriteLine("  });");
            Console.WriteLine();
            Console.WriteLine("On LITE/STANDARD, you MUST process sequentially:");
            Console.WriteLine();
            Console.WriteLine("  // COMPLIANT but SLOW on LITE/STANDARD");
            Console.WriteLine("  foreach (var document in documents)");
            Console.WriteLine("  {");
            Console.WriteLine("      var ocr = new Ocr();");
            Console.WriteLine("      ocr.StartEngine(\"eng\", SPEED_FAST);");
            Console.WriteLine("      var text = ocr.Recognize(document, ...);");
            Console.WriteLine("      ocr.StopEngine();");
            Console.WriteLine("  }");
            Console.WriteLine();
            Console.WriteLine("For 100 documents @ 2 seconds each:");
            Console.WriteLine("  - Sequential: 200 seconds (3+ minutes)");
            Console.WriteLine("  - Parallel (8 cores): 25 seconds");
            Console.WriteLine();
            Console.WriteLine("ENTERPRISE tier required for production throughput.");
        }

        /// <summary>
        /// IronOCR: Multi-threading on ALL license tiers.
        /// No artificial restrictions.
        /// </summary>
        public void IronOcrUnrestrictedThreading()
        {
            Console.WriteLine("=== IRONOCR: UNRESTRICTED MULTI-THREADING ===");
            Console.WriteLine();
            Console.WriteLine("IronOCR permits multi-threading on ALL license tiers:");
            Console.WriteLine();
            Console.WriteLine("  // Works on IronOCR Lite, Plus, Professional, Unlimited");
            Console.WriteLine("  Parallel.ForEach(documents, document =>");
            Console.WriteLine("  {");
            Console.WriteLine("      var result = new IronTesseract().Read(document);");
            Console.WriteLine("      // Process result");
            Console.WriteLine("  });");
            Console.WriteLine();
            Console.WriteLine("Or with PLINQ:");
            Console.WriteLine();
            Console.WriteLine("  var results = documents");
            Console.WriteLine("      .AsParallel()");
            Console.WriteLine("      .Select(doc => new IronTesseract().Read(doc))");
            Console.WriteLine("      .ToList();");
            Console.WriteLine();
            Console.WriteLine("IronOCR Lite license ($749) supports full parallelism.");
            Console.WriteLine("No need to pay for Enterprise tier just for threading.");
        }
    }

    // ========================================================================
    // SERVER APPLICATION IMPACT
    // ========================================================================

    /// <summary>
    /// Demonstrates why Asprise LITE/STANDARD cannot be used
    /// for typical .NET server applications.
    /// </summary>
    public class AsprisServerApplicationLimitations
    {
        /// <summary>
        /// ASP.NET Core Web API: Inherently multi-threaded.
        /// Asprise LITE/STANDARD violates license in this context.
        /// </summary>
        public void AspNetCoreViolation()
        {
            Console.WriteLine("=== ASP.NET CORE WEB API: ASPRISE LICENSE VIOLATION ===");
            Console.WriteLine();
            Console.WriteLine("ASP.NET Core processes requests on multiple threads.");
            Console.WriteLine("Using Asprise LITE/STANDARD in a Web API violates license.");
            Console.WriteLine();
            Console.WriteLine("// Controller using Asprise - LICENSE VIOLATION on LITE/STANDARD");
            Console.WriteLine("[ApiController]");
            Console.WriteLine("public class OcrController : ControllerBase");
            Console.WriteLine("{");
            Console.WriteLine("    [HttpPost(\"ocr\")]");
            Console.WriteLine("    public IActionResult ProcessDocument(IFormFile file)");
            Console.WriteLine("    {");
            Console.WriteLine("        // Multiple requests = multiple threads");
            Console.WriteLine("        // VIOLATES LITE/STANDARD license!");
            Console.WriteLine("        var ocr = new Ocr();");
            Console.WriteLine("        ocr.StartEngine(\"eng\", SPEED_FAST);");
            Console.WriteLine("        var text = ocr.Recognize(path, ...);");
            Console.WriteLine("        ocr.StopEngine();");
            Console.WriteLine("        return Ok(text);");
            Console.WriteLine("    }");
            Console.WriteLine("}");
            Console.WriteLine();
            Console.WriteLine("ENTERPRISE tier required for ANY Web API usage.");
        }

        /// <summary>
        /// Windows Service: Typically multi-threaded for performance.
        /// Asprise LITE/STANDARD cannot be used effectively.
        /// </summary>
        public void WindowsServiceLimitation()
        {
            Console.WriteLine("=== WINDOWS SERVICE: ASPRISE LIMITATION ===");
            Console.WriteLine();
            Console.WriteLine("Background processing services typically use");
            Console.WriteLine("multiple threads for throughput.");
            Console.WriteLine();
            Console.WriteLine("// Background service with Asprise LITE/STANDARD");
            Console.WriteLine("// MUST process one document at a time!");
            Console.WriteLine("protected override async Task ExecuteAsync(...)");
            Console.WriteLine("{");
            Console.WriteLine("    while (!stoppingToken.IsCancellationRequested)");
            Console.WriteLine("    {");
            Console.WriteLine("        var pending = await GetPendingDocuments();");
            Console.WriteLine("        ");
            Console.WriteLine("        // SEQUENTIAL ONLY on LITE/STANDARD!");
            Console.WriteLine("        foreach (var doc in pending)");
            Console.WriteLine("        {");
            Console.WriteLine("            await ProcessSingleDocumentAsync(doc);");
            Console.WriteLine("        }");
            Console.WriteLine("    }");
            Console.WriteLine("}");
            Console.WriteLine();
            Console.WriteLine("Processing 1000 docs/day @ 2 sec each:");
            Console.WriteLine("  - Sequential: 33+ minutes/batch");
            Console.WriteLine("  - Parallel (8 threads): ~4 minutes/batch");
        }

        /// <summary>
        /// Azure Functions / AWS Lambda: Parallel execution by design.
        /// Asprise LITE/STANDARD incompatible.
        /// </summary>
        public void CloudFunctionsIncompatibility()
        {
            Console.WriteLine("=== CLOUD FUNCTIONS: ASPRISE INCOMPATIBLE ===");
            Console.WriteLine();
            Console.WriteLine("Azure Functions and AWS Lambda execute in parallel.");
            Console.WriteLine("Multiple instances handle concurrent requests.");
            Console.WriteLine();
            Console.WriteLine("Asprise LITE license restriction:");
            Console.WriteLine("  'Single process only'");
            Console.WriteLine();
            Console.WriteLine("Cloud functions = multiple processes by definition!");
            Console.WriteLine();
            Console.WriteLine("ENTERPRISE required for ANY cloud function usage.");
            Console.WriteLine();
            Console.WriteLine("IronOCR: Works in all cloud function scenarios");
            Console.WriteLine("on any license tier.");
        }
    }

    // ========================================================================
    // MIGRATION PATTERNS: ASPRISE TO IRONOCR
    // ========================================================================

    /// <summary>
    /// Side-by-side migration examples showing how to convert
    /// Asprise code to IronOCR.
    /// </summary>
    public class AspriseMigrationPatterns
    {
        /// <summary>
        /// Pattern 1: Basic text extraction migration.
        /// </summary>
        public void MigrateBasicExtraction()
        {
            Console.WriteLine("=== MIGRATION: BASIC TEXT EXTRACTION ===");
            Console.WriteLine();
            Console.WriteLine("ASPRISE (15 lines):");
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine("  Ocr.SetUp();");
            Console.WriteLine("  Ocr ocr = new Ocr();");
            Console.WriteLine("  try");
            Console.WriteLine("  {");
            Console.WriteLine("      ocr.StartEngine(\"eng\", Ocr.SPEED_FAST);");
            Console.WriteLine("      string text = ocr.Recognize(");
            Console.WriteLine("          imagePath,");
            Console.WriteLine("          Ocr.RECOGNIZE_TYPE_TEXT,");
            Console.WriteLine("          Ocr.OUTPUT_FORMAT_PLAINTEXT);");
            Console.WriteLine("      return text;");
            Console.WriteLine("  }");
            Console.WriteLine("  finally");
            Console.WriteLine("  {");
            Console.WriteLine("      ocr.StopEngine();");
            Console.WriteLine("  }");
            Console.WriteLine();
            Console.WriteLine("IRONOCR (1 line):");
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine("  return new IronTesseract().Read(imagePath).Text;");
            Console.WriteLine();
            Console.WriteLine("Reduction: 15 lines → 1 line (93% less code)");
        }

        /// <summary>
        /// Pattern 2: Batch processing migration.
        /// </summary>
        public void MigrateBatchProcessing()
        {
            Console.WriteLine("=== MIGRATION: BATCH PROCESSING ===");
            Console.WriteLine();
            Console.WriteLine("ASPRISE LITE/STANDARD (sequential only):");
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine("  var results = new List<string>();");
            Console.WriteLine("  Ocr.SetUp();");
            Console.WriteLine("  Ocr ocr = new Ocr();");
            Console.WriteLine("  try");
            Console.WriteLine("  {");
            Console.WriteLine("      ocr.StartEngine(\"eng\", Ocr.SPEED_FAST);");
            Console.WriteLine("      foreach (var path in imagePaths)  // MUST be sequential!");
            Console.WriteLine("      {");
            Console.WriteLine("          string text = ocr.Recognize(path, ...);");
            Console.WriteLine("          results.Add(text);");
            Console.WriteLine("      }");
            Console.WriteLine("  }");
            Console.WriteLine("  finally");
            Console.WriteLine("  {");
            Console.WriteLine("      ocr.StopEngine();");
            Console.WriteLine("  }");
            Console.WriteLine("  return results;");
            Console.WriteLine();
            Console.WriteLine("IRONOCR (parallel on ALL tiers):");
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine("  return imagePaths");
            Console.WriteLine("      .AsParallel()  // No tier restrictions");
            Console.WriteLine("      .Select(path => new IronTesseract().Read(path).Text)");
            Console.WriteLine("      .ToList();");
            Console.WriteLine();
            Console.WriteLine("Performance improvement: 4-16x faster with parallelism");
        }

        /// <summary>
        /// Pattern 3: PDF processing migration.
        /// Asprise has limited PDF support; IronOCR has native PDF.
        /// </summary>
        public void MigratePdfProcessing()
        {
            Console.WriteLine("=== MIGRATION: PDF PROCESSING ===");
            Console.WriteLine();
            Console.WriteLine("ASPRISE (no direct PDF support):");
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine("  // 1. Use external library to render PDF to images");
            Console.WriteLine("  var images = ExternalPdfLibrary.RenderPages(pdfPath);");
            Console.WriteLine("  ");
            Console.WriteLine("  // 2. Process each image separately");
            Console.WriteLine("  Ocr ocr = new Ocr();");
            Console.WriteLine("  ocr.StartEngine(\"eng\", Ocr.SPEED_FAST);");
            Console.WriteLine("  foreach (var image in images)");
            Console.WriteLine("  {");
            Console.WriteLine("      string pageText = ocr.Recognize(image, ...);");
            Console.WriteLine("      // Combine results");
            Console.WriteLine("  }");
            Console.WriteLine("  ocr.StopEngine();");
            Console.WriteLine();
            Console.WriteLine("IRONOCR (native PDF support):");
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine("  using var input = new OcrInput();");
            Console.WriteLine("  input.LoadPdf(pdfPath);  // Direct PDF support");
            Console.WriteLine("  return new IronTesseract().Read(input).Text;");
            Console.WriteLine();
            Console.WriteLine("IronOCR: No external PDF library required");
        }

        /// <summary>
        /// Pattern 4: Preprocessing migration.
        /// Asprise requires manual preprocessing; IronOCR has built-in.
        /// </summary>
        public void MigratePreprocessing()
        {
            Console.WriteLine("=== MIGRATION: IMAGE PREPROCESSING ===");
            Console.WriteLine();
            Console.WriteLine("ASPRISE (manual preprocessing required):");
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine("  // Must use external image library for preprocessing");
            Console.WriteLine("  // 1. Deskew with ImageMagick or similar");
            Console.WriteLine("  // 2. Denoise with external tool");
            Console.WriteLine("  // 3. Enhance contrast manually");
            Console.WriteLine("  // 4. Then pass to Asprise");
            Console.WriteLine("  var ocr = new Ocr();");
            Console.WriteLine("  ocr.StartEngine(\"eng\", Ocr.SPEED_FAST);");
            Console.WriteLine("  string text = ocr.Recognize(preprocessedImagePath, ...);");
            Console.WriteLine("  ocr.StopEngine();");
            Console.WriteLine();
            Console.WriteLine("IRONOCR (built-in preprocessing):");
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine("  using var input = new OcrInput();");
            Console.WriteLine("  input.LoadImage(imagePath);");
            Console.WriteLine("  input.Deskew();      // Built-in deskew");
            Console.WriteLine("  input.DeNoise();     // Built-in denoise");
            Console.WriteLine("  input.Contrast();    // Built-in contrast");
            Console.WriteLine("  return new IronTesseract().Read(input).Text;");
            Console.WriteLine();
            Console.WriteLine("IronOCR: No external dependencies for preprocessing");
        }

        /// <summary>
        /// Pattern 5: Async/await migration.
        /// Asprise has no native async; IronOCR supports full async.
        /// </summary>
        public void MigrateAsyncPattern()
        {
            Console.WriteLine("=== MIGRATION: ASYNC/AWAIT PATTERN ===");
            Console.WriteLine();
            Console.WriteLine("ASPRISE (no native async support):");
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine("  // Must wrap synchronous code in Task.Run");
            Console.WriteLine("  public async Task<string> ProcessAsync(string path)");
            Console.WriteLine("  {");
            Console.WriteLine("      return await Task.Run(() =>");
            Console.WriteLine("      {");
            Console.WriteLine("          // Still violates LITE/STANDARD if called concurrently!");
            Console.WriteLine("          var ocr = new Ocr();");
            Console.WriteLine("          ocr.StartEngine(\"eng\", Ocr.SPEED_FAST);");
            Console.WriteLine("          string text = ocr.Recognize(path, ...);");
            Console.WriteLine("          ocr.StopEngine();");
            Console.WriteLine("          return text;");
            Console.WriteLine("      });");
            Console.WriteLine("  }");
            Console.WriteLine();
            Console.WriteLine("IRONOCR (native async support):");
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine("  public async Task<string> ProcessAsync(string path)");
            Console.WriteLine("  {");
            Console.WriteLine("      using var input = new OcrInput();");
            Console.WriteLine("      await input.LoadImageAsync(path);");
            Console.WriteLine("      var result = await new IronTesseract().ReadAsync(input);");
            Console.WriteLine("      return result.Text;");
            Console.WriteLine("  }");
            Console.WriteLine();
            Console.WriteLine("IronOCR: True async pattern, not wrapped Task.Run");
        }
    }

    // ========================================================================
    // COST COMPARISON
    // ========================================================================

    /// <summary>
    /// Compares total cost of ownership for production scenarios.
    /// </summary>
    public class AspriseCostComparison
    {
        /// <summary>
        /// For production server applications, Asprise ENTERPRISE is required.
        /// Compare this to IronOCR where any tier works.
        /// </summary>
        public void CompareProductionCosts()
        {
            Console.WriteLine("=== PRODUCTION COST COMPARISON ===");
            Console.WriteLine();
            Console.WriteLine("SCENARIO: ASP.NET Web API processing OCR requests");
            Console.WriteLine();
            Console.WriteLine("ASPRISE:");
            Console.WriteLine("  LITE ($299): Cannot use - server apps prohibited");
            Console.WriteLine("  STANDARD ($699): Cannot use - multi-thread prohibited");
            Console.WriteLine("  ENTERPRISE (contact sales): Required for any server use");
            Console.WriteLine("  Estimated: $2,000-5,000+ (enterprise pricing varies)");
            Console.WriteLine();
            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  Lite ($749): Full server support, multi-threading");
            Console.WriteLine("  Plus ($999): Same + additional features");
            Console.WriteLine("  Professional ($1,499): Same + priority support");
            Console.WriteLine();
            Console.WriteLine("For typical server workload:");
            Console.WriteLine("  Asprise minimum: ~$2,000+ (ENTERPRISE required)");
            Console.WriteLine("  IronOCR minimum: $749 (Lite tier sufficient)");
            Console.WriteLine();
            Console.WriteLine("Savings with IronOCR: $1,250+ per license");
        }
    }

    // ========================================================================
    // MAIN DEMONSTRATION
    // ========================================================================

    /// <summary>
    /// Entry point demonstrating thread limitations and migration patterns.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ASPRISE vs IRONOCR: THREAD LIMITATIONS & MIGRATION      ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Thread limitations
            Console.WriteLine("PART 1: THREAD LIMITATIONS");
            Console.WriteLine("══════════════════════════════════════════════════════════");
            var threadDemo = new AspriseThreadLimitations();
            threadDemo.AspriseProhibitedMultiThreading();
            Console.WriteLine();
            threadDemo.IronOcrUnrestrictedThreading();
            Console.WriteLine();

            // Server application impact
            Console.WriteLine("PART 2: SERVER APPLICATION IMPACT");
            Console.WriteLine("══════════════════════════════════════════════════════════");
            var serverDemo = new AsprisServerApplicationLimitations();
            serverDemo.AspNetCoreViolation();
            Console.WriteLine();
            serverDemo.WindowsServiceLimitation();
            Console.WriteLine();
            serverDemo.CloudFunctionsIncompatibility();
            Console.WriteLine();

            // Migration patterns
            Console.WriteLine("PART 3: MIGRATION PATTERNS");
            Console.WriteLine("══════════════════════════════════════════════════════════");
            var migrationDemo = new AspriseMigrationPatterns();
            migrationDemo.MigrateBasicExtraction();
            Console.WriteLine();
            migrationDemo.MigrateBatchProcessing();
            Console.WriteLine();
            migrationDemo.MigratePdfProcessing();
            Console.WriteLine();
            migrationDemo.MigratePreprocessing();
            Console.WriteLine();
            migrationDemo.MigrateAsyncPattern();
            Console.WriteLine();

            // Cost comparison
            Console.WriteLine("PART 4: COST COMPARISON");
            Console.WriteLine("══════════════════════════════════════════════════════════");
            var costDemo = new AspriseCostComparison();
            costDemo.CompareProductionCosts();
            Console.WriteLine();

            Console.WriteLine("══════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("SUMMARY:");
            Console.WriteLine("Asprise LITE/STANDARD have severe thread restrictions.");
            Console.WriteLine("Most .NET production scenarios require ENTERPRISE tier.");
            Console.WriteLine();
            Console.WriteLine("IronOCR has no such restrictions on any license tier.");
            Console.WriteLine("For .NET server applications, IronOCR is the clear choice.");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
            Console.WriteLine("NuGet: Install-Package IronOcr");
        }
    }
}
