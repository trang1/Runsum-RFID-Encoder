using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private void Connect()
        {

        }

        #endregion

        #region Constructor
        public MainWindowViewModel()
        {
            ConnectCommand = new DelegateCommand(Connect);
        }

        #endregion


        #region public members

        public static MainWindowViewModel Instance
        {
            get { return _mainWindowViewModel; }
        }

        public IList<ComPortInfo> ComPorts
        {
            get
            {
                return ComPortHelper.GetCOMPortsInfo();
            }
        }

        public ComPortInfo SelectedComPort { get; set; }

        public ICommand ConnectCommand { get; set; }

        #endregion
    }
}
