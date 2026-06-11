using System; // استيراد المكتبة الأساسية للتعامل مع الأنواع البسيطة والدوال الأساسية
using System.Windows; // استيراد مكتبة النوافذ والعناصر الأساسية للواجهة
using System.Windows.Controls; // استيراد أدوات التحكم مثل الأزرار والقوائم
using System.Windows.Media; // استيراد مكتبة الألوان والفرش (Brushes)
using System.Windows.Threading; // استيراد مكتبة التعامل مع التوقيت والعمليات المتزامنة
using GroceryStoreManagement.DAL; // استيراد طبقة الوصول للبيانات للتعامل مع الإشعارات
using GroceryStoreManagement.Helpers; // استيراد المجلد المساعد الذي يحتوي على أدوات التنبيه
using GroceryStoreManagement.Models; // استيراد النماذج
using System.Collections.Generic; // استيراد مكتبة القوائم والمجموعات
using System.Linq; // استيراد مكتبة الاستعلامات على البيانات
using System.Windows.Media.Animation; // استيراد مكتبة الحركات (Animations)


namespace GroceryStoreManagement.Windows // تعريف اسم المجال الخاص بالنوافذ
{
    /// <summary>
    /// النافذة الرئيسية للتطبيق - تحتوي على القائمة الجانبية ومنطقة العرض الرئيسية
    /// </summary>
    public partial class MainWindow : Window // تعريف كلاس النافذة الرئيسية
    {
        // متغيرات لتخزين مراجع النوافذ الفرعية (لإعادة استخدامها بدلاً من إنشائها كل مرة لتوفير الذاكرة)
        private DashboardWindow _dashboardWindow; // متغير لنافذة لوحة التحكم
        private ProductsWindow _productsWindow; // متغير لنافذة إدارة المنتجات
        private CustomersWindow _customersWindow; // متغير لنافذة إدارة العملاء
        private SuppliersWindow _suppliersWindow; // متغير لنافذة إدارة الموردين
        private SalesWindow _salesWindow; // متغير لنافذة المبيعات (نقطة البيع)
        private PurchasesWindow _purchasesWindow; // متغير لنافذة المشتريات
        private InventoryWindow _inventoryWindow; // متغير لنافذة المخزون
        private ReportsWindow _reportsWindow; // متغير لنافذة التقارير
        private SettingsWindow _settingsWindow; // متغير لنافذة الإعدادات
        private DispatcherTimer _notificationTimer; // مؤقت (Timer) لفحص الإشعارات بشكل دوري
        private bool _isSidebarCollapsed = false; // حالة القائمة الجانبية (مفتوحة/مغلقة)
        private double _expandedSidebarWidth = 250; // آخر عرض موسع للقائمة الجانبية
        private bool _isLoggingOut = false; // تمييز حالة تسجيل الخروج لتجاوز رسالة الإغلاق
        private const string OpenPaneGlyph = "\uE8A0"; // أيقونة فتح القائمة الجانبية
        private const string ClosePaneGlyph = "\uE8A1"; // أيقونة إغلاق القائمة الجانبية


        /// <summary>
        /// المُنشئ - يتم استدعاؤه عند تشغيل التطبيق أول مرة
        /// </summary>
        public MainWindow()
        {
            InitializeComponent(); // دالة لتهيئة ورسم العناصر الموجودة في ملف التصميم (XAML)
            InitializeWindows(); // استدعاء دالة لتهيئة المتغيرات بقيم فارغة
            ApplyPermissions(); // تطبيق الصلاحيات على القائمة الجانبية
            ShowDashboard(); // عرض لوحة التحكم كشاشة افتراضية عند الفتح
        }

