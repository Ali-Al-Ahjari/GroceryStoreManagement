// =====================================================
// ProductsWindow.xaml.cs - كود إدارة المنتجات
// هذا الملف يتحكم في منطق عرض وإدارة المنتجات
// =====================================================

using GroceryStoreManagement.DAL;           // طبقة الوصول للبيانات (Data Access Layer) لجلب وحفظ بيانات المنتجات
using GroceryStoreManagement.Models;        // النماذج (Models) التي تمثل هيكلة البيانات مثل كلاس Product
using System;                               // لاستخدام الأنواع الأساسية مثل Exception, DateTime
using System.Collections.Generic;           // لاستخدام القوائم (Lists) لتخزين البيانات في الذاكرة
using System.Linq;                          // لاستخدام استعلامات LINQ للتصفية والبحث
using System.Windows;                       // لاستخدام عناصر WPF الأساسية مثل MessageBox, RoutedEventArgs
using System.Windows.Controls;              // لاستخدام عناصر التحكم مثل UserControl, Button, TextBox, DataGrid
using System.Windows.Input;                 // للتعامل مع الأحداث مثل ضغطات المفاتيح (KeyDown)
using Microsoft.Win32;                      // لاستخدام نوافذ فتح وحفظ الملفات (OpenFileDialog, SaveFileDialog)
using GroceryStoreManagement.Helpers;       // Helper classes including PermissionHelper

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// صفحة إدارة المنتجات - تعرض جميع المنتجات وتسمح بإضافة/تعديل/حذف المنتجات
    /// ترث من UserControl لأنها تُعرض داخل النافذة الرئيسية وليست نافذة مستقلة بحد ذاتها
    /// </summary>
    public partial class ProductsWindow : UserControl
    {
        // قائمة لتخزين جميع المنتجات محلياً للبحث والتصفية دون الحاجة للرجوع لقاعدة البيانات مع كل حرف يكتب
        private List<Product> _allProducts = [];

        /// <summary>
        /// المُنشئ (Constructor) - يُنفذ مرة واحدة عند إنشاء الصفحة
        /// </summary>
        public ProductsWindow()
        {
            InitializeComponent();  // دالة تلقائية لتهيئة ورسم عناصر واجهة XAML
            LoadCategories();       // استدعاء دالة لتحميل قائمة الفئات في صندوق الاختيار
            LoadProducts();         // استدعاء دالة لجلب المنتجات من قاعدة البيانات وعرضها
        }

        /// <summary>
        /// دالة لتحميل الفئات (Categories) وعرضها في القائمة المنسدلة للفلترة
        /// </summary>
        private void LoadCategories()
        {
            try
            {
                // جلب قائمة أسماء الفئات الفريدة من قاعدة البيانات
                var categories = ProductDAL.GetAllCategories();

                // المرور على كل فئة وإضافتها كعنصر في القائمة المنسدلة (ComboBox)
                foreach (var category in categories)
                {
                    _ = (CategoryFilterComboBox?.Items.Add(new ComboBoxItem { Content = category }));
                }
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ في حال الفشل (يمكن تطويره ليظهر رسالة للمستخدم)
                Console.WriteLine($"خطأ في تحميل الفئات: {ex.Message}");
            }
        }

        /// <summary>
        /// الوظيفة الأساسية لتحميل المنتجات وتحديث الجدول
        /// </summary>
        private void LoadProducts()
        {
            try
            {
                // الخطوة 1: جلب البيانات الخام من قاعدة البيانات وتخزينها في القائمة المحلية
                _allProducts = ProductDAL.GetAllProducts();

                // الخطوة 2: تطبيق أي فلاتر مختارة حالياً (بحث، فئة، حالة مخزون)
                ApplyFilters();

                // الخطوة 3: تحديث الأرقام الإحصائية في أعلى الصفحة (عدد المنتجات، القيمة الإجمالية...)
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                // عرض رسالة خطأ واضحة للمستخدم في حال فشل الاتصال بقاعدة البيانات
                _ = MessageBox.Show($"خطأ في تحميل المنتجات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حساب وعرض إحصائيات سريعة عن المنتجات المعروضة
        /// </summary>
        private void UpdateStatistics()
        {
            try
            {
                // عرض العدد الكلي للمنتجات
                TotalProductsText.Text = _allProducts.Count.ToString();

                decimal totalValue = 0; // متغير لحساب القيمة الإجمالية (سعر البيع * الكمية)
                int lowQuantityCount = 0; // عداد للمنتجات التي قاربت على النفاد

                // تكرار على كل منتج لحساب القيم
                foreach (var product in _allProducts)
                {
                    totalValue += product.SellingPrice * product.Quantity; // إضافة قيمة مخزون هذا المنتج للإجمالي
                    if (product.IsLowStock) // التحقق مما إذا كان المنتج منخفض المخزون
                        lowQuantityCount++;
                }

                // عرض النتائج في النصوص المخصصة لها
                StockValueText.Text = totalValue.ToDisplayCurrency(); // تنسيق كعملة
                LowQuantityCountText.Text = lowQuantityCount.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في تحديث الإحصائيات: {ex.Message}");
            }
        }

        /// <summary>
        /// المحرك الرئيسي للتصفية والبحث - يقوم بفلترة القائمة _allProducts وعرض النتائج
        /// </summary>
        private void ApplyFilters()
        {
            try
            {
                // التحقق من أن القائمة والجدول جاهزين للعمل لتجنب الأخطاء
                if (_allProducts == null || ProductsDataGrid == null)
                    return;

                // تحويل القائمة إلى نوع قابل للاستعلام (Enumerable) لبدء سلسلة الفلترة
                var filteredProducts = _allProducts.AsEnumerable();

                // 1. تطبيق فلتر البحث النصي (إذا كان المستخدم قد كتب شيئاً)
                if (SearchTextBox != null && !string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    string searchTerm = SearchTextBox.Text.ToLower(System.Globalization.CultureInfo.CurrentCulture); // تحويل النص لأحرف صغيرة لتجاهل حالة الأحرف

                    // التصفية بحيث يظهر المنتج إذا كان أي من حقوله يحتوي على نص البحث
                    filteredProducts = filteredProducts.Where(p =>
                        (p.Name != null && p.Name.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) || // البحث في الاسم
                        (p.Code != null && p.Code.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) || // البحث في الكود
                        (p.Category != null && p.Category.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) || // البحث في الفئة
                        (p.SupplierName != null && p.SupplierName.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) // البحث في اسم المورد
                    );
                }

                // 2. تطبيق فلتر الفئة (إذا اختار المستخدم فئة محددة)
                if (CategoryFilterComboBox != null && CategoryFilterComboBox.SelectedIndex > 0)
                {
                    var selectedCategory = (CategoryFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedCategory))
                    {
                        filteredProducts = filteredProducts.Where(p => p.Category == selectedCategory);
                    }
                }

                // 3. تطبيق فلتر حالة المخزون (منخفض، متوسط، جيد)
                if (StockFilterComboBox != null)
                {
                    switch (StockFilterComboBox.SelectedIndex)
                    {
                        case 1: // الخيار: منخفض
                            filteredProducts = filteredProducts.Where(p => p.Status == "Low");
                            break;
                        case 2: // الخيار: متوسط
                            filteredProducts = filteredProducts.Where(p => p.Status == "Medium");
                            break;
                        case 3: // الخيار: جيد
                            filteredProducts = filteredProducts.Where(p => p.Status == "Good");
                            break;
                    }
                }

                // أخيراً: تعيين القائمة المفلترة كمصدر لبيانات الجدول ليتم عرضها
                ProductsDataGrid.ItemsSource = filteredProducts.ToList();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تصفية المنتجات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حدث يتم استدعاؤه عند تغيير أي خيار في القوائم المنسدلة (الفلتر)
        /// </summary>
        private void FilterChanged(object sender, EventArgs e)
        {
            ApplyFilters(); // إعادة تطبيق الفلاتر
        }

        /// <summary>
        /// عند النقر على أيقونة البحث
        /// </summary>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// عند الضغط على زر Enter داخل مربع البحث لتنفيذ البحث فوراً
        /// </summary>
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyFilters();
            }
        }

        /// <summary>
        /// فتح نافذة إضافة منتج جديد
        /// </summary>
        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.AddProducts)) return;

                var dialog = new ProductDialog(); // إنشاء نافذة الحوار

                // ShowDialog يعرض النافذة وينتظر حتى يغلقها المستخدم
                // إذا أرجع true فهذا يعني أن المستخدم ضغط "حفظ" ونجحت العملية
                if (dialog.ShowDialog() == true)
                    LoadProducts(); // إعادة تحميل القائمة لإظهار المنتج الجديد
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في إضافة المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// فتح نافذة تعديل المنتج المحدد
        /// </summary>
        private void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.EditProducts)) return;

                Product selectedProduct = null;

                // تحديد المنتج: إما من الزر الذي تم ضغطه (في حال وجود زر تعديل لكل سطر)
                if (sender is Button button && button.Tag is int productId)
                {
                    selectedProduct = _allProducts.FirstOrDefault(p => p.ProductID == productId);
                }
                // أو من السطر المحدد حالياً في الجدول
                else
                {
                    selectedProduct = ProductsDataGrid.SelectedItem as Product;
                }

                // التأكد من أن هناك منتج تم تحديده فعلاً
                if (selectedProduct == null)
                {
                    _ = MessageBox.Show("يرجى اختيار منتج للتعديل", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // فتح النافذة وتمرير بيانات المنتج لها
                var dialog = new ProductDialog(selectedProduct);
                if (dialog.ShowDialog() == true)
                    LoadProducts(); // تحديث الجدول بعد التعديل
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تعديل المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حذف المنتج المحدد
        /// </summary>
        private void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.DeleteProducts)) return;

                Product selectedProduct = null;

                // تحديد المنتج بنفس طريقة التعديل
                if (sender is Button button && button.Tag is int productId)
                {
                    selectedProduct = _allProducts.FirstOrDefault(p => p.ProductID == productId);
                }
                else
                {
                    selectedProduct = ProductsDataGrid.SelectedItem as Product;
                }

                if (selectedProduct == null)
                {
                    _ = MessageBox.Show("يرجى اختيار منتج للحذف", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // طلب تأكيد الحذف من المستخدم أولاً
                var result = MessageBox.Show(
                    $"هل تريد حذف المنتج '{selectedProduct.Name}'؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                // إذا وافق المستخدم
                if (result == MessageBoxResult.Yes)
                {
                    // محاولة الحذف من قاعدة البيانات
                    bool success = ProductDAL.DeleteProduct(selectedProduct.ProductID);
                    if (success)
                    {
                        LoadProducts(); // تحديث الجدول لإخفاء المحذوف
                        _ = MessageBox.Show("تم حذف المنتج بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حذف المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// فتح نافذة سريعة لتعديل كمية المخزون فقط
        /// </summary>
        private void UpdateQuantityButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.ManageStock)) return;

                Product selectedProduct = null;

                if (sender is Button button && button.Tag is int productId)
                {
                    selectedProduct = _allProducts.FirstOrDefault(p => p.ProductID == productId);
                }
                else
                {
                    selectedProduct = ProductsDataGrid.SelectedItem as Product;
                }

                if (selectedProduct == null)
                {
                    _ = MessageBox.Show("يرجى اختيار منتج لتحديث الكمية", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new UpdateQuantityDialog(selectedProduct);
                if (dialog.ShowDialog() == true)
                    LoadProducts();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحديث الكمية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// اختصار لتعديل الأسعار (نفس وظيفة التعديل الكامل حالياً)
        /// </summary>
        private void EditPricesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionHelper.CheckPermission(PermissionKeys.EditPrices)) return;
            if (ProductsDataGrid.SelectedItem is not Product)
            {
                _ = MessageBox.Show("يرجى اختيار منتج لتعديل الأسعار", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // إعادة توجيه لزر التعديل
            EditProductButton_Click(sender, e);
        }

        /// <summary>
        /// عند تغيير السطر المحدد في الجدول، يتم تحديث لوحة التفاصيل الجانبية (إذا وجدت)
        /// </summary>
        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // إذا تم اختيار منتج
            if (ProductsDataGrid.SelectedItem is Product selectedProduct)
            {
                // تعبئة النصوص بالبيانات، وضع شرط (-) في حال كانت القيمة فارغة
                DetailCodeText.Text = selectedProduct.Code ?? "-";
                DetailNameText.Text = selectedProduct.Name;
                DetailPurchasePriceText.Text = selectedProduct.PurchasePrice.ToDisplayCurrency();
                DetailSellingPriceText.Text = selectedProduct.SellingPrice.ToDisplayCurrency();
                DetailQuantityText.Text = selectedProduct.Quantity.ToString();
                DetailMinQuantityText.Text = selectedProduct.MinQuantity.ToString();
                DetailUnitText.Text = selectedProduct.Unit ?? "-";
                DetailCategoryText.Text = selectedProduct.Category ?? "-";
                DetailSupplierText.Text = selectedProduct.SupplierName ?? "-";
                DetailExpiryText.Text = selectedProduct.ExpiryStatusText;
                DetailExpiryText.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(selectedProduct.ExpiryStatusColor);

                // إظهار تحذير إذا كان المخزون منخفضاً
                LowQuantityWarning.Visibility = selectedProduct.IsLowStock
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            // إذا لم يتم اختيار شيء (أو تم إلغاء الاختيار)
            else
            {
                // تفريغ الحقول
                DetailCodeText.Text = "-";
                DetailNameText.Text = "-";
                DetailPurchasePriceText.Text = "-";
                DetailSellingPriceText.Text = "-";
                DetailQuantityText.Text = "-";
                DetailMinQuantityText.Text = "-";
                DetailUnitText.Text = "-";
                DetailCategoryText.Text = "-";
                DetailSupplierText.Text = "-";
                DetailExpiryText.Text = "-";
                LowQuantityWarning.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// وظيفة لتصدير القائمة الحالية إلى ملف CSV (يمكن فتحه ببرنامج Excel)
        /// </summary>
        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // إظهار نافذة حفظ الملف لاختيار المكان والاسم
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.csv)|*.csv",
                    FileName = $"Products_{DateTime.Now:yyyyMMdd}.csv" // اسم افتراضي مع التاريخ
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // إعداد قائمة الأسطر للكتابة
                    var lines = new List<string>
                    {
                        "الكود,اسم المنتج,الوحدة,سعر الشراء,سعر البيع,الكمية,الحد الأدنى,الفئة,المورد" // سطر العناوين
                    };

                    // تحويل كل منتج لسطر نصي مفصول بفواصل
                    foreach (var product in _allProducts)
                    {
                        var line = $"{product.Code},{product.Name},{product.Unit},{product.PurchasePrice},{product.SellingPrice},{product.Quantity},{product.MinQuantity},{product.Category},{product.SupplierName}";
                        lines.Add(line);
                    }

                    // كتابة الملف
                    System.IO.File.WriteAllLines(saveDialog.FileName, lines, System.Text.Encoding.UTF8);
                    _ = MessageBox.Show($"تم تصدير {_allProducts.Count} منتج بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تصدير البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// وظيفة لاستيراد المنتجات من ملف CSV
        /// </summary>
        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.AddProducts)) return;

                // نافذة فتح ملف
                var openDialog = new OpenFileDialog
                {
                    Filter = "Excel Files (*.csv)|*.csv|All Files (*.*)|*.*"
                };

                if (openDialog.ShowDialog() == true)
                {
                    // قراءة جميع الأسطر
                    var lines = System.IO.File.ReadAllLines(openDialog.FileName, System.Text.Encoding.UTF8);
                    int imported = 0;

                    // البدء من السطر 1 لأن السطر 0 هو العناوين
                    for (int i = 1; i < lines.Length; i++)
                    {
                        try
                        {
                            var parts = lines[i].Split(','); // تقسيم السطر
                            if (parts.Length >= 5) // التأكد من وجود الحد الأدنى من البيانات
                            {
                                var product = new Product
                                {
                                    Code = parts[0].Trim(),
                                    Name = parts[1].Trim(),
                                    Unit = parts.Length > 2 ? parts[2].Trim() : "قطعة",
                                    // محاولة تحويل النصوص لأرقام، أو استخدام 0 في حال الفشل
                                    PurchasePrice = parts.Length > 3 ? decimal.Parse(parts[3].Trim()) : 0,
                                    SellingPrice = parts.Length > 4 ? decimal.Parse(parts[4].Trim()) : 0,
                                    Quantity = parts.Length > 5 ? int.Parse(parts[5].Trim()) : 0,
                                    MinQuantity = parts.Length > 6 ? int.Parse(parts[6].Trim()) : 5,
                                    Category = parts.Length > 7 ? parts[7].Trim() : "",
                                    CreatedDate = DateTime.Now
                                };

                                // إضافة المنتج لقاعدة البيانات
                                _ = ProductDAL.AddProduct(product);
                                imported++;
                            }
                        }
                        catch
                        {
                            // تجاهل الأسطر التالفة والاستمرار
                        }
                    }

                    LoadProducts(); // تحديث العرض
                    _ = MessageBox.Show($"تم استيراد {imported} منتج بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في استيراد البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// مسح حقل البحث
        /// </summary>
        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            _ = SearchTextBox.Focus();
        }
    }
}

