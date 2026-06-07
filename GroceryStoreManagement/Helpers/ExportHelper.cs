using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;

// إضافة هذه الـ using للوصول إلى الفئات
using GroceryStoreManagement.Models;

namespace GroceryStoreManagement.Helpers
{
    public static class ExportHelper
    {
        static ExportHelper()
        {
            EnsureEpplusLicenseConfigured();
        }

        #region تصدير إلى CSV

        /// <summary>
        /// تصدير بيانات إلى ملف CSV
        /// </summary>
        public static bool ExportToCSV<T>(IEnumerable<T> data, string filePath, string delimiter = ",")
        {
            return ExportToCSV(data, filePath, displayNameMap: null, delimiter: delimiter);
        }

        /// <summary>
        /// تصدير بيانات إلى ملف CSV مع إمكانية تحديد أسماء أعمدة مخصصة.
        /// </summary>
        public static bool ExportToCSV<T>(IEnumerable<T> data, string filePath, IReadOnlyDictionary<string, string> displayNameMap, string delimiter = ",")
        {
            try
            {
                var sb = new StringBuilder();
                var columns = ResolveExportColumns<T>(displayNameMap);

                // إضافة رأس CSV
                var header = string.Join(delimiter, columns.Select(c => EscapeCsv(c.DisplayName, delimiter)));
                _ = sb.AppendLine(header);

                // إضافة البيانات
                foreach (var item in data)
                {
                    var values = new List<string>();
                    foreach (var column in columns)
                    {
                        var value = column.Property.GetValue(item, null);
                        var stringValue = value?.ToString() ?? string.Empty;
                        values.Add(EscapeCsv(stringValue, delimiter));
                    }
                    _ = sb.AppendLine(string.Join(delimiter, values));
                }

                // كتابة الملف
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في التصدير إلى CSV: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// تصدير تقرير المبيعات إلى CSV
        /// </summary>
        public static bool ExportSalesReportToCSV(List<DailySalesReport> salesReport, string filePath)
        {
            try
            {
                var sb = new StringBuilder();

                // رأس التقرير
                _ = sb.AppendLine("تقرير المبيعات اليومية");
                _ = sb.AppendLine($"تاريخ التصدير: {DateTime.Now:yyyy/MM/dd HH:mm}");
                _ = sb.AppendLine();

                // رأس الجدول
                _ = sb.AppendLine("رقم الفاتورة,اسم العميل,التاريخ,الوقت,عدد المنتجات,المبلغ الإجمالي");

                // البيانات
                foreach (var sale in salesReport)
                {
                    _ = sb.AppendLine($"{sale.SaleID},{sale.CustomerName},{sale.SaleTime:yyyy/MM/dd},{sale.SaleTime:HH:mm},{sale.ItemCount},{sale.TotalAmount}");
                }

                // الإجمالي
                _ = sb.AppendLine();
                _ = sb.AppendLine($"الإجمالي,{salesReport.Count} فواتير,{salesReport.Sum(s => s.ItemCount)} منتجات,{salesReport.Sum(s => s.TotalAmount).ToDisplayCurrency()}");

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تصدير تقرير المبيعات: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// تصدير تقرير المنتجات إلى CSV
        /// </summary>
        public static bool ExportProductsReportToCSV(List<ProductSalesReport> productsReport, string filePath)
        {
            try
            {
                var sb = new StringBuilder();

                _ = sb.AppendLine("تقرير المنتجات");
                _ = sb.AppendLine($"تاريخ التصدير: {DateTime.Now:yyyy/MM/dd HH:mm}");
                _ = sb.AppendLine();
                _ = sb.AppendLine("اسم المنتج,الفئة,الكمية المباعة,الإيرادات,متوسط السعر,المخزون الحالي,قيمة المخزون");

                foreach (var product in productsReport)
                {
                    _ = sb.AppendLine($"{product.ProductName},{product.Category},{product.QuantitySold},{product.TotalRevenue},{product.AveragePrice},{product.CurrentStock},{product.StockValue}");
                }

                _ = sb.AppendLine();
                _ = sb.AppendLine($"الإجمالي,{productsReport.Count} منتج,{productsReport.Sum(p => p.QuantitySold)} وحدة,{productsReport.Sum(p => p.TotalRevenue).ToDisplayCurrency()},,,{productsReport.Sum(p => p.StockValue).ToDisplayCurrency()}");

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تصدير تقرير المنتجات: {ex.Message}", ex);
            }
        }

        #endregion

        #region تصدير إلى Excel باستخدام EPPlus

        /// <summary>
        /// تصدير بيانات إلى Excel باستخدام EPPlus
        /// </summary>
        public static bool ExportToExcel<T>(IEnumerable<T> data, string filePath, string sheetName = "البيانات")
        {
            return ExportToExcel(data, filePath, displayNameMap: null, sheetName: sheetName);
        }

        /// <summary>
        /// تصدير بيانات إلى Excel باستخدام EPPlus مع إمكانية تحديد أسماء أعمدة مخصصة.
        /// </summary>
        public static bool ExportToExcel<T>(IEnumerable<T> data, string filePath, IReadOnlyDictionary<string, string> displayNameMap, string sheetName = "البيانات")
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                var columns = ResolveExportColumns<T>(displayNameMap);

                // كتابة العناوين
                for (int i = 0; i < columns.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = columns[i].DisplayName;
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // كتابة البيانات
                int row = 2;
                foreach (var item in data)
                {
                    for (int col = 0; col < columns.Count; col++)
                    {
                        var value = columns[col].Property.GetValue(item, null);
                        worksheet.Cells[row, col + 1].Value = value;
                    }
                    row++;
                }

                // تنسيق الخلايا
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // تنسيق أعمدة العملة
                for (int i = 0; i < columns.Count; i++)
                {
                    var propName = columns[i].Property.Name.ToLower(System.Globalization.CultureInfo.CurrentCulture);
                    if (propName.Contains("price") || propName.Contains("amount") ||
                        propName.Contains("total") || propName.Contains("value") ||
                        propName.Contains("revenue"))
                    {
                        worksheet.Column(i + 1).Style.Numberformat.Format = "#,##0.##";
                    }
                }

                // حفظ الملف
                package.SaveAs(new FileInfo(filePath));
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في التصدير إلى Excel: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// تصدير تقرير متعدد الأوراق إلى Excel
        /// </summary>
        public static bool ExportMultiSheetReport(string filePath, params (string SheetName, object Data)[] sheets)
        {
            try
            {
                using var package = new ExcelPackage();
                foreach (var (SheetName, Data) in sheets)
                {
                    var worksheet = package.Workbook.Worksheets.Add(SheetName);

                    // هنا يمكن إضافة منطق لمعالجة أنواع البيانات المختلفة
                    // سنضيف بيانات بسيطة كمثال
                    worksheet.Cells[1, 1].Value = $"بيانات {SheetName}";
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells[1, 1].Style.Font.Size = 14;
                }

                package.SaveAs(new FileInfo(filePath));
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تصدير التقرير متعدد الأوراق: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// تصدير تقرير المبيعات الشامل إلى Excel
        /// </summary>
        public static bool ExportComprehensiveSalesReport(string filePath,
            List<DailySalesReport> dailySales,
            List<ProductSalesReport> topProducts,
            List<CustomerPurchaseReport> topCustomers)
        {
            try
            {
                using var package = new ExcelPackage();
                // صفحة المبيعات اليومية
                ExportSalesToWorksheet(package, "المبيعات اليومية", dailySales);

                // صفحة المنتجات الأكثر مبيعاً
                ExportProductsToWorksheet(package, "المنتجات الأكثر مبيعاً", topProducts);

                // صفحة العملاء الأكثر شراءً
                ExportCustomersToWorksheet(package, "العملاء الأكثر شراءً", topCustomers);

                // صفحة الملخص
                CreateSummaryWorksheet(package, "ملخص التقرير", dailySales, topProducts, topCustomers);

                package.SaveAs(new FileInfo(filePath));
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تصدير تقرير المبيعات الشامل: {ex.Message}", ex);
            }
        }

        #endregion

        #region تصدير إلى PDF

        /// <summary>
        /// تصدير مجموعة بيانات إلى PDF.
        /// </summary>
        public static bool ExportToPDF<T>(IEnumerable<T> data, string filePath, string reportTitle, IReadOnlyDictionary<string, string> displayNameMap = null)
        {
            try
            {
                return PdfExportHelper.ExportCollectionToPdf(data, filePath, reportTitle, displayNameMap);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في التصدير إلى PDF: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// تصدير نص حر إلى PDF.
        /// </summary>
        public static bool ExportToPDF(string filePath, string content)
        {
            try
            {
                return PdfExportHelper.ExportTextToPdf("تقرير", content, filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في التصدير: {ex.Message}", ex);
            }
        }

        #endregion

        #region تصدير إلى نص

        /// <summary>
        /// تصدير نص إلى ملف
        /// </summary>
        public static bool ExportToText(string content, string filePath)
        {
            try
            {
                File.WriteAllText(filePath, content, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في التصدير إلى نص: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// إنشاء تقرير نصي مفصل
        /// </summary>
        public static string GenerateTextReport(DashboardSummary summary)
        {
            var sb = new StringBuilder();

            _ = sb.AppendLine("=".PadRight(50, '='));
            _ = sb.AppendLine("تقرير إدارة محلات البقالة");
            _ = sb.AppendLine($"تاريخ التقرير: {DateTime.Now:yyyy/MM/dd HH:mm}");
            _ = sb.AppendLine("=".PadRight(50, '='));
            _ = sb.AppendLine();

            _ = sb.AppendLine("الإحصائيات العامة:");
            _ = sb.AppendLine("-".PadRight(30, '-'));
            _ = sb.AppendLine($"المبيعات اليومية: {summary.DisplayTodaySales}");
            _ = sb.AppendLine($"عدد الفواتير اليوم: {summary.TodayTransactions}");
            _ = sb.AppendLine($"المبيعات الشهرية: {summary.DisplayMonthlySales}");
            _ = sb.AppendLine($"إجمالي المنتجات: {summary.TotalProducts}");
            _ = sb.AppendLine($"المنتجات منخفضة المخزون: {summary.LowStockProducts}");
            _ = sb.AppendLine($"إجمالي العملاء: {summary.TotalCustomers}");
            _ = sb.AppendLine($"قيمة المخزون: {summary.DisplayStockValue}");
            _ = sb.AppendLine();

            _ = sb.AppendLine("التوصيات:");
            _ = sb.AppendLine("-".PadRight(30, '-'));

            if (summary.LowStockProducts > 0)
            {
                _ = sb.AppendLine($"⚠️  هناك {summary.LowStockProducts} منتجات تحتاج إلى إعادة طلب");
            }

            if (summary.TodayTransactions < 5)
            {
                _ = sb.AppendLine("💡  المبيعات اليومية منخفضة، فكر في عروض ترويجية");
            }

            if (summary.StockValue > 100000)
            {
                _ = sb.AppendLine("📊  قيمة المخزون عالية، فكر في عروض لتصريف المخزون");
            }

            _ = sb.AppendLine();
            _ = sb.AppendLine("=".PadRight(50, '='));
            _ = sb.AppendLine("نهاية التقرير");

            return sb.ToString();
        }

        #endregion

        #region دوال مساعدة خاصة

        private static void ExportSalesToWorksheet(ExcelPackage package, string sheetName, List<DailySalesReport> sales)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // العناوين
            string[] headers = ["رقم الفاتورة", "اسم العميل", "التاريخ", "الوقت", "عدد المنتجات", "المبلغ الإجمالي"];

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                FormatHeaderCell(worksheet.Cells[1, i + 1]);
            }

            // البيانات
            for (int i = 0; i < sales.Count; i++)
            {
                var sale = sales[i];
                worksheet.Cells[i + 2, 1].Value = sale.SaleID;
                worksheet.Cells[i + 2, 2].Value = sale.CustomerName;
                worksheet.Cells[i + 2, 3].Value = sale.SaleTime.ToString("yyyy/MM/dd");
                worksheet.Cells[i + 2, 4].Value = sale.SaleTime.ToString("HH:mm");
                worksheet.Cells[i + 2, 5].Value = sale.ItemCount;
                worksheet.Cells[i + 2, 6].Value = sale.TotalAmount;
            }

            // تنسيق العمود الأخير كعملة
            worksheet.Column(6).Style.Numberformat.Format = "#,##0.##";

            // إجمالي المبيعات
            int lastRow = sales.Count + 2;
            worksheet.Cells[lastRow, 5].Value = "الإجمالي:";
            worksheet.Cells[lastRow, 6].Value = sales.Sum(s => s.TotalAmount);
            worksheet.Cells[lastRow, 5].Style.Font.Bold = true;
            worksheet.Cells[lastRow, 6].Style.Font.Bold = true;
            worksheet.Cells[lastRow, 6].Style.Numberformat.Format = "#,##0.##";

            // توسيع الأعمدة تلقائياً
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private static void ExportProductsToWorksheet(ExcelPackage package, string sheetName, List<ProductSalesReport> products)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            string[] headers = ["اسم المنتج", "الفئة", "الكمية المباعة", "الإيرادات", "متوسط السعر", "المخزون الحالي", "قيمة المخزون"];

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                FormatHeaderCell(worksheet.Cells[1, i + 1]);
            }

            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];
                worksheet.Cells[i + 2, 1].Value = product.ProductName;
                worksheet.Cells[i + 2, 2].Value = product.Category;
                worksheet.Cells[i + 2, 3].Value = product.QuantitySold;
                worksheet.Cells[i + 2, 4].Value = product.TotalRevenue;
                worksheet.Cells[i + 2, 5].Value = product.AveragePrice;
                worksheet.Cells[i + 2, 6].Value = product.CurrentStock;
                worksheet.Cells[i + 2, 7].Value = product.StockValue;
            }

            // تنسيق أعمدة العملة
            worksheet.Column(4).Style.Numberformat.Format = "#,##0.##";
            worksheet.Column(5).Style.Numberformat.Format = "#,##0.##";
            worksheet.Column(7).Style.Numberformat.Format = "#,##0.##";

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private static void ExportCustomersToWorksheet(ExcelPackage package, string sheetName, List<CustomerPurchaseReport> customers)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            string[] headers = ["اسم العميل", "الهاتف", "البريد الإلكتروني", "إجمالي المشتريات", "عدد المشتريات", "متوسط الشراء", "آخر عملية شراء"];

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                FormatHeaderCell(worksheet.Cells[1, i + 1]);
            }

            for (int i = 0; i < customers.Count; i++)
            {
                var customer = customers[i];
                worksheet.Cells[i + 2, 1].Value = customer.CustomerName;
                worksheet.Cells[i + 2, 2].Value = customer.Phone;
                worksheet.Cells[i + 2, 3].Value = customer.Email;
                worksheet.Cells[i + 2, 4].Value = customer.TotalPurchases;
                worksheet.Cells[i + 2, 5].Value = customer.PurchaseCount;
                worksheet.Cells[i + 2, 6].Value = customer.AveragePurchase;
                worksheet.Cells[i + 2, 7].Value = customer.LastPurchaseDate.ToString("yyyy/MM/dd");
            }

            // تنسيق أعمدة العملة
            worksheet.Column(4).Style.Numberformat.Format = "#,##0.##";
            worksheet.Column(6).Style.Numberformat.Format = "#,##0.##";

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private static void CreateSummaryWorksheet(ExcelPackage package, string sheetName,
            List<DailySalesReport> sales,
            List<ProductSalesReport> products,
            List<CustomerPurchaseReport> customers)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // عنوان التقرير
            worksheet.Cells[1, 1].Value = "تقرير المبيعات الشامل";
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[2, 1].Value = $"تاريخ التقرير: {DateTime.Now:yyyy/MM/dd HH:mm}";

            // الإحصائيات الرئيسية
            worksheet.Cells[4, 1].Value = "الإحصائيات الرئيسية";
            worksheet.Cells[4, 1].Style.Font.Bold = true;
            worksheet.Cells[4, 1].Style.Font.Size = 14;

            string[] statLabels = ["إجمالي المبيعات", "عدد الفواتير", "عدد المنتجات المباعة", "متوسط قيمة الفاتورة", "أعلى منتج مبيعاً", "أفضل عميل"];
            string[] statValues = [
                sales.Sum(s => s.TotalAmount).ToDisplayCurrency(),
                sales.Count.ToString(),
                sales.Sum(s => s.ItemCount).ToString(),
                (sales.Count > 0 ? sales.Average(s => s.TotalAmount) : 0).ToDisplayCurrency(),
                products.FirstOrDefault()?.ProductName ?? "لا يوجد",
                customers.FirstOrDefault()?.CustomerName ?? "لا يوجد"
            ];

            for (int i = 0; i < statLabels.Length; i++)
            {
                worksheet.Cells[i + 5, 1].Value = statLabels[i];
                worksheet.Cells[i + 5, 2].Value = statValues[i];
                worksheet.Cells[i + 5, 1].Style.Font.Bold = true;
            }

            // توسيع الأعمدة
            worksheet.Column(1).Width = 25;
            worksheet.Column(2).Width = 30;
        }

        private static void FormatHeaderCell(ExcelRange cell)
        {
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        #endregion

        #region دوال عامة للاستخدام من الواجهة

        /// <summary>
        /// تصدير التقرير مع اختيار النوع
        /// </summary>
        public static bool ExportReport<T>(IEnumerable<T> data, string filePath, ExportFormat format)
        {
            try
            {
                switch (format)
                {
                    case ExportFormat.CSV:
                        return ExportToCSV(data, filePath);

                    case ExportFormat.Excel:
                        return ExportToExcel(data, filePath);

                    case ExportFormat.PDF:
                        return ExportToPDF(data, filePath, "تقرير النظام");

                    case ExportFormat.Text:
                        string content = GenerateTextReportFromData(data);
                        return ExportToText(content, filePath);

                    default:
                        throw new ArgumentException("تنسيق التصدير غير مدعوم");
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// عرض نافذة اختيار الملف للتصدير
        /// </summary>
        public static string ShowSaveFileDialog(string defaultFileName, string filter)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter,
                DefaultExt = Path.GetExtension(defaultFileName)
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private static string GenerateTextReportFromData<T>(IEnumerable<T> data)
        {
            var sb = new StringBuilder();
            _ = sb.AppendLine($"تقرير تم إنشاؤه في: {DateTime.Now:yyyy/MM/dd HH:mm}");
            _ = sb.AppendLine();
            _ = sb.AppendLine("البيانات:");
            _ = sb.AppendLine("-".PadRight(50, '-'));

            foreach (var item in data)
            {
                _ = sb.AppendLine(item.ToString());
            }

            return sb.ToString();
        }

        private static List<ExportColumnDescriptor> ResolveExportColumns<T>(IReadOnlyDictionary<string, string> displayNameMap)
        {
            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToList();

            if (displayNameMap == null || displayNameMap.Count == 0)
            {
                return [.. properties.Select(p => new ExportColumnDescriptor(p, p.Name))];
            }

            var selected = new List<ExportColumnDescriptor>();
            foreach (var pair in displayNameMap)
            {
                var property = properties.FirstOrDefault(p => p.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase));
                if (property != null)
                {
                    selected.Add(new ExportColumnDescriptor(property, pair.Value));
                }
            }

            return selected.Count > 0
                ? selected
                : [.. properties.Select(p => new ExportColumnDescriptor(p, p.Name))];
        }

        private static string EscapeCsv(string value, string delimiter)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Contains(delimiter) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        private static void EnsureEpplusLicenseConfigured()
        {
            try
            {
                var packageType = typeof(ExcelPackage);
                var licenseProperty = packageType.GetProperty("License", BindingFlags.Public | BindingFlags.Static);
                var licenseObject = licenseProperty?.GetValue(null);

                if (licenseObject != null)
                {
                    var licenseType = licenseObject.GetType();
                    var setPersonal = licenseType.GetMethod("SetNonCommercialPersonal", [typeof(string)]);
                    if (setPersonal != null)
                    {
                        _ = setPersonal.Invoke(licenseObject, ["GroceryStoreManagement"]);
                        return;
                    }

                    var setOrganization = licenseType.GetMethod("SetNonCommercialOrganization", [typeof(string)]);
                    if (setOrganization != null)
                    {
                        _ = setOrganization.Invoke(licenseObject, ["GroceryStoreManagement"]);
                        return;
                    }
                }

                // fallback لإصدارات EPPlus الأقدم التي تستخدم LicenseContext.
                var licenseContextProperty = packageType.GetProperty("LicenseContext", BindingFlags.Public | BindingFlags.Static);
                if (licenseContextProperty != null)
                {
                    object nonCommercial = Enum.Parse(licenseContextProperty.PropertyType, "NonCommercial");
                    licenseContextProperty.SetValue(null, nonCommercial);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"تعذر ضبط رخصة EPPlus تلقائياً: {ex.Message}");
            }
        }

        #endregion

        private sealed record ExportColumnDescriptor(PropertyInfo Property, string DisplayName);
    }

    #region أنواع التصدير

    public enum ExportFormat
    {
        CSV,
        Excel,
        PDF,
        Text,
        JSON,
        XML
    }

    public enum ReportType
    {
        DailySales,
        MonthlySales,
        ProductSales,
        CustomerPurchases,
        Inventory,
        SupplierPerformance,
        Comprehensive
    }

    #endregion
}

