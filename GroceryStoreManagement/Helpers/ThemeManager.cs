using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GroceryStoreManagement.Helpers
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public static class ThemeManager
    {
        private const string ThemeKey = "Theme";
        private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "settings.ini");
        
        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int attrSize);

        // Windows 11 DWM backdrop constants
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38; // Windows 11 22H2+
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // Immersive Dark Mode attribute

        public static void LoadTheme()
        {
            try
            {
                AppTheme targetTheme = AppTheme.Light;

                if (File.Exists(SettingsPath))
                {
                    var lines = File.ReadAllLines(SettingsPath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2 && parts[0].Trim() == ThemeKey)
                        {
                            if (Enum.TryParse<AppTheme>(parts[1].Trim(), out var parsedTheme))
                            {
                                targetTheme = parsedTheme;
                            }
                            break;
                        }
                    }
                }

                SetTheme(targetTheme);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load theme settings");
            }
        }

        public static void SetTheme(AppTheme theme)
        {
            try
            {
                var appResources = Application.Current.Resources;
                var mergedDicts = appResources.MergedDictionaries;

                // Create URIs for themes
                var lightUri = new Uri("pack://application:,,,/GroceryStoreManagement;component/Styles/Themes/LightTheme.xaml", UriKind.Absolute);
                var darkUri = new Uri("pack://application:,,,/GroceryStoreManagement;component/Styles/Themes/DarkTheme.xaml", UriKind.Absolute);
                var targetUri = theme == AppTheme.Dark ? darkUri : lightUri;

                // Find existing theme dictionary
                var existingThemeDict = mergedDicts.FirstOrDefault(d => 
                    d.Source != null && 
                    (d.Source.OriginalString.Contains("LightTheme.xaml", StringComparison.OrdinalIgnoreCase) || 
                     d.Source.OriginalString.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase)));

                if (existingThemeDict != null)
                {
                    // Update source to trigger dynamic resource updates
                    existingThemeDict.Source = targetUri;
                }
                else
                {
                    // Add new resource dictionary if none exists
                    mergedDicts.Add(new ResourceDictionary { Source = targetUri });
                }

                CurrentTheme = theme;

                // Save theme setting to file
                SaveThemeSetting(theme);

                // Apply immersive dark mode/Mica to all open windows
                foreach (Window window in Application.Current.Windows)
                {
                    ApplyWindowBackdrop(window);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to apply theme dictionary");
            }
        }

        public static void ToggleTheme()
        {
            SetTheme(CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
        }

        public static void ApplyWindowBackdrop(Window window)
        {
            if (window == null) return;

            // Make sure the window is loaded to get its handle
            if (!window.IsLoaded)
            {
                window.Loaded += (s, e) => ApplyWindowBackdrop(window);
                return;
            }

            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                // 1. Enable Immersive Dark Mode if Dark Theme is active
                int useDarkValue = CurrentTheme == AppTheme.Dark ? 1 : 0;
                _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkValue, sizeof(int));

                // 2. Enable Windows 11 Mica backdrop progressive effect
                // Value 2 = Mica, Value 3 = Acrylic, Value 4 = Tabbed (Mica Alt)
                int backdropValue = 2; // Can use Mica (2) for both
                int result = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropValue, sizeof(int));

                // If on Windows 11 (build >= 22000), clear/transparentize window background
                // so the DWM Mica backdrop can show through.
                if (result == 0 && Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000)
                {
                    window.Background = System.Windows.Media.Brushes.Transparent;
                }
            }
            catch
            {
                // Ignore DWM failures on unsupported Windows versions (Win 7, 8, 10 build < 22000)
            }
        }

        private static void SaveThemeSetting(AppTheme theme)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    _ = Directory.CreateDirectory(directory);
                }

                if (File.Exists(SettingsPath))
                {
                    var lines = File.ReadAllLines(SettingsPath).ToList();
                    bool keyFound = false;

                    for (int i = 0; i < lines.Count; i++)
                    {
                        var parts = lines[i].Split('=');
                        if (parts.Length > 0 && parts[0].Trim() == ThemeKey)
                        {
                            lines[i] = $"{ThemeKey}={theme}";
                            keyFound = true;
                            break;
                        }
                    }

                    if (!keyFound)
                    {
                        lines.Add($"{ThemeKey}={theme}");
                    }

                    File.WriteAllLines(SettingsPath, lines);
                }
                else
                {
                    File.WriteAllText(SettingsPath, $"{ThemeKey}={theme}{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save theme setting to settings.ini");
            }
        }
    }
}
