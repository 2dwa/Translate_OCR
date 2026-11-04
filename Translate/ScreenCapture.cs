using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace Translate;

internal static class ScreenCapture
{
    internal static Rect DesktopBounds => new(NativeMethods.GetSystemMetrics(76), NativeMethods.GetSystemMetrics(77), NativeMethods.GetSystemMetrics(78), NativeMethods.GetSystemMetrics(79));
    internal static BitmapSource Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var image = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if ((long)image.PixelWidth * image.PixelHeight > 80_000_000) throw new InvalidOperationException("图片分辨率过大，请缩小后重试。");
        image.Freeze(); return image;
    }
    internal static byte[] EncodePng(BitmapSource image)
    {
        if ((long)image.PixelWidth * image.PixelHeight > 80_000_000) throw new InvalidOperationException("图片分辨率过大，请缩小后重试。");
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new MemoryStream(); encoder.Save(stream); return stream.ToArray();
    }
    internal static byte[] CropPng(BitmapSource desktop, Rect selection, Rect bounds)
    {
        var x = Math.Clamp((int)Math.Round(selection.X - bounds.X), 0, desktop.PixelWidth - 1);
        var y = Math.Clamp((int)Math.Round(selection.Y - bounds.Y), 0, desktop.PixelHeight - 1);
        var width = Math.Clamp((int)Math.Round(selection.Width), 1, desktop.PixelWidth - x);
        var height = Math.Clamp((int)Math.Round(selection.Height), 1, desktop.PixelHeight - y);
        return EncodePng(new CroppedBitmap(desktop, new Int32Rect(x, y, width, height)));
    }
    internal static byte[] CapturePng(Rect selection)
    {
        var width = Math.Max(1, (int)Math.Round(selection.Width));
        var height = Math.Max(1, (int)Math.Round(selection.Height));
        using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
            graphics.CopyFromScreen((int)Math.Round(selection.X), (int)Math.Round(selection.Y), 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
