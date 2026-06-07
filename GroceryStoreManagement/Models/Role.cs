using System.ComponentModel;

namespace GroceryStoreManagement.Models
{
    public class Role : INotifyPropertyChanged
    {
        private int _roleId;
        private string _roleName;
        private string _description;
        private bool _isSystemRole;

        public int RoleID
        {
            get => _roleId;
            set { _roleId = value; OnPropertyChanged(nameof(RoleID)); }
        }

        public string RoleName
        {
            get => _roleName;
            set { _roleName = value; OnPropertyChanged(nameof(RoleName)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public bool IsSystemRole
        {
            get => _isSystemRole;
            set { _isSystemRole = value; OnPropertyChanged(nameof(IsSystemRole)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
