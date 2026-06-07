param(
    [string]$ExePath = (Join-Path $PSScriptRoot "..\GroceryStoreManagement\bin\Debug\net10.0-windows7.0\GroceryStoreManagement.exe"),
    [string]$Username = "123",
    [string]$Password = "123",
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\artifacts\ui-smoke-test-results.json")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class UiSmokeNative
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
}
"@

$wshell = New-Object -ComObject WScript.Shell
$results = [System.Collections.Generic.List[object]]::new()

function Add-Result {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Detail = ""
    )

    $status = if ($Passed) { "PASS" } else { "FAIL" }
    $results.Add([pscustomobject]@{
            Test   = $Name
            Status = $status
            Detail = $Detail
        }) | Out-Null

    if ([string]::IsNullOrWhiteSpace($Detail)) {
        Write-Output "[$status] $Name"
    }
    else {
        Write-Output "[$status] $Name - $Detail"
    }
}

function Wait-Until {
    param(
        [scriptblock]$Condition,
        [int]$TimeoutSeconds = 15,
        [int]$DelayMilliseconds = 250
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $value = & $Condition
            if ($value) {
                return $value
            }
        }
        catch {
        }

        Start-Sleep -Milliseconds $DelayMilliseconds
    }

    return $null
}

function Get-AppProcess {
    return Get-Process GroceryStoreManagement -ErrorAction SilentlyContinue |
        Sort-Object StartTime -Descending |
        Select-Object -First 1
}

function Stop-App {
    $proc = Get-AppProcess
    if ($null -ne $proc) {
        Stop-Process -Id $proc.Id -Force
        $null = Wait-Until -TimeoutSeconds 10 -Condition { -not (Get-AppProcess) }
    }
}

function Get-AppWindows {
    $proc = Get-AppProcess
    if ($null -eq $proc) {
        return @()
    }

    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $pCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
        $proc.Id
    )
    $tCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Window
    )
    $condition = New-Object System.Windows.Automation.AndCondition($pCond, $tCond)

    $windows = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)
    $items = @()
    for ($i = 0; $i -lt $windows.Count; $i++) {
        $window = $windows.Item($i)
        $title = $window.Current.Name
        Write-Host "Get-AppWindows: Found window title='$title'" -ForegroundColor Cyan
        $items += [pscustomobject]@{
            Element = $window
            Title   = $title
            Handle  = [IntPtr]$window.Current.NativeWindowHandle
        }
    }

    return $items
}

function Get-WindowByTitle {
    param([string]$Title)

    return Get-AppWindows | Where-Object { $_.Title -eq $Title } | Select-Object -First 1
}

function Wait-ForWindow {
    param(
        [string]$Title,
        [int]$TimeoutSeconds = 15
    )

    return Wait-Until -TimeoutSeconds $TimeoutSeconds -Condition {
        Get-WindowByTitle -Title $Title
    }
}

function Get-MainWindowElement {
    $proc = Get-AppProcess
    if ($null -ne $proc -and $proc.MainWindowHandle -ne 0) {
        return [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$proc.MainWindowHandle)
    }

    $main = Get-WindowByTitle -Title "نظام إدارة المتجر"
    if ($null -ne $main) {
        return $main.Element
    }

    return $null
}

function Activate-Handle {
    param([IntPtr]$Handle)

    if ($Handle -eq [IntPtr]::Zero) {
        return
    }

    [UiSmokeNative]::SetForegroundWindow($Handle) | Out-Null
    Start-Sleep -Milliseconds 250
}

function Find-ByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId
    )

    if ($null -eq $Root) {
        return $null
    }

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId
    )

    return $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Find-VisibleByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    if ($null -eq $Root) {
        return $null
    }

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name
    )

    $matches = $Root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)
    for ($i = 0; $i -lt $matches.Count; $i++) {
        $candidate = $matches.Item($i)
        if (-not $candidate.Current.IsOffscreen) {
            return $candidate
        }
    }

    return $null
}

