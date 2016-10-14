using System.Windows;
using System.Windows.Input;

namespace RfidEncoder.ViewModels
{
    /// <summary>
    ///     Represents the viewModel for MainWindow
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        #region private members

        private static readonly MainWindowViewModel _mainWindowViewModel = new MainWindowViewModel();

        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            ExitCommand = new DelegateCommand(() => Application.Current.Shutdown());

            RacesViewModel = new RacesViewModel();
        }

        #endregion

        #region public members

        public static MainWindowViewModel Instance
        {
            get { return _mainWindowViewModel; }
        }

        public RacesViewModel RacesViewModel { get; set; }

        public ICommand ExitCommand { get; set; }

        #endregion
    }
}