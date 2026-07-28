using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;
using SkiaSharp;
using ZXing.SkiaSharp.Rendering;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Servicio para generar códigos de barras en formato PNG convertidos a base64.
/// Utiliza ZXing.Net con SkiaSharp para generar códigos de barras.
/// SkiaSharp es cross-platform (Windows, Linux, macOS), a diferencia de System.Drawing.Common.
/// </summary>
public interface IBarcodeService
{
    /// <summary>
    /// Genera una imagen de código de barras en formato Code128 y la convierte a base64.
    /// </summary>
    /// <param name="barcodeData">El dato a codificar en el código de barras</param>
    /// <returns>String con la imagen PNG codificada en base64</returns>
    Task<string> GenerarCodigoBarrasBase64Async(string barcodeData);
}

public class BarcodeService : IBarcodeService
{
    public Task<string> GenerarCodigoBarrasBase64Async(string barcodeData)
    {
        return Task.Run(() =>
        {
            var writer = new BarcodeWriter<SKBitmap>
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 300,
                    Height = 100,
                    Margin = 10
                },
                Renderer = new SKBitmapRenderer()
            };

            // Generar el bitmap del código de barras usando SkiaSharp
            using (var skBitmap = writer.Write(barcodeData))
            using (var image = SKImage.FromBitmap(skBitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                return Convert.ToBase64String(data.AsSpan());
            }
        });
    }
}
