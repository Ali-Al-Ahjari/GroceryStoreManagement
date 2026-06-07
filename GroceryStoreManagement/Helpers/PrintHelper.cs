using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GroceryStoreManagement.Helpers
{
    public static class PrintHelper
    {
        public static void PrintReceipt(int saleId)
        {
            try
            {
                var sale = SaleDAL.GetSaleById(saleId);
                if (sale == null)
                {
                    _ = MessageBox.Show("الفاتورة غير موجودة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var saleItems = SaleItemDAL.GetSaleItemsBySaleId(saleId);
                string receiptText = BuildReceiptText(sale, saleItems);

                // أولاً: محاولة طباعة حرارية مباشرة (ESC/POS) على الطابعة المحددة في الإعدادات.
                // ثانياً: fallback إلى طباعة WPF العادية عند الفشل.
                if (TryPrintThermal(receiptText))
                {
                    return;
                }

                var document = CreateReceiptDocument($"فاتورة رقم {sale.SaleID}", receiptText);
                PrintDocument(document, $"فاتورة رقم {sale.SaleID}");
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void PrintShiftZReport(Shift shift)
        {
            if (shift == null)
            {
                return;
            }

            var sb = new StringBuilder();
            _ = sb.AppendLine("تقرير إغلاق الوردية (Z-Report)");
            _ = sb.AppendLine("================================");
            _ = sb.AppendLine($"رقم الوردية: {shift.ShiftID}");
            _ = sb.AppendLine($"الحالة: {shift.StatusArabic}");
            _ = sb.AppendLine($"فتح الوردية: {shift.DisplayOpenedAt}");
            _ = sb.AppendLine($"إغلاق الوردية: {shift.DisplayClosedAt}");
            _ = sb.AppendLine($"العهدة الافتتاحية: {shift.OpeningCash.ToDisplayCurrency()}");
            _ = sb.AppendLine("--------------------------------");
            _ = sb.AppendLine($"مبيعات كاش: {shift.CashSalesTotal.ToDisplayCurrency()}");
            _ = sb.AppendLine($"مبيعات شبكة: {shift.CardSalesTotal.ToDisplayCurrency()}");
            _ = sb.AppendLine($"مبيعات تحويل: {shift.TransferSalesTotal.ToDisplayCurrency()}");
            _ = sb.AppendLine($"مبيعات آجلة: {shift.CreditSalesTotal.ToDisplayCurrency()}");
            _ = sb.AppendLine($"مرتجعات كاش: -{shift.CashRefundsTotal.ToDisplayCurrency()}");
            _ = sb.AppendLine("--------------------------------");
            _ = sb.AppendLine($"المتوقع في الدرج: {shift.ExpectedCash.ToDisplayCurrency()}");
            _ = sb.AppendLine($"الموجود فعلياً: {shift.ClosingCash.GetValueOrDefault().ToDisplayCurrency()}");
            _ = sb.AppendLine($"فرق النقدية: {shift.CashDifference.ToDisplayCurrency()}");
            _ = sb.AppendLine("================================");
            if (!string.IsNullOrWhiteSpace(shift.Notes))
            {
                _ = sb.AppendLine($"ملاحظات: {shift.Notes}");
            }
            _ = sb.AppendLine("نهاية التقرير");

            if (TryPrintThermal(sb.ToString()))
            {
                return;
            }

            var doc = CreateReceiptDocument($"Z-Report وردية #{shift.ShiftID}", sb.ToString());
            PrintDocument(doc, $"Z-Report وردية #{shift.ShiftID}");
        }

        public static bool TryOpenCashDrawer()
        {
            try
            {
                // افتراضي: مفعّل إذا لم يوجد إعداد.
                string openDrawerSetting = AppSettings.GetString("OpenCashDrawerOnCashPayment", "true");
                if (bool.TryParse(openDrawerSetting, out bool enabled) && !enabled)
                {
                    return false;
                }

                string printerName = GetConfiguredPrinterName();
                if (string.IsNullOrWhiteSpace(printerName))
                {
                    return false;
                }

                string hex = AppSettings.GetString("CashDrawerKickCommandHex", "1B-70-00-19-FA");
                byte[] command = ParseHexCommand(hex);
                return SendBytesToPrinter(printerName, command);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"فشل إرسال أمر فتح درج النقود: {ex.Message}");
                return false;
            }
        }

        private static string BuildReceiptText(Sale sale, List<SaleItem> saleItems)
        {
            var receipt = new StringBuilder();
            _ = receipt.AppendLine("فاتورة بيع");
            _ = receipt.AppendLine("================");
            _ = receipt.AppendLine($"رقم الفاتورة: {sale.SaleID}");
            _ = receipt.AppendLine($"التاريخ: {sale.DisplayDateTime}");
            _ = receipt.AppendLine($"العميل: {(string.IsNullOrEmpty(sale.CustomerName) ? "عميل نقدي" : sale.CustomerName)}");
            _ = receipt.AppendLine("================");
            _ = receipt.AppendLine("المنتجات:");
            _ = receipt.AppendLine("---------");

            foreach (var item in saleItems)
            {
                _ = receipt.AppendLine($"{item.ProductName} × {item.Quantity} = {item.DisplayTotalPrice}");
            }

            decimal taxAmount = sale.TotalAmount * (sale.Tax / 100m);
            _ = receipt.AppendLine("----------------");
            _ = receipt.AppendLine($"الإجمالي الفرعي: {sale.TotalAmount.ToDisplayCurrency()}");
            _ = receipt.AppendLine($"الخصم: -{sale.Discount.ToDisplayCurrency()}");
            _ = receipt.AppendLine($"الضريبة ({sale.Tax:0.##}%): +{taxAmount.ToDisplayCurrency()}");
            _ = receipt.AppendLine($"الصافي: {sale.NetTotal.ToDisplayCurrency()}");
            _ = receipt.AppendLine($"المدفوع: {sale.PaidAmount.ToDisplayCurrency()}");
            _ = receipt.AppendLine($"المتبقي: {sale.RemainingAmount.ToDisplayCurrency()}");

            string footer = AppSettings.GetString("InvoiceFooter", string.Empty);
            if (!string.IsNullOrWhiteSpace(footer))
            {
                _ = receipt.AppendLine("----------------");
                _ = receipt.AppendLine(footer.Replace("\\n", Environment.NewLine));
            }

            _ = receipt.AppendLine("================");
            _ = receipt.AppendLine("شكراً لزيارتكم");
            return receipt.ToString();
        }

        private static bool TryPrintThermal(string text)
        {
            string printerName = GetConfiguredPrinterName();
            if (string.IsNullOrWhiteSpace(printerName))
            {
                return false;
            }

            var payload = new List<byte>();
            payload.AddRange(new byte[] { 0x1B, 0x40 }); // Initialize printer
            payload.AddRange(Encoding.UTF8.GetBytes(text + Environment.NewLine + Environment.NewLine));
            payload.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 }); // Full cut

            bool sent = SendBytesToPrinter(printerName, payload.ToArray());
            if (!sent)
            {
                Logger.LogWarning($"فشل إرسال بيانات الطباعة الحرارية للطابعة: {printerName}");
            }

            return sent;
        }

        private static string GetConfiguredPrinterName()
        {
            string configured = AppSettings.GetString("ReceiptPrinter", string.Empty);
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            try
            {
                var server = new LocalPrintServer();
                return server.DefaultPrintQueue?.Name;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static FlowDocument CreateReceiptDocument(string title, string content)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(40),
                ColumnWidth = 500
            };

            var titleParagraph = new Paragraph(new Run(title))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(titleParagraph);

            var contentParagraph = new Paragraph(new Run(content))
            {
                FontSize = 14,
                TextAlignment = TextAlignment.Right,
                Foreground = Brushes.Black,
                LineHeight = 1.5
            };
            doc.Blocks.Add(contentParagraph);

            var footerParagraph = new Paragraph(new Run("\n\nشكراً لاختياركم متجرنا"))
            {
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Gray
            };
            doc.Blocks.Add(footerParagraph);

            return doc;
        }

        public static void PrintDocument(FlowDocument document, string description)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    document.PageHeight = printDialog.PrintableAreaHeight;
                    document.PageWidth = printDialog.PrintableAreaWidth;
                    IDocumentPaginatorSource paginatorSource = document;
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator, description);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في الطباعة: {ex.Message}", ex);
            }
        }

        public static void ExportToPdf(FlowDocument document, string filePath)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentException.ThrowIfNullOrEmpty(filePath);

            try
            {
                _ = MessageBox.Show("دعم تصدير PDF سيكون متاحاً في الإصدارات القادمة", "معلومة",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static byte[] ParseHexCommand(string hex)
        {
            string normalized = (hex ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
            if (normalized.Length % 2 != 0)
            {
                throw new InvalidDataException("صيغة أمر فتح الدرج غير صحيحة.");
            }

            var bytes = new byte[normalized.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(normalized.Substring(i * 2, 2), 16);
            }

            return bytes;
        }

        private static bool SendBytesToPrinter(string printerName, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(printerName) || bytes == null || bytes.Length == 0)
            {
                return false;
            }

            if (!OpenPrinter(printerName.Normalize(), out nint hPrinter, IntPtr.Zero))
            {
                return false;
            }

            try
            {
                var docInfo = new DOCINFOA
                {
                    pDocName = "POS Raw Print",
                    pDataType = "RAW"
                };

                if (StartDocPrinter(hPrinter, 1, docInfo) == 0)
                {
                    return false;
                }

                try
                {
                    if (!StartPagePrinter(hPrinter))
                    {
                        return false;
                    }

                    nint pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                    try
                    {
                        Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);
                        return WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out int written) && written == bytes.Length;
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pUnmanagedBytes);
                        _ = EndPagePrinter(hPrinter);
                    }
                }
                finally
                {
                    _ = EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                _ = ClosePrinter(hPrinter);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool OpenPrinter(string pPrinterName, out nint phPrinter, nint pDefault);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool ClosePrinter(nint hPrinter);

        [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern int StartDocPrinter(nint hPrinter, int level, [In] DOCINFOA pDocInfo);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool EndDocPrinter(nint hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool StartPagePrinter(nint hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool EndPagePrinter(nint hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool WritePrinter(nint hPrinter, nint pBytes, int dwCount, out int dwWritten);
    }
}
