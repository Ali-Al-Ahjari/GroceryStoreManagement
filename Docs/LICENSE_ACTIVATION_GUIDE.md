# دليل نظام الترخيص الزمني

هذا النظام يجعل البرنامج يعمل حتى تاريخ انتهاء تحدده أنت، وبعده يتقفل تسجيل الدخول بالكامل.
فك القفل يتم فقط عبر كود تفعيل موقع بمفتاحك الخاص.

## أبسط طريقة للاستخدام اليومي

بعد ضبط المسارات مرة واحدة، لا تحتاج إلا هذا الأمر:

```powershell
.\scripts\issue-activation.ps1 -Days 30
```

السكربت يقرأ بصمة الجهاز من الحافظة تلقائيًا (من نص البصمة أو رسالة الطلب الجاهزة)،
ويُنشئ رسالة رد فيها كود التفعيل ثم ينسخها إلى الحافظة.

إذا أردت التوكن فقط:

```powershell
.\scripts\issue-activation.ps1 -MachineFingerprint "PASTE_FULL_FINGERPRINT" -Days 30 -TokenOnly
```

النتيجة:
- يتم إنشاء كود التفعيل مباشرة.
- يتم تجهيز رسالة رد جاهزة للإرسال للعميل.
- يتم نسخ الرسالة (أو التوكن عند `-TokenOnly`) إلى الحافظة تلقائيًا.
- لا تحتاج نقل المفتاح العام لكل عميل طالما تستخدم نفس المفتاح الافتراضي المضمن داخل التطبيق.

## متطلبات المسار

- مشروع أداة الإصدار الخارجي (License Issuer) موجود لديك في مسار خارجي.
- السكربت الداخلي للنشر: `scripts\publish-release.ps1`.

## 1) تعريف مسار أداة التفعيل الخارجية

يمكنك تعريفه بباراميتر مباشر أو متغير بيئة:

```powershell
$env:LICENSE_ISSUER_PATH="C:\Path\To\LicenseIssuer"
```

أو ربط دائم داخل المشروع:

```powershell
Copy-Item .\scripts\license-tool.local.example.ps1 .\scripts\license-tool.local.ps1
# عدّل القيم مرة واحدة ثم استخدم سكربت النشر مباشرة
```

## 2) إنشاء مفاتيح الترخيص (مرة واحدة)

إذا كانت أداة الترخيص الخارجية Python/UI كما في الإعداد الحالي، استخدم السكربت الموحّد داخل المشروع:

```powershell
.\scripts\invoke-license-issuer.ps1 -Action gen-keys -OutputDir C:\license-keys
```

سينتج ملفين:
- `license_private_key.pem` (سري جداً ويبقى عندك فقط)
- `license_public_key.pem` (يوضع مع نسخة التطبيق)

## 3) نشر نسخة التطبيق مع المفتاح العام

من جذر المشروع:

```powershell
.\scripts\publish-release.ps1 `
  -LicenseIssuerPath "C:\Path\To\LicenseIssuer" `
  -LicensePublicKeyPath "C:\license-keys\license_public_key.pem"
```

الناتج سيكون في:

`artifacts\release\publish`

وسيتأكد السكربت من نسخ المفتاح إلى:

`artifacts\release\publish\Data\license_public_key.pem`

إذا كنت تستخدم المفتاح الافتراضي المضمن داخل التطبيق، فهذه الخطوة ليست مطلوبة يدويًا في كل مرة.

وإذا أردت توليد مفاتيح جديدة أثناء النشر مباشرة:

```powershell
.\scripts\publish-release.ps1 `
  -LicenseIssuerPath "C:\Path\To\LicenseIssuer" `
  -GenerateLicenseKeys `
  -LicensePublicKeyPath "" `
  -LicenseKeysOutputDir artifacts\license-keys
```

## 4) أخذ بصمة جهاز المتجر

من شاشة الدخول اضغط زر التفعيل.
ستظهر نافذة فيها زر **نسخ رسالة**.
انسخ الرسالة وأرسلها للمطور كما هي.

## 5) توليد كود تفعيل

مثال (30 يوم):

```powershell
.\scripts\invoke-license-issuer.ps1 -Action gen-token -PrivateKeyPath C:\license-keys\license_private_key.pem -MachineFingerprint "بصمة_الجهاز" -Days 30 -Issuer "StoreOwner"
```

مثال (تاريخ محدد):

```powershell
.\scripts\invoke-license-issuer.ps1 -Action gen-token -PrivateKeyPath C:\license-keys\license_private_key.pem -MachineFingerprint "بصمة_الجهاز" -ExpiresAt 2026-12-31T23:59:59Z -Issuer "StoreOwner"
```

الناتج (Token) يُلصق كما هو في نافذة التفعيل.

للتحقق من التوكن قبل إرساله:

```powershell
.\scripts\invoke-license-issuer.ps1 -Action verify-token -PublicKeyPath C:\license-keys\license_public_key.pem -Token "PASTE_TOKEN" -MachineFingerprint "بصمة_الجهاز"
```

## 6) التفعيل أو التجديد داخل المتجر

- افتح نافذة التفعيل.
- الصق الكود.
- اضغط تفعيل.

إذا انتهت المدة أو تم قفل الترخيص، لن يسمح النظام بتسجيل الدخول حتى إدخال كود جديد صالح.

## ملاحظات أمان مهمة

- لا تشارك `license_private_key.pem` مع أي جهة.
- لا تضع المفتاح الخاص داخل مستودع المشروع.
- الكود مرتبط ببصمة جهاز واحد، ولن يعمل على جهاز مختلف.