function Invoke-OrClick {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [IntPtr]$OwnerHandle
    )

    if ($null -eq $Element) {
        throw "Element not found."
    }

    try {
        $invoke = $Element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        Start-Sleep -Milliseconds 250
        return
    }
    catch {
        Write-Output "InvokePattern failed, falling back to keyboard focus: $_"
    }

    Activate-Handle -Handle $OwnerHandle

    try {
        $Element.SetFocus()
        Start-Sleep -Milliseconds 150
        $wshell.SendKeys(" ")
        Start-Sleep -Milliseconds 250
        return
    }
    catch {
        Write-Output "SetFocus/Space failed, falling back to mouse event: $_"
    }

    Activate-Handle -Handle $OwnerHandle
    $rect = $Element.Current.BoundingRectangle
    if ($rect.Width -gt 0 -and $rect.Height -gt 0) {
        $x = [int]($rect.Left + ($rect.Width / 2))
        $y = [int]($rect.Top + ($rect.Height / 2))
        [UiSmokeNative]::SetCursorPos($x, $y) | Out-Null
        Start-Sleep -Milliseconds 100
        [UiSmokeNative]::mouse_event([UiSmokeNative]::MOUSEEVENTF_LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 50
        [UiSmokeNative]::mouse_event([UiSmokeNative]::MOUSEEVENTF_LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 250
        return
    }

    throw "Element has no clickable bounds and InvokePattern is unavailable."
}

function Click-ByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId
    )

    $element = Find-ByAutomationId -Root $Window -AutomationId $AutomationId
    Invoke-OrClick -Element $element -OwnerHandle ([IntPtr]$Window.Current.NativeWindowHandle)
}

function Click-ByName {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$Name
    )

    $element = Find-VisibleByName -Root $Window -Name $Name
    Invoke-OrClick -Element $element -OwnerHandle ([IntPtr]$Window.Current.NativeWindowHandle)
}

function Set-TextValue {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId,
        [string]$Value
    )

    $element = Find-ByAutomationId -Root $Window -AutomationId $AutomationId
    if ($null -eq $element) {
        throw "AutomationId '$AutomationId' not found."
    }

    $pattern = $element.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $pattern.SetValue($Value)
}

function Set-PasswordValue {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId,
        [string]$Value
    )

    $element = Find-ByAutomationId -Root $Window -AutomationId $AutomationId
    if ($null -eq $element) {
        throw "Password control '$AutomationId' not found."
    }

    Activate-Handle -Handle ([IntPtr]$Window.Current.NativeWindowHandle)
    Invoke-OrClick -Element $element -OwnerHandle ([IntPtr]$Window.Current.NativeWindowHandle)
    Start-Sleep -Milliseconds 150
    $wshell.SendKeys("^a")
    Start-Sleep -Milliseconds 80
    $wshell.SendKeys("{BACKSPACE}")
    Start-Sleep -Milliseconds 80
    $wshell.SendKeys($Value)
    Start-Sleep -Milliseconds 150
}

function Read-TextByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId
    )

    $element = Find-ByAutomationId -Root $Window -AutomationId $AutomationId
    if ($null -eq $element) {
        return $null
    }

    return $element.Current.Name
}

function Close-WindowElement {
    param([System.Windows.Automation.AutomationElement]$Window)

    if ($null -eq $Window) {
        return
    }

    Activate-Handle -Handle ([IntPtr]$Window.Current.NativeWindowHandle)
    $wshell.SendKeys("%{F4}")
}

function Confirm-DialogByEnter {
    param([System.Windows.Automation.AutomationElement]$Window)

    if ($null -eq $Window) {
        return
    }

    Activate-Handle -Handle ([IntPtr]$Window.Current.NativeWindowHandle)
    Start-Sleep -Milliseconds 150
    $wshell.SendKeys("{ENTER}")
}

function Wait-MainTitle {
    param(
        [string]$Title,
        [int]$TimeoutSeconds = 15
    )

    return Wait-Until -TimeoutSeconds $TimeoutSeconds -Condition { Get-WindowByTitle -Title $Title }
}

