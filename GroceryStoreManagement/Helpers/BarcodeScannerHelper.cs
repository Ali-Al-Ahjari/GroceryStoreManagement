using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using ZXing;
using System.Runtime.Versioning;

namespace GroceryStoreManagement.Helpers
{
    [SupportedOSPlatform("windows")]
    public class BarcodeScannerHelper
    {
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;
        private readonly ZXing.Windows.Compatibility.BarcodeReader _reader;
        public event Action<Bitmap> NewFrameReceived;
        public event Action<string> BarcodeFound;

        public BarcodeScannerHelper()
        {
            _reader = new ZXing.Windows.Compatibility.BarcodeReader();
            _reader.Options.PossibleFormats =
            [
                BarcodeFormat.EAN_13,
                BarcodeFormat.EAN_8,
                BarcodeFormat.CODE_128,
                BarcodeFormat.CODE_39,
                BarcodeFormat.QR_CODE
            ];
        }

        public List<string> GetAvailableCameras()
        {
            var cameras = new List<string>();
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in _videoDevices)
            {
                cameras.Add(device.Name);
            }
            return cameras;
        }

        public void StartCamera(int cameraIndex)
        {
            if (_videoDevices == null || _videoDevices.Count == 0) return;
            if (cameraIndex < 0 || cameraIndex >= _videoDevices.Count) return;

            StopCamera();

            _videoSource = new VideoCaptureDevice(_videoDevices[cameraIndex].MonikerString);
            _videoSource.NewFrame += VideoSource_NewFrame;
            _videoSource.Start();
        }

        public void StopCamera()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.NewFrame -= VideoSource_NewFrame;
                _videoSource.WaitForStop();
                _videoSource = null;
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                // Copy the frame securely to avoid conflicts
                using var bitmap = (Bitmap)eventArgs.Frame.Clone();
                // Trigger new frame event (for UI display)
                NewFrameReceived?.Invoke((Bitmap)bitmap.Clone());

                // Try to decode
                var result = _reader.Decode(bitmap);
                if (result != null)
                {
                    BarcodeFound?.Invoke(result.Text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing frame: {ex.Message}");
            }
        }

        // Helper to convert Bitmap to BitmapImage for WPF
        public static BitmapImage ConvertToBitmapImage(Bitmap bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            memory.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
    }
}
