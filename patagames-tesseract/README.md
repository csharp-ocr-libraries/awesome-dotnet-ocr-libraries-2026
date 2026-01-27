# Patagames Tesseract.Net.SDK for .NET: Complete Developer Guide (2026)

Tesseract.Net.SDK by Patagames is a commercial .NET wrapper around the Tesseract OCR engine. Unlike the free charlesw/Tesseract wrapper, Patagames offers commercial licensing with support and additional features, but the core Tesseract limitations remain. For a solution that addresses Tesseract's limitations, see [IronOCR](https://ironsoftware.com/csharp/ocr/).

## Patagames Overview

### What Is It?

- **NuGet:** `Tesseract.Net.SDK`
- **Vendor:** Patagames Software
- **License:** Commercial
- **Type:** Commercial Tesseract wrapper

### Claimed Advantages

Patagames markets several benefits over free wrappers:

- Commercial support and updates
- Simplified API
- Pre-built native binaries
- Managed tessdata handling

### Core Limitation

**It's still Tesseract underneath.** This means:

- Preprocessing is still your responsibility
- Accuracy on poor images is still limited
- PDFs still need external handling
- Native dependencies still exist

## Pricing

Patagames uses per-developer licensing:

| License | Price |
|---------|-------|
| Single Developer | Contact for quote |
| Team | Contact for quote |
| Enterprise | Contact for quote |

Pricing is not publicly listed.

## Basic Implementation

```csharp
using Patagames.Ocr;

public class PatagamesExample
{
    public string ExtractText(string imagePath)
    {
        using var api = OcrApi.Create();
        api.Init(Languages.English);

        using var img = OcrBitmap.FromFile(imagePath);
        var result = api.GetTextFromImage(img);

        return result;
    }
}
```

## Patagames vs IronOCR

| Aspect | Patagames Tesseract | IronOCR |
|--------|---------------------|---------|
| Price | Commercial (contact) | $749-5,999 (public) |
| Based on | Tesseract | IronTesseract |
| Preprocessing | Manual | Automatic |
| PDF support | Limited | Native |
| Password PDFs | No | Yes |
| Searchable PDF output | Manual | Built-in |
| Commercial support | Yes | Yes |

### The Fundamental Question

If you're paying for a commercial Tesseract wrapper, why not pay for a solution that solves Tesseract's core problems?

**Patagames solves:** API simplification, native library bundling
**Patagames doesn't solve:** Preprocessing, PDF handling, accuracy on poor images

**IronOCR solves:** All of the above

## Migration to IronOCR

```csharp
// Patagames
using var api = OcrApi.Create();
api.Init(Languages.English);
using var img = OcrBitmap.FromFile(imagePath);
var text = api.GetTextFromImage(img);

// IronOCR
var text = new IronTesseract().Read(imagePath).Text;
```

For developers considering commercial Tesseract wrappers, [IronOCR](https://www.nuget.org/packages/IronOcr) typically offers more value for similar or lower cost.

**Related Tesseract Wrappers:**
- [IronOCR Documentation](https://ironsoftware.com/csharp/ocr/docs/)
- [TesseractOCR (charlesw)](../charlesw-tesseract/) - Free Tesseract wrapper
- [Tesseract.NET.SDK](../tesseract-net-sdk/) - Alternative commercial wrapper
- [Tesseract Overview](../tesseract/) - Complete ecosystem guide

---

*Last verified: January 2026*
