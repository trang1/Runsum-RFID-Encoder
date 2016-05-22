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

            Task.Factory.StartNew(() =>
            {
                var ports = ComPortHelper.GetCOMPortsInfo();
                App.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ComPorts = ports;
                    OnPropertyChanged("ComPorts");
                }));
            });
        }

        #endregion


        #region public members

        public static MainWindowViewModel Instance
        {
            get { return _mainWindowViewModel; }
        }

        public IList<ComPortInfo> ComPorts
        {
            get; set;
        }

        public ComPortInfo SelectedComPort { get; set; }
        public IList<string> Regions { get; set; }

        public string SelectedRegion { get; set; }
        public IList<string> BaudRates {
            get
            {
                return new List<string>
                {
                    "Select","9600", "19200", "38400", "115200", "230400", "460800","921600"
                };
            }}

        public string SelectedBaudRate { get; set; }
        public ICommand ConnectCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public bool IsConnected { get; set; }


        #endregion
    }
}
