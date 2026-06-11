# Original User Request

## Initial Request — 2026-06-09T10:13:51+03:00

A complete refactoring and modern redesign of the Grocery Store Management application's user interface using a unified, centralized WPF Design System. The redesign covers all windows and dialogs, supporting dynamic Light and Dark theme switching at runtime, establishing consistent spacing, sizing, borders, and input fields, and implementing high-performance smooth transition animations.

Working directory: d:\D\SAM\3\نظام متكامل لادارة المتجر
Integrity mode: development

## Requirements

### R1. Dynamic Theme, Spacing, and Motion Design System
- **Theme Dictionary Partitioning**: Split colors and brushes into two separate theme dictionaries: `LightTheme.xaml` and `DarkTheme.xaml` under a new folder `GroceryStoreManagement/Styles/Themes/`.
- **Dynamic Styling**: Modify `Styles.xaml` to merge the active theme dictionary dynamically. Reference all theme-dependent colors, brushes, and shadow resources using `{DynamicResource KeyName}` instead of `StaticResource`, allowing instant runtime theme swapping without restarting the application.
- **Unified Layout Tokens**: Ensure all spacing (margins, padding, border thicknesses) and corner radii are bound to the centralized layout tokens (e.g. `CornerRadiusControlNormal`, `PaddingControlNormal`, `MarginStackNormal`).
- **High-Performance Micro-Animations**: Implement smooth visual transitions (for hover, focus, clicked, and validation error states) in control templates (Buttons, TextBoxes, PasswordBoxes, ComboBoxes, CheckBoxes, RadioButtons) using optimized WPF `VisualStateManager` or short storyboards.

### R2. Comprehensive Window & Dialog Refactoring (All 32+ Views)
- **Phase 1: Core Windows**: Ensure complete polish and dynamic theme compliance for:
  - `LoginWindow.xaml`
  - `MainWindow.xaml`
  - `DashboardWindow.xaml`
  - `SettingsWindow.xaml`
  - `ProductsWindow.xaml`
  - `SaleDialog.xaml`
- **Phase 2: Auxiliary Dialogs & Windows**: Systematically refactor the remaining 31 windows and dialogs (e.g. `BarcodeScannerWindow`, `CustomersWindow`, `SuppliersWindow`, `InventoryWindow`, `PurchasesWindow`, `ReportsWindow`, `ShiftManagementWindow`, and all related Dialogs).
- **Styling Rules**:
  - Remove all inline color overrides (e.g. `Background="Red"`, `Foreground="#FFF"`, etc.) and bind them to centralized semantic brushes (e.g. `{DynamicResource PrimaryBrush}`, `{DynamicResource DangerBrush}`, etc.).
  - Remove inline `FontFamily` overrides and use the global `{StaticResource PrimaryFont}` or `{StaticResource IconFont}`.
  - Standardize all text headings using centralized text styles: `HeaderLabel`, `SubHeaderLabel`, `BodyLabel`, and `LabelStyle`.
  - Maintain Right-to-Left (RTL) Arabic alignment integrity for all layout containers.

## Verification Plan

### Automated Verification
1. **Compilation Check**:
   - Run: `dotnet build GroceryStoreManagement.sln -c Debug --no-restore`
   - Must compile cleanly with 0 errors.
2. **UI Smoke Test suite**:
   - Run the automated smoke test script to verify navigations and XAML bindings:
     ```powershell
     powershell -ExecutionPolicy Bypass -File scripts/ui-smoke-test.ps1 -ExePath "GroceryStoreManagement/bin/Debug/net10.0-windows7.0/GroceryStoreManagement.exe"
     ```
   - All tests in `artifacts/ui-smoke-test-results.json` must report `PASS`.

## Acceptance Criteria

### Compilation & Runtime Stability
- [ ] Solution compiles cleanly using dotnet build with 0 compilation errors.
- [ ] No XAML parsing exceptions or dynamic resource resolution errors occur at runtime.

### UI & Spacing Consistency
- [ ] All 32+ screens and dialogs use centralized layout tokens for margins, paddings, and corner radii.
- [ ] No hardcoded colors or local fontFamily settings remain in any XAML file.
- [ ] Dynamic Light/Dark theme switching successfully swaps all colors on the fly.
- [ ] All hover/focus states have smooth transition effects.
- [ ] The app maintains perfect Right-to-Left (RTL) layout alignment in all dialogs.

