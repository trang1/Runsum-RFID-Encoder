using System.ComponentModel;
using System.Windows;

namespace RfidEncoder.ViewModels
{
    /// <summary>
    /// Standard base viewModel for all viewModels using in the project
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public FrameworkElement FrameworkElement { get; set; }
    }
}
