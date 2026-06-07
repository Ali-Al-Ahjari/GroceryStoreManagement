using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Linq;
using System.Windows;

namespace GroceryStoreManagement.Windows
{
    public partial class SupplierDialog : Window
    {
        private readonly Supplier _supplier;
        private readonly bool _isEditMode;

        public SupplierDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            InitializeDialog();
        }

        public SupplierDialog(Supplier supplier)
        {
            InitializeComponent();
            _supplier = supplier;
            _isEditMode = true;
            InitializeDialog();
            LoadSupplierData();
        }

        private void InitializeDialog()
        {
            Title = _isEditMode ? "تعديل مورد" : "إضافة مورد جديد";
            
            // تفعيل التنقل بمفتاح Enter
            EnterKeyHelper.EnableEnterKeyNavigation(this);
        }

        private void LoadSupplierData()
        {
            if (_supplier != null)
            {
                NameTextBox.Text = _supplier.Name;
                PhoneTextBox.Text = _supplier.Phone;
                EmailTextBox.Text = _supplier.Email;
                AddressTextBox.Text = _supplier.Address;

                // عرض معلومات التدقيق
                ShowAuditInfo();
            }
        }

        private void ShowAuditInfo()
        {
            if (_isEditMode && _supplier != null)
            {
                string info = AuditHelper.GetAuditInfo(
                    _supplier.CreatedDate,
                    AuditHelper.GetUserName(_supplier.CreatedBy),
                    _supplier.ModifiedDate,
                    AuditHelper.GetUserName(_supplier.ModifiedBy));

                if (!string.IsNullOrEmpty(info))
                {
                    AuditInfoText.Text = info;
                    AuditInfoBorder.Visibility = Visibility.Visible;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من صحة البيانات
                if (!ValidateData())
                    return;

                // إنشاء أو تحديث المورد
                Supplier supplier = _isEditMode ? _supplier : new Supplier();

                supplier.Name = NameTextBox.Text.Trim();
                supplier.Phone = Validator.CleanPhoneNumber(PhoneTextBox.Text);
                supplier.Email = EmailTextBox.Text.Trim();
                supplier.Address = AddressTextBox.Text.Trim();

                // حفظ المورد
                if (_isEditMode)
                {
                    _ = SupplierDAL.UpdateSupplier(supplier);
                }
                else
                {
                    _ = SupplierDAL.AddSupplier(supplier);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حفظ المورد: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateData()
        {

            // التحقق من الاسم
            if (!Validator.ValidateName(NameTextBox.Text, out string errorMessage))
            {
                _ = MessageBox.Show(errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = NameTextBox.Focus();
                return false;
            }

            // التحقق من الهاتف
            if (!Validator.ValidatePhone(PhoneTextBox.Text, out errorMessage))
            {
                _ = MessageBox.Show(errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = PhoneTextBox.Focus();
                return false;
            }

            // التحقق من البريد الإلكتروني
            if (!Validator.ValidateEmail(EmailTextBox.Text, out errorMessage))
            {
                _ = MessageBox.Show(errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = EmailTextBox.Focus();
                return false;
            }

            // التحقق من العنوان
            if (!Validator.ValidateAddress(AddressTextBox.Text, out errorMessage))
            {
                _ = MessageBox.Show(errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = AddressTextBox.Focus();
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