## Follow-up — 2026-06-09T10:17:24+03:00

A complete refactoring and modern redesign of the Grocery Store Management application's user interface using a unified, centralized WPF Design System. The redesign covers all windows and dialogs, supporting dynamic Light and Dark theme switching at runtime, establishing consistent spacing, sizing, borders, and input fields, and implementing high-performance smooth transition animations.

Working directory: d:\D\SAM\3\نظام متكامل لادارة المتجر
Integrity mode: development

## Requirements

### R1. Dynamic Theme, Spacing, and Motion Design System
- **Theme Dictionary Partitioning**: Split colors and brushes into two separate theme dictionaries: `LightTheme.xaml` and `DarkTheme.xaml` under a new folder `GroceryStoreManagement/Styles/Themes/`.
- **Dynamic Styling**: Modify `Styles.xaml` to merge the active theme dictionary dynamically. Reference all theme-dependent colors, brushes, and shadow resources using `{DynamicResource KeyName}` instead of `StaticResource`, allowing instant runtime theme swapping without restarting the application.
- **Unified Layout Tokens**: Ensure all spacing (margins, padding, border thicknesses) and corner radii are bound to the centralized layout tokens (e.g. `CornerRadiusControlNormal`, `PaddingControlNormal`, `MarginStackNormal`).
- **High-Performance Micro-Animations**: Implement smooth visual transitions (for hover, focus, clicked, and validation error states) in control templates (Buttons, TextBoxes, PasswordBoxes, ComboBoxes, CheckBoxes, RadioButtons) using optimized WPF `VisualStateManager` or short storyboards.

### R2. Comprehensive Window & Dialog Refactoring (All 32+ Views)
- **Phase 1: Core Windows**: Ensure complete polish and dynamic theme compliance for:
  - `LoginWindow.xaml`
  - `MainWindow.xaml`
  - `DashboardWindow.xaml`
  - `SettingsWindow.xaml`
  - `ProductsWindow.xaml`
  - `SaleDialog.xaml`
- **Phase 2: Auxiliary Dialogs & Windows**: Systematically refactor the remaining 31 windows and dialogs (e.g. `BarcodeScannerWindow`, `CustomersWindow`, `SuppliersWindow`, `InventoryWindow`, `PurchasesWindow`, `ReportsWindow`, `ShiftManagementWindow`, and all related Dialogs).
- **Styling Rules**:
  - Remove all inline color overrides (e.g. `Background="Red"`, `Foreground="#FFF"`, etc.) and bind them to centralized semantic brushes (e.g. `{DynamicResource PrimaryBrush}`, `{DynamicResource DangerBrush}`, etc.).
  - Remove inline `FontFamily` overrides and use the global `{StaticResource PrimaryFont}` or `{StaticResource IconFont}`.
  - Standardize all text headings using centralized text styles: `HeaderLabel`, `SubHeaderLabel`, `BodyLabel`, and `LabelStyle`.
  - Maintain Right-to-Left (RTL) Arabic alignment integrity for all layout containers.

## Verification Plan

### Automated Verification
1. **Compilation Check**:
   - Run: `dotnet build GroceryStoreManagement.sln -c Debug --no-restore`
   - Must compile cleanly with 0 errors.
2. **UI Smoke Test suite**:
   - Run the automated smoke test script to verify navigations and XAML bindings:
     ```powershell
     powershell -ExecutionPolicy Bypass -File scripts/ui-smoke-test.ps1 -ExePath "GroceryStoreManagement/bin/Debug/net10.0-windows7.0/GroceryStoreManagement.exe"
     ```
   - All tests in `artifacts/ui-smoke-test-results.json` must report `PASS`.

## Acceptance Criteria

### Compilation & Runtime Stability
- [ ] Solution compiles cleanly using dotnet build with 0 compilation errors.
- [ ] No XAML parsing exceptions or dynamic resource resolution errors occur at runtime.

