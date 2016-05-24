using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using ThingMagic;

namespace RfidEncoder.ViewModels
{
    /// <summary>
    ///     Represents the viewModel for MainWindow
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        #region private members

        private static readonly MainWindowViewModel _mainWindowViewModel = new MainWindowViewModel();
        private bool _isRefreshing;
        private Reader _reader;
        private Dictionary<string, string> _optimalReaderSettings;
 
        private void Connect()
        {
            if(_reader != null)    
                _reader.Destroy();
                
            //ConfigureAntennaBoxes(null);
            //ConfigureProtocols(null);
            
            try
            {
                    // Creates a Reader Object for operations on the Reader.
                    string readerUri = SelectedComPort.Name;
                    //Regular Expression to get the com port number from comport name .
                    //for Ex: If The Comport name is "USB Serial Port (COM19)" by using this 
                    // regular expression will get com port number as "COM19".
                    MatchCollection mc = Regex.Matches(readerUri, @"(?<=\().+?(?=\))");
                    foreach (Match m in mc)
                    {
                        readerUri = m.ToString();
                    }
                    _reader = Reader.Create(string.Concat("tmr:///", readerUri));
                    
                //uri = readerUri;

                // If Option selected add the serial-reader-specific message logger
                // before connecting, so we can see the initialization.
                //if ((bool)chkEnableTransportLogging.IsChecked)
                //{
                //    SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                //    saveFileDialog1.Filter = "Text Files (.txt)|*.txt";
                //    saveFileDialog1.Title = "Select a File to save transport layer logging";
                //    string strDestinationFile = "UniversalReader_transportLog" 
                //        + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + @".txt";
                //    saveFileDialog1.FileName = strDestinationFile;
                //    // Show the Dialog.
                //    // If the user clicked OK in the dialog and
                //    // a .txt file was selected, open it.
                //    if (saveFileDialog1.ShowDialog() == true)
                //    {
                //        StreamWriter writer = new StreamWriter(saveFileDialog1.FileName);
                //        writer.AutoFlush = true;
                //        if (_reader is SerialReader)
                //            _reader.Transport += SerialListener;
                //    }
                //    else
                //    {
                //        chkEnableTransportLogging.IsChecked = false;
                //    }
                //}

                //chkEnableTransportLogging.IsEnabled = false;
                
                    // Set the selected baud rate, so that api try's connecting to the 
                    // module with the selected baud rate first
                SetBaudRate();

                //Show the status
                //lblshowStatus.Content = "Connecting..";                
                Mouse.SetCursor(Cursors.Wait);
                _reader.Connect();
                
                Mouse.SetCursor(Cursors.Arrow);
               
                //readerStatus.IsEnabled = true;
                var regionToSet = (Reader.Region)_reader.ParamGet("/reader/region/id");
                
                Regions.Add("Select");
                Regions.AddRange(((Reader.Region[])_reader.ParamGet("/reader/region/supportedRegions")).Select(r=>r.ToString()));

                if (regionToSet != Reader.Region.UNSPEC)
                {
                    //set the region on module
                    SelectedRegion = Regions[Regions.IndexOf(regionToSet.ToString())];
                }
                else
                {
                    SelectedRegion = Regions[Regions.IndexOf("Select")];
                }

                // TODO: Initialize max and min read power for read power slider
                //InitializeRdPwrSldrMaxNMinValue();

                //Initialize settings received o
                _optimalReaderSettings = null;
                InitializeOptimalSettings();

                //_reader.ParamSet("/reader/transportTimeout", int.Parse(txtRFOnTimeout.Text) + 5000);
                //try setting a unique ID
                //reader.ParamSet("/reader/hostname", "this reader");
                //if (_reader is SerialReader)
                //{
                //    cbxBaudRate.IsEnabled = true;
                //}

                // Load Gen2 Settings 
                //initialReaderSettingsLoaded = false;
                //LoadGen2Settings();
                //initialReaderSettingsLoaded = true;

                ////Enable fast search on for mercury6, astra-ex, m6e. 
                //if (model.Equals("Astra") || model.Equals("M5e") 
                //    || model.Equals("M5e Compact") || model.Equals("M5e EU")
                //    || model.Equals("M4e") || model.Equals("M5e PRC"))
                //{
                //    chkEnableFastSearch.IsEnabled = false;
                //}

                //btnRead.IsEnabled = true;
                //advanceReaderSettings.IsEnabled = true;
                //if (_reader is SerialReader)
                //{
                //    var br = _reader.ParamGet("/reader/baudRate").ToString();
                //    SelectedBaudRate = br;

                //    //initializeBaudRate();
                //    if (!(model.Equals("M6e") || model.Equals("M6e Micro") || model.Equals("M6e Micro USB")
                //        || model.Equals("M6e Micro USBPro") || model.Equals("M6e PRC") || model.Equals("M6e Nano")))
                //    {
                //        _reader.ParamSet("/reader/tagReadData/reportRssiInDbm", true);
                //    }
                //}
                //ConfigureAntennaBoxes(_reader);
                //supportedProtocols = (TagProtocol[])_reader.ParamGet("/reader/version/supportedProtocols");
                //ConfigureProtocols(supportedProtocols);
                //btnConnect.ToolTip = "Disconnect";
                //btnConnect.Content = "Disconnect";
                //btnConnectExpander.Content = btnConnect.Content+"...";

                ////Enable save data, btnClearTagReads, read-once, readasyncread buttons
                //saveData.IsEnabled = true;
                //btnClearTagReads.IsEnabled = true;
                //btnRead.IsEnabled = true;

                ////startRead.IsEnabled = true;
                //InitializeRdrDiagnostics();
                //// Disabling equal time switching for both Nano and Micro USB modules.
                //// Because these modules has only one antenna.
                //if (model.Equals("M6e Nano") || model.Equals("M6e Micro USB") || model.Equals("M6e Micro USBPro"))
                //{
                //    rdBtnEqualSwitching.IsEnabled = false;
                //    rdBtnEqualSwitching.IsChecked = false;
                //    rdBtnAutoSwitching.IsChecked = true;
                //}
                //else
                //{
                //    rdBtnEqualSwitching.IsEnabled = true;
                //    rdBtnEqualSwitching.IsChecked = true;
                //    rdBtnAutoSwitching.IsChecked = false;
                //}
                Mouse.SetCursor(Cursors.Arrow);
                //CustomizedMessageBox = new URACustomMessageBoxWindow();

                //// Clear firmware Update open file dialog status
                //txtFirmwarePath.Text = "";
            }
            catch (Exception ex)
            {
                Mouse.SetCursor(Cursors.Arrow);
                //btnConnect.IsEnabled = true;
                //initialReaderSettingsLoaded = true;
                //lblshowStatus.Content = "Disconnected";
                //chkEnableTransportLogging.IsEnabled = true;
                //gridReadOptions.IsEnabled = false;
                //regioncombo.IsEnabled = false;
                //gridDisplayOptions.IsEnabled = false;
                if (ex is IOException)
                {
                    if (!(SelectedComPort.Name.Contains("COM") || SelectedComPort.Name.Contains("com")))
                    {
                        MessageBox.Show("Application needs a valid Reader Address of type COMx",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        
                            MessageBox.Show("Reader not connected on " + SelectedComPort.Name, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    //cmbReaderAddr.IsEnabled = true;
                    //cmbFixedReaderAddr.IsEnabled = true;
                }
                else if (ex is ReaderException)
                {
                    if ( ex.Message.IndexOf("target machine actively refused") != -1)
                    {
                        
                            MessageBox.Show("Error connecting to reader: " + "Connection attempt failed...",
                                "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (ex is FAULT_BL_INVALID_IMAGE_CRC_Exception || ex is FAULT_BL_INVALID_APP_END_ADDR_Exception)
                    {
                        MessageBox.Show("Error connecting to reader: " + ex.Message +". Please update the module firmware.", "Reader Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        //expdrFirmwareUpdate.IsExpanded = true;
                        //expdrFirmwareUpdate.Focus();
                        ////Dock the settings/status panel if not docked, to display Firmware update options
                        //if (pane1Button.Visibility == System.Windows.Visibility.Visible)
                        //{
                        //    pane1Button.RaiseEvent(new RoutedEventArgs(ButtonBase.MouseEnterEvent));
                        //    pane1Pin.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        //}
                        //if (expdrConnect.IsExpanded)
                        //{
                        //    expdrConnect.IsExpanded = false;
                        //}
                    }
                    else
                    {
                        MessageBox.Show("Error connecting to reader: " + ex.Message, "Reader Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if (ex is UnauthorizedAccessException)
                {
                    MessageBox.Show("Access to " + SelectedComPort.Name + " denied. Please check if another "
                        +"program is accessing this port", "Error!", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    if (-1 != ex.Message.IndexOf("target machine actively refused"))
                    {
                        MessageBox.Show("Error connecting to reader: " + "Connection attempt failed...",
                            "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Error connecting to reader: " + ex.Message, "Reader Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Set the selected baud rate
        /// </summary>
        private void SetBaudRate()
        {
            if (_reader != null && SelectedBaudRate != "Select" &&
                SelectedBaudRate != _reader.ParamGet("/reader/baudRate").ToString())
            {
                _reader.ParamSet("/reader/baudRate", Convert.ToInt32(SelectedBaudRate));
            }
        }

        /// <summary>
        /// Populate optimal settings based on reader
        /// </summary>
        private void InitializeOptimalSettings()
        {
            _optimalReaderSettings = new Dictionary<string, string>();
            try
            {
                Gen2.LinkFrequency blf = (Gen2.LinkFrequency)_reader.ParamGet("/reader/gen2/BLF");
                switch (blf)
                {
                    case Gen2.LinkFrequency.LINK250KHZ:
                        _optimalReaderSettings["/reader/gen2/BLF"] = "LINK250KHZ"; break;
                    case Gen2.LinkFrequency.LINK640KHZ:
                        _optimalReaderSettings["/reader/gen2/BLF"] = "LINK640KHZ"; ; break;
                    default:
                        _optimalReaderSettings.Add("/reader/gen2/BLF", ""); break;
                }
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.Contains("Unknown Link Frequency"))
                {
                    MessageBox.Show("Unknown Link Frequency found, Reverting to defaults.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _optimalReaderSettings["/reader/gen2/BLF"] = "LINK250KHZ";
                }
                else
                {
                    _optimalReaderSettings.Add("/reader/gen2/BLF", "");
                }
            }

            try
            {

                Gen2.Tari tariVal = (Gen2.Tari)_reader.ParamGet("/reader/gen2/Tari");
                switch (tariVal)
                {
                    case Gen2.Tari.TARI_6_25US:
                        _optimalReaderSettings["/reader/gen2/tari"] = "TARI_6_25US"; break;
                    case Gen2.Tari.TARI_12_5US:
                        _optimalReaderSettings["/reader/gen2/tari"] = "TARI_12_5US"; break;
                    case Gen2.Tari.TARI_25US:
                        _optimalReaderSettings["/reader/gen2/tari"] = "TARI_25US"; break;
                    default:
                        _optimalReaderSettings.Add("/reader/gen2/tari", ""); break;
                }
            }
            catch (ArgumentException)
            {
                _optimalReaderSettings.Add("/reader/gen2/tari", "");
            }
            catch (ReaderCodeException)
            {
            }

            try
            {
                Gen2.TagEncoding tagencoding = (Gen2.TagEncoding)_reader.ParamGet("/reader/gen2/tagEncoding");
                switch (tagencoding)
                {
                    case Gen2.TagEncoding.FM0:
                        _optimalReaderSettings["/reader/gen2/tagEncoding"] = "FM0"; break;
                    case Gen2.TagEncoding.M2:
                        _optimalReaderSettings["/reader/gen2/tagEncoding"] = "M2"; break;
                    case Gen2.TagEncoding.M4:
                        _optimalReaderSettings["/reader/gen2/tagEncoding"] = "M4"; break;
                    case Gen2.TagEncoding.M8:
                        _optimalReaderSettings["/reader/gen2/tagEncoding"] = "M8"; break;
                    default:
                        _optimalReaderSettings.Add("/reader/gen2/tagEncoding", ""); break;
                }
            }
            catch (ArgumentException)
            {
                _optimalReaderSettings.Add("/reader/gen2/tagEncoding", "");
            }
            try
            {
                Gen2.Session session = (Gen2.Session)_reader.ParamGet("/reader/gen2/session");
                switch (session)
                {
                    case Gen2.Session.S0:
                        _optimalReaderSettings["/reader/gen2/session"] = "S0"; break;
                    case Gen2.Session.S1:
                        _optimalReaderSettings["/reader/gen2/session"] = "S1"; break;
                    case Gen2.Session.S2:
                        _optimalReaderSettings["/reader/gen2/session"] = "S2"; break;
                    case Gen2.Session.S3:
                        _optimalReaderSettings["/reader/gen2/session"] = "S3"; break;
                    default:
                        _optimalReaderSettings.Add("/reader/gen2/session", ""); break;
                }
            }
            catch (ArgumentException)
            {
                _optimalReaderSettings.Add("/reader/gen2/session", "");
            }
            try
            {
                Gen2.Target target = (Gen2.Target)_reader.ParamGet("/reader/gen2/Target");
                switch (target)
                {
                    case Gen2.Target.A:
                        _optimalReaderSettings["/reader/gen2/target"] = "A"; break;
                    case Gen2.Target.B:
                        _optimalReaderSettings["/reader/gen2/target"] = "B"; break;
                    case Gen2.Target.AB:
                        _optimalReaderSettings["/reader/gen2/target"] = "AB"; break;
                    case Gen2.Target.BA:
                        _optimalReaderSettings["/reader/gen2/target"] = "BA"; break;
                    default: _optimalReaderSettings.Add("Target", ""); break;
                }
            }
            catch (FeatureNotSupportedException)
            {
                _optimalReaderSettings.Add("Target", "");
            }
            try
            {
                Gen2.Q qval = (Gen2.Q)_reader.ParamGet("/reader/gen2/q");

                if (qval.GetType() == typeof(Gen2.DynamicQ))
                {
                    _optimalReaderSettings["/reader/gen2/q"] = "DynamicQ";
                }
                else if (qval.GetType() == typeof(Gen2.StaticQ))
                {
                    Gen2.StaticQ stqval = (Gen2.StaticQ)qval;
                    _optimalReaderSettings["/reader/gen2/q"] = "StaticQ";
                    int countQ = Convert.ToInt32(((Gen2.StaticQ)qval).InitialQ);
                    _optimalReaderSettings["/application/performanceTuning/staticQValue"] = countQ.ToString();
                }
                else
                {
                    _optimalReaderSettings.Add("/reader/gen2/q", "");
                }
            }
            catch (FeatureNotSupportedException)
            {
                _optimalReaderSettings.Add("/reader/gen2/q", "");
            }
        }

        #endregion

        #region Constructor
        public MainWindowViewModel()
        {
            ConnectCommand = new DelegateCommand(Connect, 
                () => SelectedComPort != null && !IsRefreshing);
            RefreshCommand = new DelegateCommand(Refresh, () => !IsRefreshing);
            ExitCommand = new DelegateCommand(() => Application.Current.Shutdown());

            Refresh();

            RacesViewModel = new RacesViewModel();
        }
        
        private void Refresh()
        {
            IsRefreshing = true;
            Task.Factory.StartNew(() =>
            {
                var ports = ComPortHelper.GetCOMPortsInfo();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ComPorts = ports;
                    IsRefreshing = false;
                    OnPropertyChanged("ComPorts");
                });
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

        public RacesViewModel RacesViewModel { get; set; }
        public ComPortInfo SelectedComPort { get; set; }
        public List<string> Regions { get; set; }

        public string SelectedRegion { get; set; }
        public IList<string> BaudRates {
            get
            {
                return new List<string>
                {
                    "Select","9600", "19200", "38400", "115200", "230400", "460800","921600"
                };
            }}

        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set
            {
                _isRefreshing = value;
                OnPropertyChanged("IsRefreshing");
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SelectedBaudRate { get; set; }
        public ICommand ConnectCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand ExitCommand { get; set; }
        public bool IsConnected { get; set; }

        #endregion
    }
}
