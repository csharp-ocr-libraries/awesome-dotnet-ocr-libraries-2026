/**
 * Google Cloud Vision OCR: Security Considerations
 *
 * Your documents are uploaded to Google's servers.
 * Important considerations for regulated industries.
 *
 * Get IronOCR for on-premise OCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 */

using System;

// ============================================================================
// GOOGLE CLOUD VISION SECURITY CONCERNS
// ============================================================================

namespace GoogleVisionSecurity
{
    /// <summary>
    /// Google Cloud Vision processes documents on Google's infrastructure.
    /// Consider these factors for regulated industries.
    /// </summary>
    public class SecurityAnalysis
    {
        public void ShowSecurityConsiderations()
        {
            Console.WriteLine("=== GOOGLE CLOUD VISION SECURITY CONSIDERATIONS ===\n");

            Console.WriteLine("DATA TRANSMISSION:");
            Console.WriteLine("  - Documents uploaded to Google servers");
            Console.WriteLine("  - Data travels over internet (TLS encrypted)");
            Console.WriteLine("  - Stored temporarily during processing");
            Console.WriteLine("  - Google's data retention policies apply");
            Console.WriteLine();

            Console.WriteLine("DATA RESIDENCY:");
            Console.WriteLine("  - Multi-region by default");
            Console.WriteLine("  - Regional endpoints available (extra config)");
            Console.WriteLine("  - Data may cross international boundaries");
            Console.WriteLine();

            Console.WriteLine("COMPLIANCE CERTIFICATIONS:");
            Console.WriteLine("  - SOC 1/2/3");
            Console.WriteLine("  - ISO 27001");
            Console.WriteLine("  - FedRAMP (limited scope)");
            Console.WriteLine("  - HIPAA BAA available");
            Console.WriteLine();

            Console.WriteLine("POTENTIAL CONCERNS:");
            Console.WriteLine("  - Third-party data handling");
            Console.WriteLine("  - Vendor lock-in");
            Console.WriteLine("  - Internet dependency");
            Console.WriteLine("  - Usage data collection");
            Console.WriteLine();

            Console.WriteLine("INDUSTRIES WITH STRICTER REQUIREMENTS:");
            Console.WriteLine("  - Defense/Military (ITAR)");
            Console.WriteLine("  - Government classified (FedRAMP High, CMMC)");
            Console.WriteLine("  - Healthcare (HIPAA - verify BAA scope)");
            Console.WriteLine("  - Finance (SOX, PCI-DSS)");
            Console.WriteLine("  - Legal (attorney-client privilege)");
            Console.WriteLine();

            Console.WriteLine("For on-premise processing, consider IronOCR:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }

        public void ShowAirGappedLimitations()
        {
            Console.WriteLine("=== AIR-GAPPED NETWORK LIMITATIONS ===\n");

            Console.WriteLine("Google Cloud Vision CANNOT work in:");
            Console.WriteLine("  ✗ Air-gapped networks");
            Console.WriteLine("  ✗ Classified environments");
            Console.WriteLine("  ✗ Networks without internet access");
            Console.WriteLine("  ✗ Isolated data centers");
            Console.WriteLine();

            Console.WriteLine("IronOCR works everywhere:");
            Console.WriteLine("  ✓ Air-gapped networks");
            Console.WriteLine("  ✓ Classified environments");
            Console.WriteLine("  ✓ Offline operations");
            Console.WriteLine("  ✓ Any network configuration");
            Console.WriteLine();
            Console.WriteLine("Download: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - COMPLETE DATA SOVEREIGNTY
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrSecurityExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR processes everything locally.
    /// Your data never leaves your infrastructure.
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class SecureOcrService
    {
        /// <summary>
        /// All processing stays local
        /// </summary>
        public string ProcessSecureDocument(string documentPath)
        {
            // No cloud upload
            // No internet required
            // No third-party handling
            return new IronTesseract().Read(documentPath).Text;
        }

        /// <summary>
        /// Works in air-gapped environments
        /// </summary>
        public string ProcessInAirGappedNetwork(string documentPath)
        {
            // Perfect for classified environments
            return new IronTesseract().Read(documentPath).Text;
        }

        /// <summary>
        /// HIPAA-compliant processing
        /// </summary>
        public string ProcessPatientRecords(string documentPath)
        {
            // PHI never leaves your servers
            // No BAA required with third party
            // Full control over data lifecycle
            var result = new IronTesseract().Read(documentPath);

            // Process locally, store locally
            return result.Text;
        }

        /// <summary>
        /// ITAR-compliant processing
        /// </summary>
        public string ProcessDefenseDocuments(string documentPath)
        {
            // Technical data stays on US soil
            // No foreign server access
            // Meets defense contractor requirements
            return new IronTesseract().Read(documentPath).Text;
        }

        /// <summary>
        /// Legal document processing
        /// </summary>
        public string ProcessLegalDocuments(string documentPath)
        {
            // Attorney-client privilege protected
            // No third-party exposure
            return new IronTesseract().Read(documentPath).Text;
        }
    }

    public class ComplianceMatrix
    {
        public void ShowComplianceComparison()
        {
            Console.WriteLine("=== COMPLIANCE COMPARISON ===\n");

            Console.WriteLine("Requirement       | Google Vision  | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────────");
            Console.WriteLine("Data on-premise   | No             | Yes");
            Console.WriteLine("Air-gapped        | No             | Yes");
            Console.WriteLine("No 3rd party      | No             | Yes");
            Console.WriteLine("HIPAA (full)      | BAA required   | Native");
            Console.WriteLine("ITAR              | Complex        | Native");
            Console.WriteLine("FedRAMP High      | Limited        | N/A (local)");
            Console.WriteLine("CMMC              | Complex        | Native");
            Console.WriteLine("GDPR Art 28       | DPA required   | N/A (local)");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// REGULATED INDUSTRY? CHOOSE ON-PREMISE.
//
// IronOCR keeps your sensitive documents where they belong - on your servers.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Compliance: Your data, your infrastructure, your control.
// ============================================================================
