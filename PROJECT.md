# Project: Grocery Store Management Redesign

## Architecture
- **Language & Runtime**: C# WPF, targeting .NET 10.0 (Windows x64).
- **Design System**: Centralized Windows 11-style Design System with light/dark slate-based colors, teal accents (`#0F766E`), clear surface elevations, rounded corners, and Segoe UI / Noto Naskh Arabic typography.
- **RTL Arabic Alignment**: Design tokens and layouts optimized for Right-to-Left Arabic presentation.
- **WPF Resources**: Merged resource dictionaries in `App.xaml` providing theme design tokens and reusable control styles.
- **Directory Layout**:
  - `GroceryStoreManagement/` (WPF Application project folder)
    - `App.xaml`, `App.xaml.cs`
    - `Styles/` (Design system resource dictionaries)
      - `Styles.xaml` (or separate dictionary files merged in App.xaml)
    - `Windows/` (WPF screens and dialogs)
      - LoginWindow.xaml, LoginWindow.xaml.cs
      - MainWindow.xaml, MainWindow.xaml.cs
      - DashboardWindow.xaml, DashboardWindow.xaml.cs
      - SettingsWindow.xaml, SettingsWindow.xaml.cs
      - ProductsWindow.xaml, ProductsWindow.xaml.cs
      - SaleDialog.xaml, SaleDialog.xaml.cs
      - (Other 31 auxiliary windows and dialogs)
  - `artifacts/` (Output build files and test results)
  - `scripts/` (UI smoke test and automation scripts)

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Design Tokens | Define spacing, typography, colors, and elevations in WPF resources merged in `App.xaml`. | None | PLANNED |
| M2 | Reusable Controls | Implement templates for TextBox, PasswordBox, ComboBox, DatePicker, DataGrid, and Button styles. | M1 | PLANNED |
| M3 | Phase 1: Core Windows | Refactor Login, Main, Dashboard, Settings, Products, and Sale windows to use tokens. | M2 | PLANNED |
| M4 | Phase 2: Auxiliary Dialogs | Refactor the remaining 31 dialogs and list windows under `Windows/` to use tokens. | M3 | PLANNED |
| M5 | Build & Smoke Verification | Run MSBuild, execute `ui-smoke-test.ps1`, verify results, and resolve warnings/failures. | M4 | PLANNED |

## Interface Contracts
### Resource Keys for Design Tokens
- **Colors & Brushes**:
  - `PrimaryBrush` / `PrimaryColor`: Teal brand color (`#0F766E`)
  - `PrimaryLightBrush` / `PrimaryLightColor`: Teal highlight (`#14B8A6`)
  - `PrimaryDarkBrush` / `PrimaryDarkColor`: Darker teal (`#115E59`)
  - `BackgroundBrush` / `BackgroundColor`: Modern dark/light slate background
  - `SurfaceBrush` / `SurfaceColor`: Panel/Card background
  - `BorderBrush` / `BorderColor`: Subtle borders
  - `TextPrimaryBrush`, `TextSecondaryBrush`, `TextMutedBrush`: Standard text hierarchy
  - `SuccessBrush`, `WarningBrush`, `ErrorBrush`: Action states
- **Typography**:
  - `PrimaryFont`: System font family (`./Assets/Fonts/#Noto Naskh Arabic` or fallback system fonts)
  - `HeaderLabel` (26px, Bold), `SubHeaderLabel` (18px, SemiBold), `BodyLabel` (13px, Regular), `CaptionLabel` (11px, Muted)
- **Spacing & Corners**:
  - `CardCornerRadius` (CornerRadius `18`), `ControlCornerRadius` (CornerRadius `12`)
  - `CardShadow`, `CardShadowHover`, `ButtonShadow`, `InputFocusGlow`

### Reusable Style Keys
- **Inputs**: TextBoxes, PasswordBoxes, ComboBoxes, and DatePickers (default TargetTypes and specific keys).
- **Buttons**: `PrimaryButton`, `SecondaryButton`, `OutlineButton`, `DangerButton`, `IconButton`, `TextButton`, `SidebarButton`.
- **DataGrid**: `ModernDataGridStyle` (alternating rows, RTL header alignment, virtualization).
