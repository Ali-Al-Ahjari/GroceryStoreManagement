using GroceryStoreManagement.Helpers;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GroceryStoreManagement.Windows
{
    public partial class BarcodeScannerWindow : Window
    {
        private readonly BarcodeScannerHelper _scanner;
        private int _currentCameraIndex = 0;
        public string ScannedCode { get; private set; }

        public BarcodeScannerWindow()
        {
            InitializeComponent();
            _scanner = new BarcodeScannerHelper();
            _scanner.NewFrameReceived += Scanner_NewFrameReceived;
            _scanner.BarcodeFound += Scanner_BarcodeFound;

            Loaded += BarcodeScannerWindow_Loaded;
        }

        private void BarcodeScannerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartScanning();
        }

        private void StartScanning()
        {
            try
            {
                var cameras = _scanner.GetAvailableCameras();
                if (cameras.Count > 0)
                {
                    if (_currentCameraIndex >= cameras.Count) _currentCameraIndex = 0;

                    _scanner.StartCamera(_currentCameraIndex);
                    TxtStatus.Text = "جاري المسح...";
                }
                else
                {
                    TxtStatus.Text = "لا توجد كاميرات متصلة";
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"خطأ: {ex.Message}";
            }
        }

        private void Scanner_NewFrameReceived(Bitmap bitmap)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    CameraPreview.Source = BarcodeScannerHelper.ConvertToBitmapImage(bitmap);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "فشل تحديث معاينة الكاميرا");
                }
            });
        }

        private void Scanner_BarcodeFound(string code)
        {
            Dispatcher.Invoke(() =>
            {
                System.Media.SystemSounds.Beep.Play();

                ScannedCode = code;
                TxtBarcode.Text = code;
                TxtStatus.Text = $"تم قراءة الرمز: {code}";
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _scanner.StopCamera();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            ScannedCode = TxtBarcode.Text;
            DialogResult = true;
            Close();
        }

        private void BtnManualEntry_Click(object sender, RoutedEventArgs e)
        {
            _ = TxtBarcode.Focus();
        }

        private void BtnSwitchCamera_Click(object sender, RoutedEventArgs e)
        {
            _scanner.StopCamera();
            _currentCameraIndex++;
            StartScanning();
        }
    }
}
