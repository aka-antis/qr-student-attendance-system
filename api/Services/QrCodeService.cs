using QRCoder;

namespace Api.Services;

public static class QrCodeService
{
    public static byte[] GeneratePng(string token, int pixelsPerModule = 20)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }

    public static string GenerateBase64Png(string token, int pixelsPerModule = 10)
        => Convert.ToBase64String(GeneratePng(token, pixelsPerModule));
}