        /// <summary>
        /// دالة لتهيئة متغيرات النوافذ بقيم فارغة (تعيينها كـ null)
        /// </summary>
        private void InitializeWindows()
        {
            try // بداية كتلة التعامل مع الأخطاء
            {
                // تعيين جميع متغيرات النوافذ كـ null (فارغة) لضمان عدم وجود بيانات قديمة
                _dashboardWindow = null;
                _productsWindow = null;
                _customersWindow = null;
                _suppliersWindow = null;
                _salesWindow = null;
                _purchasesWindow = null;
                _inventoryWindow = null;
                _reportsWindow = null;
                _settingsWindow = null;
            }
            catch (Exception ex) // في حال حدوث خطأ
            {
                // عرض رسالة خطأ للمستخدم
                _ = MessageBox.Show($"خطأ في تهيئة النوافذ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تطبيق الصلاحيات على عناصر الواجهة لإخفاء ما لا يملك المستخدم صلاحية الوصول إليه
        /// </summary>
        private void ApplyPermissions()
        {
            try
            {
                // إخفاء الأزرار بناءً على الصلاحيات
                BtnDashboard.Visibility = PermissionHelper.CanAccessDashboard ? Visibility.Visible : Visibility.Collapsed;
                BtnProducts.Visibility = PermissionHelper.HasPermission(PermissionKeys.ViewProducts) ? Visibility.Visible : Visibility.Collapsed;
                BtnCustomers.Visibility = PermissionHelper.HasPermission(PermissionKeys.ViewCustomers) ? Visibility.Visible : Visibility.Collapsed;
                BtnInvoices.Visibility = PermissionHelper.HasPermission(PermissionKeys.ViewSales) ? Visibility.Visible : Visibility.Collapsed;

                BtnSuppliers.Visibility = PermissionHelper.HasPermission(PermissionKeys.ViewSuppliers) ? Visibility.Visible : Visibility.Collapsed;
                BtnPurchases.Visibility = PermissionHelper.HasPermission(PermissionKeys.ViewPurchases) ? Visibility.Visible : Visibility.Collapsed;
                BtnInventory.Visibility = PermissionHelper.HasPermission(PermissionKeys.ViewInventoryReports) ? Visibility.Visible : Visibility.Collapsed; // أو ManageStock
                BtnReports.Visibility = PermissionHelper.CanViewReports ? Visibility.Visible : Visibility.Collapsed;
                BtnSettings.Visibility = PermissionHelper.HasPermission(PermissionKeys.AccessSettings) ? Visibility.Visible : Visibility.Collapsed;

                RefreshCurrentUserHeader();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في تطبيق الصلاحيات على الواجهة");
            }
        }

        /// <summary>
        /// تحديث اسم المستخدم والدور في الهيدر العلوي
        /// </summary>
        private void RefreshCurrentUserHeader()
        {
            if (SessionContext.CurrentUser == null)
            {
                TxtCurrentUserFullName.Text = "المستخدم";
                TxtCurrentUserRole.Text = "—";
                return;
            }

            TxtCurrentUserFullName.Text = string.IsNullOrWhiteSpace(SessionContext.CurrentUser.FullName)
                ? SessionContext.CurrentUser.Username
                : SessionContext.CurrentUser.FullName;

            TxtCurrentUserRole.Text = SessionContext.CurrentUser.RoleName
                ?? (SessionContext.CurrentUser.Username == "admin" ? "مدير النظام" : "موظف");
        }

        /// <summary>
        /// إعادة تحميل بيانات المستخدم الحالي من قاعدة البيانات
        /// </summary>
        private static void ReloadCurrentSessionUser()
        {
            if (SessionContext.CurrentUser == null) return;

            var refreshedUser = UserDAL.GetUserById(SessionContext.CurrentUser.UserID);
            if (refreshedUser != null)
            {
                SessionContext.CurrentUser = refreshedUser;
            }
        }

        /// <summary>
        /// فتح نافذة حساب المستخدم الحالي
        /// </summary>
        private void OpenMyAccountDialog()
        {
            try
            {
                var accountWindow = new MyAccountWindow
                {
                    Owner = this
                };

                if (accountWindow.ShowDialog() == true)
                {
                    ReloadCurrentSessionUser();
                    ApplyPermissions();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في فتح نافذة الحساب");
                _ = MessageBox.Show($"تعذر فتح نافذة الحساب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Navigation Methods // منطقة لتجميع دوال التنقل بين الصفحات



        /// <summary>
        /// دالة لعرض نافذة لوحة التحكم (الرئيسية)
        /// </summary>
        private void ShowDashboard()
        {
            if (!PermissionHelper.CheckPermission(PermissionKeys.AccessDashboard)) return;

            try
            {
                _dashboardWindow ??= new DashboardWindow();

                MainFrame.Content = _dashboardWindow;

                _ = (PageTitle?.Text = "لوحة التحكم");

                UpdateActiveButton(BtnDashboard);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(
                    $"خطأ في تحميل لوحة التحكم: {ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
        private void ShowProducts()
        {
            if (!PermissionHelper.CheckPermission(PermissionKeys.ViewProducts)) return;

            try
            {
                _productsWindow ??= new ProductsWindow();

                MainFrame.Content = _productsWindow;

                _ = (PageTitle?.Text = "المنتجات");

                UpdateActiveButton(BtnProducts);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(
                    $"خطأ في تحميل المنتجات: {ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// دالة لعرض نافذة العملاء
        /// </summary>
        private void ShowCustomers()
        {
            if (!PermissionHelper.CheckPermission(PermissionKeys.ViewCustomers)) return;

            try
            {
                _customersWindow ??= new CustomersWindow();

                MainFrame.Content = _customersWindow;
                _ = (PageTitle?.Text = "العملاء");
                UpdateActiveButton(BtnCustomers);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// دالة لعرض نافذة الموردين
        /// </summary>
        private void ShowSuppliers()
        {
            if (!PermissionHelper.CheckPermission(PermissionKeys.ViewSuppliers)) return;

            try
            {
                _suppliersWindow ??= new SuppliersWindow();

                MainFrame.Content = _suppliersWindow;
                _ = (PageTitle?.Text = "الموردون");
                UpdateActiveButton(BtnSuppliers);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل الموردين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// دالة لعرض نافذة المبيعات (الفواتير)
        /// </summary>
        private void ShowSales()
        {
            if (!PermissionHelper.CheckPermission(PermissionKeys.ViewSales)) return;

            try
            {
                _salesWindow ??= new SalesWindow();

                MainFrame.Content = _salesWindow;
                _ = (PageTitle?.Text = "المبيعات");
                UpdateActiveButton(BtnInvoices);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل المبيعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// دالة لعرض نافذة المشتريات
        /// </summary>
        private void ShowPurchases()
        {
            if (!PermissionHelper.CheckPermission(PermissionKeys.ViewPurchases)) return;

            try
            {
                _purchasesWindow ??= new PurchasesWindow();

                MainFrame.Content = _purchasesWindow;
                _ = (PageTitle?.Text = "المشتريات");
                UpdateActiveButton(BtnPurchases);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل المشتريات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// دالة لعرض نافذة المخزون (الجرد)
        /// </summary>
        private void ShowInventory()
        {
            // استخدام صلاحية ManageStock للمخزون
            if (!PermissionHelper.CheckPermission(PermissionKeys.ManageStock)) return;

            try
            {
                _inventoryWindow ??= new InventoryWindow();

                MainFrame.Content = _inventoryWindow;
                _ = (PageTitle?.Text = "المخزون");
                UpdateActiveButton(BtnInventory);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل المخزون: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// دالة لعرض نافذة التقارير
        /// </summary>
        private void ShowReports()
        {
            if (!PermissionHelper.CheckPermission(PermissionKeys.ViewReports)) return;

            try
            {
                _reportsWindow ??= new ReportsWindow();

                MainFrame.Content = _reportsWindow;
                _ = (PageTitle?.Text = "التقارير");
                UpdateActiveButton(BtnReports);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل التقارير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// دالة لعرض نافذة الإعدادات مع إمكانية تحديد قسم معين لفتحه
        /// </summary>
        private void ShowSettings(string section = "System")
        {
            try
            {
                _settingsWindow ??= new SettingsWindow();
                _settingsWindow.SelectSection(section);
                MainFrame.Content = _settingsWindow;
                _ = (PageTitle?.Text = (section == "Users") ? "المستخدمين" : "الإعدادات");
                UpdateActiveButton(BtnSettings);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// إعادة تحميل الصفحة الحالية لإظهار أحدث البيانات من قاعدة البيانات
        /// </summary>
        private void RefreshCurrentView()
        {
            try
            {
                switch (MainFrame.Content)
                {
                    case DashboardWindow:
                        _dashboardWindow = null;
                        ShowDashboard();
                        break;
                    case ProductsWindow:
                        _productsWindow = null;
                        ShowProducts();
                        break;
                    case CustomersWindow:
                        _customersWindow = null;
                        ShowCustomers();
                        break;
                    case SuppliersWindow:
                        _suppliersWindow = null;
                        ShowSuppliers();
                        break;
                    case SalesWindow:
                        _salesWindow = null;
                        ShowSales();
                        break;
                    case PurchasesWindow:
                        _purchasesWindow = null;
                        ShowPurchases();
                        break;
                    case InventoryWindow:
                        _inventoryWindow = null;
                        ShowInventory();
                        break;
                    case ReportsWindow:
                        _reportsWindow = null;
                        ShowReports();
                        break;
                    case SettingsWindow:
                        {
                            string section = (PageTitle?.Text == "المستخدمين") ? "Users" : "System";
                            _settingsWindow = null;
                            ShowSettings(section);
                            break;
                        }
                    default:
                        if (MainFrame.Content is FrameworkElement currentContent)
                        {
                            if (Activator.CreateInstance(currentContent.GetType()) is FrameworkElement refreshedContent)
                            {
                                MainFrame.Content = refreshedContent;
                            }
                            else
                            {
                                MainFrame.Content = null;
                                MainFrame.Content = currentContent;
                            }
                        }
                        break;
                }

                CheckNotifications();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحديث الصفحة الحالية من الزر العام");
                _ = MessageBox.Show($"تعذر تحديث الصفحة الحالية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Public Navigation Methods // دوال عامة يمكن استدعاؤها من نوافذ أخرى

        // دالة عامة للانتقال لصفحة التقارير، يمكن استخدامها من أزرار الصفحة الرئيسية مثلاً
        public void NavigateToReports()
        {
            ShowReports();
        }

        // دالة عامة للانتقال للإعدادات
        public void NavigateToSettings()
        {
            ShowSettings();
        }

        #endregion

        #region Button State Management // منطقة التحكم في شكل الأزرار

        /// <summary>
        /// دالة تحديث مظهر الزر النشط في القائمة الجانبية لإخبار المستخدم أين هو
        /// </summary>
        /// <param name="activeButton">الزر الذي تم النقر عليه</param>
        private void UpdateActiveButton(Button activeButton)
        {
            // أولاً: إعادة تعيين جميع الأزرار للحالة الافتراضية
            ResetAllButtons();

            // ثانياً: تظليل الزر الجديد باستخدام التاج Active ليتوافق مع النمط (Style)
            activeButton?.SetCurrentValue(TagProperty, "Active");
        }

        /// <summary>
        /// دالة مساعدة لإعادة ألوان جميع الأزرار للصورة الأصلية
        /// </summary>
        private void ResetAllButtons()
        {
            // دالة داخلية صغيرة لإرجاع زر واحد لحالته الأصلية
            void ResetButton(Button btn)
            {
                btn?.SetCurrentValue(TagProperty, null); // إزالة حالة التفعيل - الستايلات ستتولى الباقي
                btn?.ClearValue(BackgroundProperty); // إزالة أي قيم محلية
                btn?.ClearValue(ForegroundProperty); // إزالة أي قيم محلية
            }

            // تطبيق الدالة على كل أزرار القائمة
            ResetButton(BtnDashboard);
            ResetButton(BtnProducts);
            ResetButton(BtnCustomers);
            ResetButton(BtnSuppliers);
            ResetButton(BtnPurchases);
            ResetButton(BtnInvoices);
            ResetButton(BtnInventory);
            ResetButton(BtnReports);
            ResetButton(BtnSettings);
        }

        #endregion

        #region Button Click Event Handlers // معالجات أحداث الضغط على الأزرار

        // عند الضغط على زر "لوحة التحكم"
        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboard();
        }

        // عند الضغط على زر "المنتجات"
        private void BtnProducts_Click(object sender, RoutedEventArgs e)
        {
            ShowProducts();
        }

        // عند الضغط على زر "العملاء"
        private void BtnCustomers_Click(object sender, RoutedEventArgs e)
        {
            ShowCustomers();
        }

        // عند الضغط على زر "الموردين"
        private void BtnSuppliers_Click(object sender, RoutedEventArgs e)
        {
            ShowSuppliers();
        }

        // عند الضغط على زر "الفواتير"
        private void BtnSales_Click(object sender, RoutedEventArgs e)
        {
            ShowSales();
        }

        // عند الضغط على زر "المشتريات"
        private void BtnPurchases_Click(object sender, RoutedEventArgs e)
        {
            ShowPurchases();
        }

        // عند الضغط على زر "المخزون"
        private void BtnInventory_Click(object sender, RoutedEventArgs e)
        {
            ShowInventory();
        }

        // عند الضغط على زر "التقارير"
        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            ShowReports();
        }

        // عند الضغط على زر "الإعدادات"
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        // عند الضغط على زر "تحديث الآن" في الهيدر العلوي
        private void BtnGlobalRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshCurrentView();
        }

        // عند الضغط على منطقة الحساب في الشريط العلوي
        private void BtnUserMenu_Click(object sender, RoutedEventArgs e)
        {
            UserMenuPopup.IsOpen = !UserMenuPopup.IsOpen;
        }

        // فتح حسابي من قائمة المستخدم
        private void OpenMyAccount_Click(object sender, RoutedEventArgs e)
        {
            UserMenuPopup.IsOpen = false;
            OpenMyAccountDialog();
        }

        // فتح الإعدادات من قائمة المستخدم
        private void OpenSettingsFromUserMenu_Click(object sender, RoutedEventArgs e)
        {
            UserMenuPopup.IsOpen = false;
            if (!PermissionHelper.CheckPermission(PermissionKeys.AccessSettings)) return;
            ShowSettings();
        }

        // عند الضغط على زر "تسجيل الخروج"
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            UserMenuPopup.IsOpen = false;
            LogoutToLogin();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // عند الضغط على زر القائمة (فتح/إغلاق)
        private void BtnMenuToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isSidebarCollapsed)
                {
                    // فتح القائمة (Expand)
                    AnimateSidebar(_expandedSidebarWidth);
                    SetMenuToggleIcon(ClosePaneGlyph);
                }
                else
                {
                    // حفظ العرض الحالي قبل الإغلاق لإعادته كما هو عند الفتح
                    if (SidebarBorder.ActualWidth > 1)
                    {
                        _expandedSidebarWidth = SidebarBorder.ActualWidth;
                    }

                    // إغلاق القائمة (Collapse)
                    AnimateSidebar(0);
                    SetMenuToggleIcon(OpenPaneGlyph);
                }
                _isSidebarCollapsed = !_isSidebarCollapsed;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في تبديل حالة القائمة الجانبية");
            }
        }

        private void SetMenuToggleIcon(string glyph)
        {
            BtnMenuToggle.Content = new TextBlock
            {
                Text = glyph,
                FontFamily = (FontFamily)FindResource("IconFont"),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        /// <summary>
        /// دالة لتحريك القائمة الجانبية بشكل سلس
        /// </summary>
        private void AnimateSidebar(double targetWidth)
        {
            DoubleAnimation animation = new()
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // تحريك عرض العمود الأول في الجريد
            // ملاحظة: لا يمكن تحريك GridLength مباشرة، لذا سنقوم بتحريك Width الخاص بالـ Border
            // وسنقوم بتغيير عرض العمود ليكون متوافقاً

            if (targetWidth > 0) SidebarBorder.Visibility = Visibility.Visible;

            animation.Completed += (s, e) =>
            {
                if (targetWidth == 0) SidebarBorder.Visibility = Visibility.Collapsed;
                SidebarBorder.Width = targetWidth;
                SidebarColumn.Width = new GridLength(targetWidth);
            };

            // سنقوم بتحريك العرض الفعلي للـ Border
            SidebarBorder.BeginAnimation(WidthProperty, animation);

            // تحديث عرض العمود باستمرار خلال الحركة
            CompositionTarget.Rendering += UpdateColumnWidth;
            animation.Completed += (s, e) => CompositionTarget.Rendering -= UpdateColumnWidth;
        }

        private void UpdateColumnWidth(object sender, EventArgs e)
        {
            if (SidebarBorder.Width >= 0)
            {
                SidebarColumn.Width = new GridLength(SidebarBorder.ActualWidth);
            }
        }


        #endregion

        #region Application Lifecycle // دورة حياة التطبيق (الخروج، الإغلاق، التحميل)

        /// <summary>
        /// تسجيل خروج المستخدم الحالي والعودة إلى نافذة تسجيل الدخول
        /// </summary>
        private void LogoutToLogin()
        {
            var result = MessageBox.Show("هل تريد تسجيل الخروج والعودة إلى شاشة الدخول؟",
                                         "تسجيل الخروج",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _isLoggingOut = true;
                _notificationTimer?.Stop();

                // إنهاء الجلسة الحالية
                SessionContext.CurrentUser = null;
                SessionContext.CurrentShiftID = null;

                // فتح نافذة تسجيل الدخول أولاً حتى لا يغلق التطبيق
                var loginWindow = new LoginWindow();
                Application.Current.MainWindow = loginWindow;
                loginWindow.Show();

                // إغلاق النافذة الحالية بعد فتح نافذة الدخول
                Close();
            }
            catch (Exception ex)
            {
                _isLoggingOut = false;
                Logger.LogError(ex, "فشل تسجيل الخروج");
                _ = MessageBox.Show($"تعذر تنفيذ تسجيل الخروج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// دالة للخروج من التطبيق مباشرة بدون رسائل تأكيد
        /// </summary>
        private static void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// حدث يتم تنفيذه عند محاولة إغلاق النافذة (مثلاً عند الضغط على X)
        /// </summary>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isLoggingOut)
            {
                return;
            }

            SaveApplicationState(); // حفظ تلقائي قبل الإغلاق
        }

        /// <summary>
        /// دالة لحفظ حالة التطبيق الحالية (فارغة حالياً ويمكن تطويرها)
        /// </summary>
        private static void SaveApplicationState()
        {
            try
            {
                // مكان مخصص لكود حفظ التغييرات
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving application state: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث يتم تنفيذه بمجرد انتهاء تحميل النافذة وظهورها
        /// </summary>


        /// <summary>
        /// حدث يتم تنفيذه بمجرد انتهاء تحميل النافذة وظهورها
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // مزامنة العرض الموسع مع عرض XAML الحالي (حتى لا يعود لقيمة أقدم)
            if (SidebarBorder.Width > 0)
            {
                _expandedSidebarWidth = SidebarBorder.Width;
            }

            SetMenuToggleIcon(_isSidebarCollapsed ? OpenPaneGlyph : ClosePaneGlyph);

            // تطبيق الصلاحيات
            ApplyPermissions();

            // إنشاء مؤقت للإشعارات
            _notificationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5) // ضبط المؤقت ليعمل كل 5 دقائق
            };
            _notificationTimer.Tick += (s, args) => CheckNotifications(); // ربط المؤقت بدالة فحص الإشعارات
            _notificationTimer.Start(); // بدء تشغيل المؤقت

            // إجراء فحص أولي للإشعارات عند الفتح فوراً
            CheckNotifications(true);

            // تطبيق المظهر الأنيق ونظام Mica وتحديث أيقونة المظهر
            ThemeManager.ApplyWindowBackdrop(this);
            UpdateThemeToggleButtonIcon();
        }

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
            UpdateThemeToggleButtonIcon();
        }

        private void UpdateThemeToggleButtonIcon()
        {
            if (TxtThemeIcon != null)
            {
                TxtThemeIcon.Text = ThemeManager.CurrentTheme == AppTheme.Dark ? "\uE706" : "\uE708";
            }
        }

        // دالة لفحص وجود إشعارات جديدة (مثل المنتجات التي قاربت الانتهاء)
        private void CheckNotifications(bool isStartup = false)
        {
            try
            {
                // تشغيل خدمة التنبيهات لتوليد إشعارات جديدة إذا لزم الأمر
                AlertService.RunChecks();

                // جلب عدد الإشعارات غير المقروءة من قاعدة البيانات
                var unreadNotes = NotificationDAL.GetUnreadNotifications();
                int count = unreadNotes.Count;

                // تحديث عداد الإشعارات الأحمر (Badge)
                if (count > 0)
                {
                    NotificationBadge.Visibility = Visibility.Visible; // إظهار العداد
                    NotificationCountPicker.Text = count > 99 ? "99+" : count.ToString(); // تنسيق الرقم

                    if (isStartup)
                    {
                        var criticalCount = unreadNotes.Count(n => n.Type == "Critical");
                        if (criticalCount > 0)
                        {
                            _ = MessageBox.Show($"يوجد {criticalCount} تنبيهات هامة (منتجات منتهية الصلاحية). يرجى مراجعة الإشعارات.", "تنبيه هام", MessageBoxButton.OK, MessageBoxImage.Warning);
                            // فتح قائمة الإشعارات تلقائياً
                            NotificationsPopup.IsOpen = true;
                            LoadNotificationsList();
                        }
                    }
                }
                else
                {
                    NotificationBadge.Visibility = Visibility.Collapsed; // إخفاء العداد إذا كان 0
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحديث إشعارات الواجهة");
            }
        }

        // عند الضغط على زر الجرس (الإشعارات)
        private void BtnNotifications_Click(object sender, RoutedEventArgs e)
        {
            // تبديل حالة القائمة المنبثقة (فتح/إغلاق)
            NotificationsPopup.IsOpen = !NotificationsPopup.IsOpen;

            // إذا تم فتح القائمة
            if (NotificationsPopup.IsOpen)
            {
                LoadNotificationsList(); // تحميل محتوى الإشعارات
            }
        }

        // دالة لتحميل قائمة الإشعارات وعرضها في القائمة
        private void LoadNotificationsList()
        {
            try
            {
                // جلب آخر 20 إشعار من قاعدة البيانات
                var notes = NotificationDAL.GetAllNotifications(20);
                NotificationsList.ItemsSource = notes; // ربط البيانات بالقائمة

                // إذا لم توجد إشعارات، اعرض رسالة "لا توجد إشعارات"
                if (notes.Count == 0)
                {
                    NotificationsList.Visibility = Visibility.Collapsed;
                    EmptyNotificationsText.Visibility = Visibility.Visible;
                }
                else
                {
                    NotificationsList.Visibility = Visibility.Visible;
                    EmptyNotificationsText.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحميل قائمة الإشعارات");
            }
        }

        // زر لتحديد جميع الإشعارات كمقروءة
        private void MarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            NotificationDAL.MarkAllAsRead(); // تحديث قاعدة البيانات
            CheckNotifications(); // تحديث العداد الخارجي (ليختفي)
            LoadNotificationsList(); // تحديث القائمة الداخلية (لتتغير الألوان)
        }

        // زر لعرض سجل الإشعارات الكامل
        private void ViewAllNotifications_Click(object sender, RoutedEventArgs e)
        {
            NotificationsPopup.IsOpen = false; // إغلاق القائمة الصغيرة
            var win = new NotificationsWindow(); // إنشاء نافذة الإشعارات الكاملة
            _ = win.ShowDialog(); // عرضها
            CheckNotifications(); // تحديث العداد بعد إغلاق النافذة
        }

        // زر لحذف إشعار محدد (علامة X)
        private void DeleteNotification_Click(object sender, RoutedEventArgs e)
        {
            // التأكد من أن المرسل هو زر ويحتوي على معرف الإشعار
            if (sender is Button btn && btn.Tag is int id)
            {
                NotificationDAL.DeleteNotification(id); // حذف الإشعار من البيانات
                LoadNotificationsList(); // إعادة تحميل القائمة
                CheckNotifications(); // تحديث العداد
            }
        }

        #endregion

        #region Keyboard Shortcuts // اختصارات لوحة المفاتيح

        /// <summary>
        /// حدث يتم تنفيذه عند ضغط أي مفتاح في الكيبورد
        /// </summary>
        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // التحقق مما إذا كان زر Ctrl مضغوطاً
            if (e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                // التحقق من المفتاح الآخر
                switch (e.Key)
                {
                    case System.Windows.Input.Key.D1: // Ctrl + 1
                        ShowDashboard(); // عرض الرئيسية
                        break;
                    case System.Windows.Input.Key.D2: // Ctrl + 2
                        ShowProducts(); // عرض المنتجات
                        break;
                    case System.Windows.Input.Key.D3: // Ctrl + 3
                        ShowCustomers(); // عرض العملاء
                        break;
                    case System.Windows.Input.Key.D4: // Ctrl + 4
                        ShowSuppliers(); // عرض الموردين
                        break;
                    case System.Windows.Input.Key.D5: // Ctrl + 5
                        ShowSales(); // عرض المبيعات
                        break;
                    case System.Windows.Input.Key.D6: // Ctrl + 6
                        ShowInventory(); // عرض المخزون
                        break;
                    case System.Windows.Input.Key.D7: // Ctrl + 7
                        ShowReports(); // عرض التقارير
                        break;
                    case System.Windows.Input.Key.R: // Ctrl + R
                        RefreshCurrentView(); // تحديث الصفحة الحالية
                        break;
                    case System.Windows.Input.Key.Q: // Ctrl + Q
                        ExitApplication(); // خروج
                        break;
                }
            }
        }

        #endregion
    }
}
