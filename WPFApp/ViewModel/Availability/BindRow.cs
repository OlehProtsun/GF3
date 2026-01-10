using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Availability
{
    internal sealed class BindRow : ObservableObject
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        private string _key = string.Empty;
        public string Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public BindModel ToModel()
        {
            return new BindModel
            {
                Id = Id,
                Key = Key ?? string.Empty,
                Value = Value ?? string.Empty,
                IsActive = IsActive
            };
        }

        public static BindRow FromModel(BindModel model)
        {
            return new BindRow
            {
                Id = model.Id,
                Key = model.Key,
                Value = model.Value,
                IsActive = model.IsActive
            };
        }
    }
}
