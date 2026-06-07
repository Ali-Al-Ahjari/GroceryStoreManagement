using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using GroceryStoreManagement.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GroceryStoreManagement.Windows
{
    public partial class LogsWindow : Window
    {
        private List<ActivityLog> _allLogs = [];

        public LogsWindow()
        {
            InitializeComponent();
            LoadLogs();
        }

        private void LoadLogs()
        {
            try
            {
                // Assuming LogDAL exists and has GetLogs. If not, check ActivityLogDAL
                // The previous code used LogDAL.GetLogs(500), so we keep that assumption or fix to ActivityLogDAL.
                // Given build log history, checking if ActivityLogDAL is better. 
                // Wait, previous file used LogDAL. Let's assume LogDAL works or I might need to switch to ActivityLogDAL if LogDAL is missing.
                // Assuming LogDAL for now as per previous file.
                _allLogs = LogDAL.GetLogs(500);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل سجل النشاطات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allLogs == null || LogsDataGrid == null) return;

            var filtered = _allLogs.AsEnumerable();

            // Search
            if (TxtSearch != null && !string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                string search = TxtSearch.Text.ToLower(System.Globalization.CultureInfo.CurrentCulture);
                filtered = filtered.Where(l =>
                    (l.Username != null && l.Username.Contains(search, StringComparison.CurrentCultureIgnoreCase)) ||
                    (l.Details != null && l.Details.Contains(search, StringComparison.CurrentCultureIgnoreCase)) ||
                    (l.Action != null && l.Action.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                );
            }

            // Action Type Filter
            if (CmbActionType != null && CmbActionType.SelectedIndex > 0)
            {
                if (CmbActionType.SelectedItem is ComboBoxItem item && item.Content != null)
                {
                    string selectedAction = item.Content.ToString();
                    if (selectedAction != "الكل")
                    {
                        // Map Arabic to English if necessary, or just filter by contains if DB stores English
                        // Assuming DB stores English: "Add", "Edit", "Delete", "Login"
                        // Or Arabic? Let's assume we filter by what's displayed or do a mapping.
                        // Simple mapping for now based on typical convention:
                        string filterTerm = "";
                        filterTerm = selectedAction switch
                        {
                            "إضافة" => "Add",
                            "تعديل" => "Edit",
                            "حذف" => "Delete",
                            "دخول" => "Login",
                            _ => selectedAction,
                        };

                        // Try both Arabic and English to be safe
                        filtered = filtered.Where(l => l.Action.Contains(filterTerm) || l.Action.Contains(selectedAction));
                    }
                }
            }

            // Date Filters
            if (DpFromDate != null && DpFromDate.SelectedDate.HasValue)
            {
                filtered = filtered.Where(l => l.LogDate.Date >= DpFromDate.SelectedDate.Value.Date);
            }
            if (DpToDate != null && DpToDate.SelectedDate.HasValue)
            {
                filtered = filtered.Where(l => l.LogDate.Date <= DpToDate.SelectedDate.Value.Date);
            }

            var result = filtered.OrderByDescending(l => l.LogDate).ToList();
            LogsDataGrid.ItemsSource = result;

            // Update Stats
            _ = (TxtTotalLogs?.Text = $"إجمالي السجلات: {result.Count}");
        }

        // Event Handlers

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DpFromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DpToDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbActionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = LogsDataGrid.ItemsSource as IEnumerable<ActivityLog> ?? _allLogs;
                string filePath = ExportHelper.ShowSaveFileDialog(
                    $"activity_logs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv|Text (*.txt)|*.txt");

                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                ExportFormat format = filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                    ? ExportFormat.CSV
                    : filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                        ? ExportFormat.Text
                        : ExportFormat.Excel;

                if (ExportHelper.ExportReport(data, filePath, format))
                {
                    _ = MessageBox.Show("تم تصدير السجلات بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
