# Windows.Media.Ocr for .NET: Complete Developer Guide (2026)

Windows.Media.Ocr is Microsoft's built-in OCR API available on Windows 10/11. It's free, requires no additional installation, and offers decent accuracy for basic OCR tasks—but with significant platform and capability limitations. For cross-platform OCR, see [IronOCR](https://ironsoftware.com/csharp/ocr/).

## What Is Windows.Media.Ocr?

### Platform Overview

- **API:** `Windows.Media.Ocr` namespace
- **Availability:** Windows 10/11 only
- **Price:** Free (included with Windows)
- **NuGet:** `Microsoft.Windows.SDK.NET.Ref` for WinRT interop

### Key Limitations

- **Windows-only** - No Linux, macOS, or Docker
- **UWP/WinRT API** - Requires WinRT interop in .NET
- **Limited languages** - Only installed Windows language packs
- **No PDF support** - Image-only processing
- **No preprocessing** - Raw image input

## Basic Usage

```csharp
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

public class WindowsOcrExample
{
    public async Task<string> ExtractTextAsync(string imagePath)
    {
        // Load image via Windows APIs
        var file = await StorageFile.GetFileFromPathAsync(imagePath);
        using var stream = await file.OpenAsync(FileAccessMode.Read);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        var bitmap = await decoder.GetSoftwareBitmapAsync();

        // Create OCR engine for current language
        var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();

        if (ocrEngine == null)
            throw new Exception("No OCR language available");

        var result = await ocrEngine.RecognizeAsync(bitmap);

        return result.Text;
    }
}
```

### Using from .NET 6+

For modern .NET, you need WinRT interop:

```xml
<PropertyGroup>
  <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
</PropertyGroup>
```

```csharp
// Target Windows TFM to access WinRT APIs
```

## Windows.Media.Ocr vs IronOCR

| Aspect | Windows.Media.Ocr | IronOCR |
|--------|-------------------|---------|
| Price | Free | $749-5,999 |
| Platform | Windows only | Cross-platform |
| Linux/Docker | No | Yes |
| Languages | Windows language packs | 125+ bundled |
| PDF support | No | Native |
| Preprocessing | None | Automatic |
| API style | WinRT async | Standard .NET |

### When Windows.Media.Ocr Works

- Windows desktop app only
- Simple OCR needs
- Budget is $0
- Platform lock-in acceptable

### When IronOCR is Better

- Cross-platform deployment
- Server/cloud deployment
- PDF processing needed
- Linux/Docker required
- Better preprocessing needed

For Windows-only desktop apps with basic OCR needs, Windows.Media.Ocr is free and built-in. For anything else, [IronOCR](https://www.nuget.org/packages/IronOcr)'s cross-platform support and preprocessing make it the clear choice.

**Related Resources:**
- [IronOCR Documentation](https://ironsoftware.com/csharp/ocr/docs/)
- [Azure Computer Vision](../azure-computer-vision/) - Microsoft's cloud OCR service
- [Tesseract](../tesseract/) - Cross-platform free alternative

---

*Last verified: January 2026*
