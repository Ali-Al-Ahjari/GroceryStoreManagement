using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GroceryStoreManagement.Helpers
{
    /// <summary>
    /// فئة مساعدة لتمكين التنقل بين عناصر الإدخال باستخدام مفتاح Enter
    /// </summary>
    public static class EnterKeyHelper
    {
        /// <summary>
        /// خاصية مرفقة لتمكين التنقل بمفتاح Enter
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(EnterKeyHelper),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.PreviewKeyDown += Element_PreviewKeyDown;
                }
                else
                {
                    element.PreviewKeyDown -= Element_PreviewKeyDown;
                }
            }
        }

        private static void Element_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.OriginalSource is UIElement element)
            {
                // الانتقال للعنصر التالي
                // لا تنتقل إذا كان العنصر زر أو TextBox متعدد الأسطر
                if (element is Button)
                {
                    return; // اترك الزر يعمل بشكل طبيعي
                }

                if (element is TextBox textBox && textBox.AcceptsReturn)
                {
                    return; // اترك TextBox متعدد الأسطر يعمل بشكل طبيعي
                }

                // الانتقال للعنصر التالي
                element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
            }
        }

        /// <summary>
        /// تطبيق التنقل بـ Enter على نافذة أو حاوية
        /// </summary>
        public static void EnableEnterKeyNavigation(UIElement container)
        {
            SetIsEnabled(container, true);
        }

        /// <summary>
        /// إلغاء التنقل بـ Enter من نافذة أو حاوية
        /// </summary>
        public static void DisableEnterKeyNavigation(UIElement container)
        {
            SetIsEnabled(container, false);
        }
    }
}
