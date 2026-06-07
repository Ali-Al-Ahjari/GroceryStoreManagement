# CHANGELOG - سجل التغييرات

## [2026-01-05] - تنظيف المشروع

### الملفات المحذوفة (17 ملف)

#### من جذر المشروع

- `build_errors.txt` - سجل أخطاء مؤقت
- `build_log.txt` - سجل بناء مؤقت
- `fix_xml_errors.ps1` - سكريبت إصلاح لمرة واحدة
- `update_datagrids.ps1` - سكريبت تحديث لمرة واحدة

#### من مجلد GroceryStoreManagement

- `BuildStatus.txt` - ملف حالة مؤقت
- `GroceryStoreManagement.csproj.bak` - نسخة احتياطية قديمة
- `all_errors.txt` - سجل أخطاء مؤقت
- `build_log.txt` - سجل بناء مؤقت
- `build_output.txt` - مخرجات بناء مؤقتة
- `build_output_utf8.txt` - مخرجات بناء مؤقتة
- `fix_mainwindow.ps1` - سكريبت إصلاح
- `fix_reports.ps1` - سكريبت إصلاح
- `fix_reports_regex.ps1` - سكريبت إصلاح
- `fix_xaml.ps1` - سكريبت إصلاح
- `full_build_output.txt` - مخرجات بناء مؤقتة
- `full_build_output_utf8.txt` - مخرجات بناء مؤقتة
- `msbuild.log` - سجل MSBuild مؤقت

### التغييرات اليدوية (Phase 2)

- تم إزالة الفواصل القديمة (ASCII Art) من `UserDAL.cs` و `DatabaseHelper.cs` لتحسين قراءة الكود.
- تم تجنب النقل الآلي للملفات بناءً على تفضيل المستخدم.

### النسخة الاحتياطية

- تم إنشاء نسخة احتياطية في: `backup_before_cleanup_2026-01-05_04-35.zip`

### ملاحظات

- لم يتم حذف أي ملفات كود أساسية
- الهيكلة الحالية للمشروع منطقية ولم تتغير
- جميع ملفات التوثيق (README, ROADMAP, etc.) تم الإبقاء عليها