### UI & Spacing Consistency
- [ ] All 32+ screens and dialogs use centralized layout tokens for margins, paddings, and corner radii.
- [ ] No hardcoded colors or local fontFamily settings remain in any XAML file.
- [ ] Dynamic Light/Dark theme switching successfully swaps all colors on the fly.
- [ ] All hover/focus states have smooth transition effects.
- [ ] The app maintains perfect Right-to-Left (RTL) layout alignment in all dialogs.

## Follow-up — 2026-06-10T22:15:00+03:00

إعادة تصميم شاشة تسجيل الدخول (LoginWindow) بالكامل وإنشاء نظام التصميم الموحد والسمات (Themes) الديناميكية بمفهوم تصميم ويندوز 11 الحديث والأنيق (Fluent UI) مع الحفاظ على التوافق التام للكتابة والاتجاه من اليمين إلى اليسار (RTL) للغة العربية.

مجلد العمل: d:\D\SAM\3\نظام متكامل لادارة المتجر
وضع النزاهة (Integrity Mode): development

## المتطلبات

### R1. نظام التصميم الموحد والسمات الديناميكية (Design System & Themes)
- **ملفات السمات**: فصل الألوان والفرش إلى ملفين مستقلين: `LightTheme.xaml` و `DarkTheme.xaml` تحت المجلد `GroceryStoreManagement/Styles/Themes/`.
- **الموارد الديناميكية**: استخدام `{DynamicResource}` بدلاً من `{StaticResource}` لجميع الألوان والفرش للسماح بالتبديل الفوري بين المظهر الفاتح والداكن أثناء التشغيل دون إعادة تشغيل التطبيق.
- **قيم التصميم الثابتة (Tokens)**: ربط الحواف (CornerRadius) والتباعد (Padding) والهوامش (Margin) برموز موحدة داخل `Styles.xaml` (مثل `CornerRadiusControlNormal = 6` للحواف الحادة والأنيقة).

### R2. قوالب عناصر التحكم والتأثيرات الحركية (Control Templates & Animations)
- **حقول الإدخال**: تحديث قالب `TextBox` و `PasswordBox` بحدود ناعمة (سماكة 1px وشفافية 10%) وتأثير توهج وانتقال سلس للألوان عند التركيز (IsFocused).
- **أزرار الأكشن**: إضافة تأثيرات حركية فيزيائية للأزرار (تقليص الحجم إلى 0.98 عند الضغط IsPressed) للحصول على تجربة استخدام متميزة.
- **شريط العنوان المخصص**: إزالة إطار النافذة الافتراضي واستبداله بشريط عنوان مخصص يتضمن زر الإغلاق بخلفية حمراء عند تمرير الفأرة، وتخفيت أزرار التحكم عند فقدان التركيز (IsActive = False).

### R3. إعادة تصميم نافذة تسجيل الدخول (LoginWindow)
- **أبعاد النافذة**: جعل أبعاد النافذة مدمجة وعصرية (450 × 650) متمركزة في عمود واحد نظيف.
- **إزالة الألوان الثابتة**: إزالة أي كود ألوان ثابت (Hex Codes) من كود C# واستبداله بالوصول الديناميكي للموارد (FindResource).
- **بطاقة الترخيص**: إعادة تصميم بطاقة حالة الترخيص وتناسق ألوانها تلقائياً مع المظهر النشط (فاتح أو داكن).

## معايير القبول (Acceptance Criteria)

### استقرار وبناء النظام
- [ ] يكتمل بناء المشروع (dotnet build) بنجاح وبدون أي أخطاء (0 Errors).
- [ ] لا تظهر أي استثناءات متعلقة بتحليل XAML أو موارد مفقودة عند التشغيل.

### التوافق الجمالي والـ RTL
- [ ] تظهر نافذة تسجيل الدخول بأبعاد مدمجة وأنيقة وتدعم السحب والتحريك.
- [ ] تتغير جميع عناصر الواجهة والألوان فوراً وبسلاسة عند تبديل السمة (Themes).
- [ ] يدعم التصميم المحاذاة الكاملة لليمن إلى اليسار (RTL) دون أي تداخل في النصوص أو الأيقونات.
- [ ] نجاح جميع الاختبارات التلقائية الـ 38 بنسبة 100% (Passed=38, Failed=0).
