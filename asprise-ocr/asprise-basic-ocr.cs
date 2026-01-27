/**
 * Asprise OCR Basic Examples: Understanding the Java-First Architecture
 *
 * This file demonstrates Asprise OCR patterns and their limitations
 * for .NET developers. Asprise is fundamentally a Java library with
 * .NET support added via native library wrappers.
 *
 * CRITICAL LIMITATIONS:
 * - LITE/STANDARD tiers: Single-thread, single-process only
 * - ENTERPRISE required for server applications
 * - Native binary deployment required for each platform
 * - Java-centric documentation requires translation for C# developers
 *
 * For native .NET OCR without these restrictions:
 * IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 */

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AspriseBasicExamples
{
    // ========================================================================
    // ASPRISE OCR - JAVA-FIRST ARCHITECTURE EXPLAINED
    // ========================================================================

    /// <summary>
    /// Asprise OCR for .NET wraps native C++ libraries that were originally
    /// designed for Java interop. This creates several challenges:
    ///
    /// 1. Native binary management (aocr.dll, aocr_x64.dll)
    /// 2. Platform-specific deployment
    /// 3. Manual resource cleanup (no IDisposable)
    /// 4. Error handling via return codes instead of exceptions
    /// </summary>
    public class AspriseOcrBasicUsage
    {
        /*
         * NOTE: Asprise requires the native library to be present.
         * This is a simplified demonstration of the API pattern.
         *
         * In production, you would need:
         * - aocr.dll (32-bit Windows)
         * - aocr_x64.dll (64-bit Windows)
         * - libaocr.so (Linux)
         * - libaocr.dylib (macOS)
         */

        /// <summary>
        /// Basic text extraction pattern in Asprise.
        /// Note the manual engine lifecycle management required.
        /// </summary>
        public string ExtractTextBasic(string imagePath)
        {
            // Asprise pattern: Static setup call (Java heritage)
            // Ocr.SetUp();

            // Asprise pattern: Explicit engine initialization
            // Ocr ocr = new Ocr();
            // ocr.StartEngine("eng", Ocr.SPEED_FAST);

            // Asprise pattern: Recognition with type and format constants
            // string text = ocr.Recognize(imagePath,
            //     Ocr.RECOGNIZE_TYPE_TEXT,
            //     Ocr.OUTPUT_FORMAT_PLAINTEXT);

            // Asprise pattern: CRITICAL - Must manually stop engine
            // Failure to call StopEngine() leaks native resources
            // ocr.StopEngine();

            // COMPARISON WITH IRONOCR:
            // return new IronTesseract().Read(imagePath).Text;
            // - No setup required
            // - No manual cleanup
            // - Returns immediately

            Console.WriteLine("Asprise requires:");
            Console.WriteLine("1. Static SetUp() call");
            Console.WriteLine("2. StartEngine() with language");
            Console.WriteLine("3. Recognize() with type constants");
            Console.WriteLine("4. StopEngine() for cleanup");
            Console.WriteLine();
            Console.WriteLine("IronOCR requires:");
            Console.WriteLine("new IronTesseract().Read(imagePath).Text");

            return string.Empty;
        }

        /// <summary>
        /// Multi-language OCR in Asprise.
        /// Note: Asprise supports 20+ languages vs IronOCR's 125+.
        /// </summary>
        public string ExtractTextMultiLanguage(string imagePath, string languages)
        {
            // Asprise pattern: Multiple languages via string parameter
            // Ocr ocr = new Ocr();
            // ocr.StartEngine(languages, Ocr.SPEED_SLOW);

            // Language parameter format: "eng+fra+deu"
            // Combined with '+' character

            // COMPARISON - IronOCR fluent language API:
            // var ocr = new IronTesseract();
            // ocr.Language = OcrLanguage.English;
            // ocr.AddSecondaryLanguage(OcrLanguage.French);
            // ocr.AddSecondaryLanguage(OcrLanguage.German);
            // return ocr.Read(imagePath).Text;

            Console.WriteLine($"Asprise language support: 20+ languages");
            Console.WriteLine($"IronOCR language support: 125+ languages");
            Console.WriteLine();
            Console.WriteLine("IronOCR provides strongly-typed language enum");
            Console.WriteLine("vs Asprise string-based language codes");

            return string.Empty;
        }
    }

    // ========================================================================
    // NATIVE BINARY DEPLOYMENT CHALLENGES
    // ========================================================================

    /// <summary>
    /// Demonstrates the native binary deployment complexity
    /// that Asprise requires vs IronOCR's pure NuGet approach.
    /// </summary>
    public class AspriseNativeBinaryManagement
    {
        /// <summary>
        /// Asprise requires platform-specific native binaries.
        /// This class shows what deployment looks like.
        /// </summary>
        public void ExplainDeploymentRequirements()
        {
            Console.WriteLine("=== ASPRISE DEPLOYMENT REQUIREMENTS ===");
            Console.WriteLine();
            Console.WriteLine("Windows 32-bit: aocr.dll required");
            Console.WriteLine("Windows 64-bit: aocr_x64.dll required");
            Console.WriteLine("Linux: libaocr.so required");
            Console.WriteLine("macOS: libaocr.dylib required");
            Console.WriteLine();
            Console.WriteLine("Each must be in PATH or application directory");
            Console.WriteLine();
            Console.WriteLine("=== IRONOCR DEPLOYMENT ===");
            Console.WriteLine();
            Console.WriteLine("All platforms: Install-Package IronOcr");
            Console.WriteLine("NuGet handles all native dependencies automatically");
        }

        /// <summary>
        /// Shows what can go wrong with native binary deployment.
        /// These are real errors developers encounter.
        /// </summary>
        public void CommonDeploymentErrors()
        {
            Console.WriteLine("=== COMMON ASPRISE DEPLOYMENT ERRORS ===");
            Console.WriteLine();

            // Error 1: Missing DLL
            Console.WriteLine("ERROR: Unable to load DLL 'aocr.dll'");
            Console.WriteLine("CAUSE: Native binary not deployed");
            Console.WriteLine("FIX: Copy aocr.dll to bin directory");
            Console.WriteLine();

            // Error 2: Architecture mismatch
            Console.WriteLine("ERROR: BadImageFormatException");
            Console.WriteLine("CAUSE: 32-bit DLL loaded in 64-bit process (or vice versa)");
            Console.WriteLine("FIX: Use correct architecture DLL");
            Console.WriteLine();

            // Error 3: Linux/macOS
            Console.WriteLine("ERROR: DllNotFoundException on Linux");
            Console.WriteLine("CAUSE: libaocr.so not in LD_LIBRARY_PATH");
            Console.WriteLine("FIX: Set LD_LIBRARY_PATH or copy to /usr/lib");
            Console.WriteLine();

            // IronOCR comparison
            Console.WriteLine("=== IRONOCR: NO SUCH ERRORS ===");
            Console.WriteLine("NuGet deployment handles all native dependencies");
            Console.WriteLine("Works on Windows, Linux, macOS without manual config");
        }
    }

    // ========================================================================
    // ENGINE LIFECYCLE MANAGEMENT
    // ========================================================================

    /// <summary>
    /// Asprise requires explicit engine lifecycle management.
    /// This is different from .NET's IDisposable pattern.
    /// </summary>
    public class AspriseEngineLifecycle
    {
        // Asprise engine constants (from Java heritage)
        public const int SPEED_FASTEST = 0;
        public const int SPEED_FAST = 1;
        public const int SPEED_SLOW = 2;
        public const int SPEED_SLOWEST = 3;

        public const int RECOGNIZE_TYPE_TEXT = 1;
        public const int RECOGNIZE_TYPE_BARCODE = 2;
        public const int RECOGNIZE_TYPE_ALL = 3;

        public const int OUTPUT_FORMAT_PLAINTEXT = 1;
        public const int OUTPUT_FORMAT_XML = 2;
        public const int OUTPUT_FORMAT_PDF = 3;

        /// <summary>
        /// Demonstrates the full Asprise engine lifecycle.
        /// Note: No IDisposable pattern - manual cleanup required.
        /// </summary>
        public void DemonstrateEngineLifecycle()
        {
            Console.WriteLine("=== ASPRISE ENGINE LIFECYCLE ===");
            Console.WriteLine();

            // Step 1: Static setup (required before any usage)
            Console.WriteLine("Step 1: Ocr.SetUp()");
            Console.WriteLine("        Global initialization, must be called once");
            Console.WriteLine();

            // Step 2: Create instance
            Console.WriteLine("Step 2: Ocr ocr = new Ocr()");
            Console.WriteLine("        Creates wrapper object");
            Console.WriteLine();

            // Step 3: Start engine
            Console.WriteLine("Step 3: ocr.StartEngine(\"eng\", SPEED_FAST)");
            Console.WriteLine("        Loads Tesseract engine with language");
            Console.WriteLine("        Allocates native memory");
            Console.WriteLine();

            // Step 4: Perform OCR
            Console.WriteLine("Step 4: ocr.Recognize(path, type, format)");
            Console.WriteLine("        Actual OCR operation");
            Console.WriteLine();

            // Step 5: CRITICAL - Stop engine
            Console.WriteLine("Step 5: ocr.StopEngine()");
            Console.WriteLine("        CRITICAL: Must call to free native resources");
            Console.WriteLine("        Forgetting this causes memory leaks");
            Console.WriteLine();

            Console.WriteLine("=== IRONOCR: NO LIFECYCLE MANAGEMENT ===");
            Console.WriteLine();
            Console.WriteLine("var result = new IronTesseract().Read(imagePath);");
            Console.WriteLine("- No setup required");
            Console.WriteLine("- No engine start/stop");
            Console.WriteLine("- Automatic resource management");
        }

        /// <summary>
        /// Shows the memory leak risk with Asprise.
        /// </summary>
        public void MemoryLeakRisk()
        {
            Console.WriteLine("=== ASPRISE MEMORY LEAK RISK ===");
            Console.WriteLine();
            Console.WriteLine("BAD CODE (leaks memory):");
            Console.WriteLine();
            Console.WriteLine("  Ocr ocr = new Ocr();");
            Console.WriteLine("  ocr.StartEngine(\"eng\", SPEED_FAST);");
            Console.WriteLine("  string text = ocr.Recognize(path, ...);");
            Console.WriteLine("  // FORGOT TO CALL StopEngine()!");
            Console.WriteLine("  // Native memory leaked");
            Console.WriteLine();
            Console.WriteLine("CORRECT CODE:");
            Console.WriteLine();
            Console.WriteLine("  Ocr ocr = new Ocr();");
            Console.WriteLine("  try {");
            Console.WriteLine("      ocr.StartEngine(\"eng\", SPEED_FAST);");
            Console.WriteLine("      string text = ocr.Recognize(path, ...);");
            Console.WriteLine("  } finally {");
            Console.WriteLine("      ocr.StopEngine();  // Always cleanup");
            Console.WriteLine("  }");
            Console.WriteLine();
            Console.WriteLine("IRONOCR - NO SUCH RISK:");
            Console.WriteLine();
            Console.WriteLine("  using var input = new OcrInput();");
            Console.WriteLine("  input.LoadImage(path);");
            Console.WriteLine("  var result = new IronTesseract().Read(input);");
            Console.WriteLine("  // 'using' handles cleanup automatically");
        }
    }

    // ========================================================================
    // ERROR HANDLING COMPARISON
    // ========================================================================

    /// <summary>
    /// Asprise uses error codes from its Java/C heritage.
    /// This is different from .NET's exception-based error handling.
    /// </summary>
    public class AspriseErrorHandling
    {
        /// <summary>
        /// Shows error handling pattern differences.
        /// </summary>
        public void CompareErrorHandling()
        {
            Console.WriteLine("=== ERROR HANDLING COMPARISON ===");
            Console.WriteLine();
            Console.WriteLine("ASPRISE (error code pattern):");
            Console.WriteLine();
            Console.WriteLine("  string result = ocr.Recognize(path, type, format);");
            Console.WriteLine("  if (result == null || result.StartsWith(\"ERROR:\")) {");
            Console.WriteLine("      // Handle error manually");
            Console.WriteLine("      // Parse error string for details");
            Console.WriteLine("  }");
            Console.WriteLine();
            Console.WriteLine("IRONOCR (exception pattern - standard .NET):");
            Console.WriteLine();
            Console.WriteLine("  try {");
            Console.WriteLine("      var result = ocr.Read(imagePath);");
            Console.WriteLine("  } catch (OcrException ex) {");
            Console.WriteLine("      // Strongly-typed exception");
            Console.WriteLine("      // Standard .NET pattern");
            Console.WriteLine("      // Full stack trace");
            Console.WriteLine("  }");
        }
    }

    // ========================================================================
    // MAIN DEMONSTRATION
    // ========================================================================

    /// <summary>
    /// Entry point demonstrating Asprise patterns and limitations.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ASPRISE OCR: JAVA-FIRST ARCHITECTURE DEMONSTRATION  ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Demonstrate native binary issues
            var nativeDemo = new AspriseNativeBinaryManagement();
            nativeDemo.ExplainDeploymentRequirements();
            Console.WriteLine();
            nativeDemo.CommonDeploymentErrors();
            Console.WriteLine();

            // Demonstrate engine lifecycle
            var lifecycleDemo = new AspriseEngineLifecycle();
            lifecycleDemo.DemonstrateEngineLifecycle();
            Console.WriteLine();
            lifecycleDemo.MemoryLeakRisk();
            Console.WriteLine();

            // Demonstrate error handling
            var errorDemo = new AspriseErrorHandling();
            errorDemo.CompareErrorHandling();
            Console.WriteLine();

            Console.WriteLine("════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("SUMMARY: Asprise OCR is a Java-first library.");
            Console.WriteLine(".NET developers face native binary deployment,");
            Console.WriteLine("manual resource management, and Java-style APIs.");
            Console.WriteLine();
            Console.WriteLine("For native .NET OCR, consider IronOCR:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
            Console.WriteLine("NuGet: Install-Package IronOcr");
        }
    }
}