function Assert-PageTitle {
    param([string]$Expected)

    $window = Get-MainWindowElement
    $title = Read-TextByAutomationId -Window $window -AutomationId "PageTitle"
    if ($title -ne $Expected) {
        throw "Expected page '$Expected' but found '$title'."
    }

    return $title
}

function Run-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    try {
        $detail = & $Action
        Add-Result -Name $Name -Passed $true -Detail ([string]$detail)
    }
    catch {
        Add-Result -Name $Name -Passed $false -Detail $_.Exception.Message
    }
}

function Open-And-Close-Dialog {
    param(
        [string]$Name,
        [scriptblock]$OpenAction,
        [string]$ExpectedTitle,
        [switch]$ConfirmByEnter
    )

    Run-Step -Name $Name -Action {
        & $OpenAction
        $dialog = Wait-ForWindow -Title $ExpectedTitle -TimeoutSeconds 12
        if ($null -eq $dialog) {
            throw "Window '$ExpectedTitle' did not open."
        }

        if ($ConfirmByEnter) {
            Confirm-DialogByEnter -Window $dialog.Element
        }
        else {
            Close-WindowElement -Window $dialog.Element
        }

        $null = Wait-Until -TimeoutSeconds 10 -Condition { -not (Get-WindowByTitle -Title $ExpectedTitle) }
        return $ExpectedTitle
    }
}

Run-Step -Name "Launch application to login screen" -Action {
    Stop-App
    if (-not (Test-Path $ExePath)) {
        throw "Application not found at '$ExePath'."
    }

    Start-Process -FilePath $ExePath -ArgumentList "--test-login" | Out-Null
    $login = Wait-ForWindow -Title "تسجيل الدخول - نظام إدارة المتجر" -TimeoutSeconds 20
    if ($null -eq $login) {
        throw "Login window did not appear."
    }

    return "Login window loaded"
}

Open-And-Close-Dialog -Name "Login: license activation dialog" -ExpectedTitle "تفعيل الترخيص" -OpenAction {
    $window = Wait-ForWindow -Title "تسجيل الدخول - نظام إدارة المتجر"
    Click-ByAutomationId -Window $window.Element -AutomationId "BtnLicenseActivation"
}

Open-And-Close-Dialog -Name "Login: connection settings dialog" -ExpectedTitle "إعدادات الاتصال" -OpenAction {
    $window = Wait-ForWindow -Title "تسجيل الدخول - نظام إدارة المتجر"
    Click-ByAutomationId -Window $window.Element -AutomationId "BtnSettings"
}

Open-And-Close-Dialog -Name "Login: password recovery dialog" -ExpectedTitle "استعادة كلمة المرور" -OpenAction {
    $window = Wait-ForWindow -Title "تسجيل الدخول - نظام إدارة المتجر"
    Click-ByAutomationId -Window $window.Element -AutomationId "BtnForgotPassword"
}

Run-Step -Name "Login: active license badge visible" -Action {
    $window = Wait-ForWindow -Title "تسجيل الدخول - نظام إدارة المتجر"
    $badge = Read-TextByAutomationId -Window $window.Element -AutomationId "TxtLicenseStateBadge"
    if ($badge -ne "مفعل") {
        throw "Unexpected license badge '$badge'."
    }

    return $badge
}

Run-Step -Name "Login with provided credentials" -Action {
    $window = Wait-ForWindow -Title "تسجيل الدخول - نظام إدارة المتجر"
    Set-TextValue -Window $window.Element -AutomationId "TxtUsername" -Value $Username
    Set-PasswordValue -Window $window.Element -AutomationId "TxtPassword" -Value $Password
    Click-ByAutomationId -Window $window.Element -AutomationId "BtnLogin"

    $main = Wait-ForWindow -Title "نظام إدارة المتجر" -TimeoutSeconds 20
    if ($null -eq $main) {
        throw "Main window did not appear after login."
    }

    return "Logged in as $Username"
}

