

using CommunityToolkit.Mvvm.ComponentModel;

namespace Electric_Meter.MVVM.ViewModels
{
    public class MenuItemViewModel : ObservableObject
    {

        private string _displayName;
        public string DisplayName
        {
            get { return _displayName; }
            set
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }
}
