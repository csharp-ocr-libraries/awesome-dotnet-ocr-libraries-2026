/**
 * Azure Computer Vision OCR: Cost Analysis
 *
 * Calculate your Azure OCR costs vs IronOCR one-time license.
 * Azure charges per transaction; costs add up quickly.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 */

using System;

// ============================================================================
// AZURE COMPUTER VISION PRICING BREAKDOWN
// ============================================================================

namespace AzureCostAnalysis
{
    /// <summary>
    /// Azure Computer Vision Read API pricing (as of 2025):
    /// - Free tier: 5,000 transactions/month
    /// - S1: $1.00 per 1,000 transactions (0-1M)
    /// - S2: $0.60 per 1,000 transactions (1M-10M)
    /// - S3: $0.40 per 1,000 transactions (10M+)
    ///
    /// Each page/image = 1 transaction
    /// Multi-page PDF = multiple transactions
    /// </summary>
    public class AzureCostCalculator
    {
        // Azure pricing tiers (per 1,000 transactions)
        private const decimal Tier1Price = 1.00m;   // 0-1M
        private const decimal Tier2Price = 0.60m;   // 1M-10M
        private const decimal Tier3Price = 0.40m;   // 10M+

        /// <summary>
        /// Calculate monthly Azure OCR cost
        /// </summary>
        public decimal CalculateMonthlyCost(int documentsPerMonth, int avgPagesPerDocument = 1)
        {
            int totalTransactions = documentsPerMonth * avgPagesPerDocument;

            // Free tier
            if (totalTransactions <= 5000)
                return 0;

            // Subtract free tier
            int billableTransactions = totalTransactions - 5000;

            // Calculate tiered pricing
            decimal cost = 0;

            if (billableTransactions <= 1_000_000)
            {
                cost = (billableTransactions / 1000m) * Tier1Price;
            }
            else if (billableTransactions <= 10_000_000)
            {
                cost = 1000 * Tier1Price; // First 1M
                cost += ((billableTransactions - 1_000_000) / 1000m) * Tier2Price;
            }
            else
            {
                cost = 1000 * Tier1Price; // First 1M
                cost += 9000 * Tier2Price; // Next 9M
                cost += ((billableTransactions - 10_000_000) / 1000m) * Tier3Price;
            }

            return cost;
        }

        /// <summary>
        /// Show cost comparisons
        /// </summary>
        public void ShowCostAnalysis()
        {
            Console.WriteLine("=== AZURE COMPUTER VISION COST ANALYSIS ===\n");

            Console.WriteLine("Monthly Volume     | Monthly Cost | Annual Cost");
            Console.WriteLine("─────────────────────────────────────────────────");
            Console.WriteLine($"5,000 docs        | $0           | $0 (free tier)");
            Console.WriteLine($"10,000 docs       | ${CalculateMonthlyCost(10000):F2}         | ${CalculateMonthlyCost(10000) * 12:F2}");
            Console.WriteLine($"50,000 docs       | ${CalculateMonthlyCost(50000):F2}        | ${CalculateMonthlyCost(50000) * 12:F2}");
            Console.WriteLine($"100,000 docs      | ${CalculateMonthlyCost(100000):F2}       | ${CalculateMonthlyCost(100000) * 12:F2}");
            Console.WriteLine($"500,000 docs      | ${CalculateMonthlyCost(500000):F2}      | ${CalculateMonthlyCost(500000) * 12:F2}");
            Console.WriteLine($"1,000,000 docs    | ${CalculateMonthlyCost(1000000):F2}     | ${CalculateMonthlyCost(1000000) * 12:F2}");
            Console.WriteLine();

            Console.WriteLine("Multi-page PDFs cost more:");
            Console.WriteLine("─────────────────────────────────────────────────");
            Console.WriteLine($"10,000 x 5-page PDFs  | ${CalculateMonthlyCost(10000, 5):F2}     | ${CalculateMonthlyCost(10000, 5) * 12:F2}/year");
            Console.WriteLine($"10,000 x 10-page PDFs | ${CalculateMonthlyCost(10000, 10):F2}    | ${CalculateMonthlyCost(10000, 10) * 12:F2}/year");
            Console.WriteLine();

            Console.WriteLine("Plus additional Azure costs:");
            Console.WriteLine("- Blob storage for documents");
            Console.WriteLine("- Egress bandwidth");
            Console.WriteLine("- Azure infrastructure");
        }

