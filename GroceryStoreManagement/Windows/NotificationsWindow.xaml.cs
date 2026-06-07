using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GroceryStoreManagement.Windows
{
    public partial class NotificationsWindow : Window
    {
        private List<Notification> _notifications = [];

        public NotificationsWindow()
        {
            InitializeComponent();
            LoadNotifications();
        }

        private void LoadNotifications()
        {
            try
            {
                _notifications = NotificationDAL.GetAllNotifications(100);

                _ = (NotificationsList?.ItemsSource = _notifications);

                UpdateUI();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل الإشعارات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            _ = (TxtCount?.Text = $"{_notifications.Count} إشعار");

            _ = (EmptyMessage?.Visibility = _notifications.Count != 0 ? Visibility.Collapsed : Visibility.Visible);
        }

        private void BtnMarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NotificationDAL.MarkAllAsRead();
                LoadNotifications();
                _ = MessageBox.Show("تم تحديد جميع الإشعارات كمقروءة", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ");
            }
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("هل أنت متأكد من حذف جميع الإشعارات؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var note in _notifications.ToList())
                    {
                        NotificationDAL.DeleteNotification(note.NotificationID);
                    }
                    LoadNotifications();
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show($"خطأ في حذف الإشعارات: {ex.Message}", "خطأ");
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                try
                {
                    NotificationDAL.DeleteNotification(id);
                    LoadNotifications();
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show($"خطأ في حذف الإشعار: {ex.Message}", "خطأ");
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