$pages = @(
    @{ Button = "BtnDashboard"; Title = "لوحة التحكم" },
    @{ Button = "BtnProducts"; Title = "المنتجات" },
    @{ Button = "BtnInventory"; Title = "المخزون" },
    @{ Button = "BtnSuppliers"; Title = "الموردون" },
    @{ Button = "BtnInvoices"; Title = "المبيعات" },
    @{ Button = "BtnCustomers"; Title = "العملاء" },
    @{ Button = "BtnPurchases"; Title = "المشتريات" },
    @{ Button = "BtnReports"; Title = "التقارير" },
    @{ Button = "BtnSettings"; Title = "الإعدادات" }
)

foreach ($page in $pages) {
    Run-Step -Name "Navigate to $($page.Title)" -Action {
        $main = Get-MainWindowElement
        Click-ByAutomationId -Window $main -AutomationId $page.Button
        Start-Sleep -Milliseconds 700
        return Assert-PageTitle -Expected $page.Title
    }
}

Run-Step -Name "Dashboard: global refresh button" -Action {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnDashboard"
    Start-Sleep -Milliseconds 400
    Click-ByAutomationId -Window $main -AutomationId "BtnGlobalRefresh"
    Start-Sleep -Seconds 1
    return Assert-PageTitle -Expected "لوحة التحكم"
}

Open-And-Close-Dialog -Name "Notifications: full window opens" -ExpectedTitle "الإشعارات" -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnNotifications"
    Start-Sleep -Milliseconds 400
    Click-ByName -Window $main -Name "عرض الكل"
}

Open-And-Close-Dialog -Name "User menu: my account dialog" -ExpectedTitle "حسابي" -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnUserMenu"
    Start-Sleep -Milliseconds 400
    Click-ByName -Window $main -Name "حسابي"
}

$settingsSections = @(
    @{ RadioId = "SystemSettingsRadio"; Heading = "إعدادات النظام والمؤسسة" },
    @{ RadioId = "PrintSettingsRadio"; Heading = "إعدادات الطباعة والفواتير" },
    @{ RadioId = "EmailSettingsRadio"; Heading = "إعدادات البريد الإلكتروني (SMTP)" },
    @{ RadioId = "UsersSettingsRadio"; Heading = "إدارة المستخدمين" },
    @{ RadioId = "RolesSettingsRadio"; Heading = "إدارة الأدوار والصلاحيات" },
    @{ RadioId = "BackupSettingsRadio"; Heading = "النسخ الاحتياطي والاستعادة" },
    @{ RadioId = "DatabaseSettingsRadio"; Heading = "معلومات وصيانة قاعدة البيانات" }
)

foreach ($section in $settingsSections) {
    Run-Step -Name "Settings section: $($section.Heading)" -Action {
        $main = Get-MainWindowElement
        Click-ByAutomationId -Window $main -AutomationId "BtnSettings"
        Start-Sleep -Milliseconds 400
        Click-ByAutomationId -Window $main -AutomationId $section.RadioId
        Start-Sleep -Milliseconds 500
        $heading = Find-VisibleByName -Root $main -Name $section.Heading
        if ($null -eq $heading) {
            throw "Heading '$($section.Heading)' not visible."
        }

        return $section.Heading
    }
}

Open-And-Close-Dialog -Name "Settings: logs window" -ExpectedTitle "سجل النشاطات" -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnSettings"
    Start-Sleep -Milliseconds 400
    Click-ByAutomationId -Window $main -AutomationId "LogsSettingsButton"
}

Open-And-Close-Dialog -Name "Settings: error logs window" -ExpectedTitle "سجل الأخطاء" -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnSettings"
    Start-Sleep -Milliseconds 400
    Click-ByAutomationId -Window $main -AutomationId "ErrorLogsSettingsButton"
}

Open-And-Close-Dialog -Name "Settings: add user dialog" -ExpectedTitle "إضافة/تعديل مستخدم" -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnSettings"
    Start-Sleep -Milliseconds 400
    Click-ByAutomationId -Window $main -AutomationId "UsersSettingsRadio"
    Start-Sleep -Milliseconds 400
    Click-ByName -Window $main -Name "إضافة مستخدم"
}

