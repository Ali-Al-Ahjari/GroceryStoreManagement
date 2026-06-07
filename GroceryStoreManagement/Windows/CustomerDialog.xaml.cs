using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Windows;

namespace GroceryStoreManagement.Windows
{
    public partial class CustomerDialog : Window
    {
        private readonly Customer _customer;
        private readonly bool _isEditMode;
        public CustomerDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            InitializeDialog();
        }

        public CustomerDialog(Customer customer)
        {
            InitializeComponent();
            _customer = customer;
            _isEditMode = true;
            InitializeDialog();
            LoadCustomerData();
        }

        private void InitializeDialog()
        {
            DialogTitle.Text = _isEditMode ? "👤 تعديل بيانات العميل" : "👤 إضافة عميل جديد";
            
            // تفعيل التنقل بمفتاح Enter
            EnterKeyHelper.EnableEnterKeyNavigation(this);
        }

        private void LoadCustomerData()
        {
            if (_customer != null)
            {
                TxtName.Text = _customer.Name;
                TxtPhone.Text = _customer.Phone;
                TxtEmail.Text = _customer.Email;
                TxtAddress.Text = _customer.Address;
                TxtCreditLimit.Text = _customer.CreditLimit.ToString("F2");
                ChkIsActive.IsChecked = _customer.IsActive;
                TxtNotes.Text = _customer.Notes;
            }
        }

        private bool ValidateData()
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                _ = MessageBox.Show("يرجى إدخال اسم العميل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                _ = TxtName.Focus();
                return false;
            }
            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateData()) return;

                bool isNew = !_isEditMode;
                Customer customer = isNew ? new() : _customer;

                customer.Name = TxtName.Text.Trim();
                customer.Phone = TxtPhone.Text.Trim();
                customer.Email = TxtEmail.Text.Trim();
                customer.Address = TxtAddress.Text.Trim();
                customer.IsActive = ChkIsActive.IsChecked ?? true;
                if (decimal.TryParse(TxtCreditLimit.Text, out decimal creditLimit))
                    customer.CreditLimit = creditLimit;
                else
                    customer.CreditLimit = 0;
                customer.Notes = TxtNotes.Text.Trim();

                bool success;
                if (isNew)
                {
                    success = CustomerDAL.AddCustomer(customer) > 0;
                }
                else
                {
                    success = CustomerDAL.UpdateCustomer(customer);
                }

                if (success)
                {
                    _ = MessageBox.Show("تم حفظ بيانات العميل بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    _ = MessageBox.Show("فشل حفظ بيانات العميل", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}