        /// <summary>
        /// Compare with IronOCR
        /// </summary>
        public void CompareWithIronOcr()
        {
            Console.WriteLine("=== AZURE vs IRONOCR COST COMPARISON ===\n");

            Console.WriteLine("IRONOCR PRICING (one-time):");
            Console.WriteLine("  Lite:         $749  (1 developer)");
            Console.WriteLine("  Professional: $1,499 (10 developers)");
            Console.WriteLine("  Unlimited:    $2,999 (unlimited)");
            Console.WriteLine();

            Console.WriteLine("BREAK-EVEN ANALYSIS:");
            Console.WriteLine("─────────────────────────────────────────────────");

            // At 10,000 docs/month
            decimal monthly10k = CalculateMonthlyCost(10000);
            int monthsToBreakEvenLite10k = (int)Math.Ceiling(749 / monthly10k);
            Console.WriteLine($"10,000 docs/month:");
            Console.WriteLine($"  Azure: ${monthly10k * 12:F2}/year");
            Console.WriteLine($"  IronOCR Lite pays for itself in {monthsToBreakEvenLite10k} months");
            Console.WriteLine();

            // At 50,000 docs/month
            decimal monthly50k = CalculateMonthlyCost(50000);
            int monthsToBreakEvenLite50k = (int)Math.Ceiling(749 / monthly50k);
            Console.WriteLine($"50,000 docs/month:");
            Console.WriteLine($"  Azure: ${monthly50k * 12:F2}/year");
            Console.WriteLine($"  IronOCR Lite pays for itself in {monthsToBreakEvenLite50k} month(s)");
            Console.WriteLine();

            // At 100,000 docs/month
            decimal monthly100k = CalculateMonthlyCost(100000);
            Console.WriteLine($"100,000 docs/month:");
            Console.WriteLine($"  Azure: ${monthly100k * 12:F2}/year");
            Console.WriteLine($"  IronOCR pays for itself immediately");
            Console.WriteLine();

            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - UNLIMITED DOCUMENTS, ONE-TIME PRICE
// ============================================================================

namespace IronOcrCostExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR: Pay once, process unlimited documents.
    /// No per-transaction fees. No cloud costs.
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrUnlimited
    {
        /// <summary>
        /// Process 1 document or 1 million - same cost
        /// </summary>
        public void ProcessUnlimited(string[] documentPaths)
        {
            var ocr = new IronTesseract();

            // No transaction limits
            // No per-page fees
            // No cloud costs
            foreach (var path in documentPaths)
            {
                var result = ocr.Read(path);
                Console.WriteLine($"Processed: {path}");
            }
        }

        /// <summary>
        /// Multi-page PDFs - still no extra cost
        /// </summary>
        public void ProcessMultiPagePdfs(string[] pdfPaths)
        {
            var ocr = new IronTesseract();

            foreach (var path in pdfPaths)
            {
                // 1 page or 100 pages - same price
                var result = ocr.Read(path);
                Console.WriteLine($"{path}: {result.Pages.Length} pages processed");
            }
        }

        /// <summary>
        /// No internet required - no egress costs
        /// </summary>
        public string ProcessOffline(string imagePath)
        {
            // All local - no bandwidth costs
            return new IronTesseract().Read(imagePath).Text;
        }
    }

    public class CostBenefits
    {
        public void ShowBenefits()
        {
            Console.WriteLine("=== IRONOCR COST BENEFITS ===\n");

            Console.WriteLine("✓ One-time license fee");
            Console.WriteLine("✓ Unlimited documents");
            Console.WriteLine("✓ Unlimited pages per document");
            Console.WriteLine("✓ No per-transaction fees");
            Console.WriteLine("✓ No cloud infrastructure costs");
            Console.WriteLine("✓ No bandwidth/egress fees");
            Console.WriteLine("✓ No storage costs");
            Console.WriteLine("✓ Predictable budgeting");
            Console.WriteLine();
            Console.WriteLine("Pricing: https://ironsoftware.com/csharp/ocr/#pricing");
        }
    }
}


// ============================================================================
// STOP PAYING PER DOCUMENT
//
// Azure charges add up fast. IronOCR is unlimited for one price.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Pricing: https://ironsoftware.com/csharp/ocr/#pricing
// ============================================================================