Open-And-Close-Dialog -Name "Settings: add role dialog" -ExpectedTitle "إدارة الأدوار" -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnSettings"
    Start-Sleep -Milliseconds 400
    Click-ByAutomationId -Window $main -AutomationId "RolesSettingsRadio"
    Start-Sleep -Milliseconds 400
    Click-ByName -Window $main -Name "إضافة دور"
}

Open-And-Close-Dialog -Name "Products: add product dialog" -ExpectedTitle "إضافة/تعديل منتج" -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnProducts"
    Start-Sleep -Milliseconds 500
    Click-ByName -Window $main -Name "إضافة منتج"
}

Open-And-Close-Dialog -Name "Customers: add customer dialog" -ExpectedTitle "إضافة/تعديل عميل" -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnCustomers"
    Start-Sleep -Milliseconds 500
    Click-ByName -Window $main -Name "إضافة عميل"
}

Open-And-Close-Dialog -Name "Suppliers: add supplier dialog" -ExpectedTitle "إضافة/تعديل مورد" -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnSuppliers"
    Start-Sleep -Milliseconds 500
    Click-ByName -Window $main -Name "إضافة مورد"
}

Open-And-Close-Dialog -Name "Purchases: new purchase dialog" -ExpectedTitle "فاتورة شراء" -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnPurchases"
    Start-Sleep -Milliseconds 500
    Click-ByName -Window $main -Name "فاتورة شراء جديدة"
}

Open-And-Close-Dialog -Name "Sales: shift management dialog" -ExpectedTitle "إدارة الوردية" -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnInvoices"
    Start-Sleep -Milliseconds 500
    Click-ByAutomationId -Window $main -AutomationId "StartShiftButton"
}

Open-And-Close-Dialog -Name "Sales: no-shift warning before invoice" -ExpectedTitle "الوردية غير مفتوحة" -ConfirmByEnter -OpenAction {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnInvoices"
    Start-Sleep -Milliseconds 500
    Click-ByAutomationId -Window $main -AutomationId "NewInvoiceButton"
}

Run-Step -Name "Reports: generate default report" -Action {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnReports"
    Start-Sleep -Milliseconds 500
    Click-ByAutomationId -Window $main -AutomationId "BtnGenerateReport"
    Start-Sleep -Seconds 2
    return Assert-PageTitle -Expected "التقارير"
}

Run-Step -Name "Logout returns to login screen" -Action {
    $main = Get-MainWindowElement
    Click-ByAutomationId -Window $main -AutomationId "BtnUserMenu"
    Start-Sleep -Milliseconds 400
    Click-ByName -Window $main -Name "تسجيل الخروج"

    $confirm = Wait-ForWindow -Title "تسجيل الخروج" -TimeoutSeconds 10
    if ($null -eq $confirm) {
        throw "Logout confirmation dialog did not open."
    }

    Confirm-DialogByEnter -Window $confirm.Element
    $login = Wait-ForWindow -Title "تسجيل الدخول - نظام إدارة المتجر" -TimeoutSeconds 15
    if ($null -eq $login) {
        throw "Login screen did not return after logout."
    }

    return "Returned to login screen"
}

Run-Step -Name "Re-login after logout" -Action {
    $window = Wait-ForWindow -Title "تسجيل الدخول - نظام إدارة المتجر"
    Set-TextValue -Window $window.Element -AutomationId "TxtUsername" -Value $Username
    Set-PasswordValue -Window $window.Element -AutomationId "TxtPassword" -Value $Password
    Click-ByAutomationId -Window $window.Element -AutomationId "BtnLogin"

    $main = Wait-ForWindow -Title "نظام إدارة المتجر" -TimeoutSeconds 20
    if ($null -eq $main) {
        throw "Failed to return to main window after logout."
    }

    return "Logged in again"
}

$outputDirectory = Split-Path -Path $OutputPath -Parent
if (-not (Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$results | ConvertTo-Json -Depth 3 | Set-Content -Path $OutputPath -Encoding UTF8

$passed = ($results | Where-Object Status -eq "PASS").Count
$failed = ($results | Where-Object Status -eq "FAIL").Count
Write-Output "Summary: Passed=$passed Failed=$failed"
Write-Output "Results saved to $OutputPath"
