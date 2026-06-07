using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;

namespace GroceryStoreManagement.Helpers
{
    public static class BarcodeGenerator
    {
        public static BitmapImage GenerateBarcode(string content, int width = 300, int height = 100)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            try
            {
                var writer = new ZXing.Windows.Compatibility.BarcodeWriter
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions
                    {
                        Height = height,
                        Width = width,
                        PureBarcode = false,
                        Margin = 10
                    }
                };

                using var bitmap = writer.Write(content);
                using var memory = new MemoryStream();
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Make it cross-thread accessible

                return bitmapImage;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Barcode Generation");
                return null;
            }
        }
    }
}
