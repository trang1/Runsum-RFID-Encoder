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
        public IList<string> Regions { get; set; }

        public string SelectedRegion { get; set; }
        public IList<object> BaudRates {
            get
            {
                return new List<object>
                {
                    new {ID = 0, Title = "Select"},
                    new {ID = 1, Title = "9600"},
                    new {ID = 2, Title = "19200"},
                    new {ID = 3, Title = "38400"},
                    new {ID = 4, Title = "115200"},
                    new {ID = 5, Title = "230400"},
                    new {ID = 6, Title = "460800"},
                    new {ID = 7, Title = "921600"}
                };
            }}

        public string SelectedBaudRate { get; set; }
        public ICommand ConnectCommand { get; set; }

        public bool IsConnected { get; set; }


        #endregion
    }
}
