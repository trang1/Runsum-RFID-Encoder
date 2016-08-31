using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ThingMagic;

namespace RfidEncoder.ViewModels
{
    public class TagOperationsViewModel : ViewModelBase
    {
        private Reader _reader;
        private bool _isConnected;
        private Dictionary<string, string> _optimalReaderSettings;
        private bool _isRefreshing;
        private bool _isWaitingForTagRead;

        public TagOperationsViewModel()
        {
            ConnectCommand = new DelegateCommand(Connect,
                   () => IsConnected ? !MainWindowViewModel.Instance.RacesViewModel.IsEncoding :
                   SelectedComPort != null && !IsRefreshing);
            RefreshCommand = new DelegateCommand(Refresh, () => !IsRefreshing);

            ReadTagCommand = new DelegateCommand(ReadTag, () => IsConnected && !IsRefreshing && 
                !MainWindowViewModel.Instance.RacesViewModel.IsEncoding && !IsWaitingForTagRead);
            ReadMultipleTagsCommand = new DelegateCommand(ReadMultipleTags, () => IsConnected && !IsRefreshing &&
                !MainWindowViewModel.Instance.RacesViewModel.IsEncoding);

            WriteTagCommand = new DelegateCommand(WriteTag, () => IsConnected && !IsRefreshing &&
                !MainWindowViewModel.Instance.RacesViewModel.IsEncoding && !IsWaitingForTagRead);

            Regions = new List<string>();

            var baudRate = ConfigurationManager.AppSettings["DefaultBaudRate"];
            if (BaudRates.Contains(baudRate))
                SelectedBaudRate = baudRate;

            Refresh();          
        }

        private void ReadMultipleTags()
        {
            if (IsWaitingForTagRead)
            {
                _cancelReading = true;
                IsWaitingForTagRead = false;
                return;
            }

            IsWaitingForTagRead = true;
            Task.Factory.StartNew(() =>
            {
                do
                {

                    var tag = ReadTagSync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SingleReadResult = tag.ToString();
                    });
                } while (!_cancelReading);

                _cancelReading = false;
            });
        }

        private void ReadTag()
        {
            IsWaitingForTagRead = true;
            Task.Factory.StartNew(() =>
            {
                var tag = ReadTagSync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SingleReadResult = tag.ToString();
                    IsWaitingForTagRead = false;
                });
            });
        }

        public bool IsWaitingForTagRead
        {
            get { return _isWaitingForTagRead; }
            set
            {
                _isWaitingForTagRead = value; 
                OnPropertyChanged("IsWaitingForTagRead");
                OnPropertyChanged("ReadMultipleTagsButtonContent");
                CommandManager.InvalidateRequerySuggested();
            }
        }

        volatile bool _cancelReading;
        private double _readPower;

        public string ReadMultipleTagsButtonContent
        {
            get { return IsWaitingForTagRead ? "Stop reading" : "Read continuously"; }
        }
        private void WriteTag()
        {
            uint tag;
            if (uint.TryParse(TagToWrite, out tag))
            {
                WriteTag(tag);
                MessageBox.Show("Tag " + TagToWrite + " successfully written.", "Information");
            }
            else
            {
                MessageBox.Show("The string " + TagToWrite + " couldn't be recognized as a valid unsigned integer.",
                    "Error");
            }
        }

        // seems like the wrong method
        private uint? ReadTag(int timeout)
        {
            try
            {
                CheckParams();
                var data = _reader.Read(timeout);
                if (data.Length > 0)
                {
                    var tag = ByteConv.ToU32(data[0].Epc, 0);
                    Trace.TraceInformation("Tag " + tag + " has been read.");
                    return tag;
                }
                return null;
            }
            catch (Exception exception)
            {
                var error = "Error reading tags. " + exception.Message;
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Trace.TraceError(error + exception.StackTrace);
                return null;
            }
        }

        public uint? ReadTagSync()
        {
            var action = new Action<Exception>(exception =>
                {
                    var error = "Error reading tags. " + exception.Message;
                    MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Trace.TraceError(error + exception.StackTrace);
                });

            uint? tag = null;

            var readAction = new EventHandler<TagReadDataEventArgs>((sender, e) =>
            {
                var data = e.TagReadData;

                tag = ByteConv.ToU32(data.Epc, 0);
                Trace.TraceInformation("Tag " + tag + " has been read.");
            });

            var exceptionAction = new EventHandler<ReaderExceptionEventArgs>((sender, reea) =>
            {
                if (reea.ReaderException != null)
                    action(reea.ReaderException);
            });

            try
            {
                CheckParams();

                // Create a simplereadplan which uses the antenna list created above
                SimpleReadPlan plan = new SimpleReadPlan(new[] {1}, TagProtocol.GEN2, null, null, 1000);
                // Set the created readplan
                _reader.ParamSet("/reader/read/plan", plan);

                // Create and add tag listener
                _reader.TagRead += readAction;

                // Create and add read exception listener
                _reader.ReadException += exceptionAction;

                // Search for tags in the background
                _reader.StartReading();

                do
                {
                    Thread.Sleep(200);

                    // do events
                    Application.Current.Dispatcher.Invoke(() => { });
                    
                } while (!tag.HasValue);

                _reader.StopReading();
            }
            catch (Exception exception)
            {
                action(exception);
            }
            _reader.TagRead -= readAction;
            _reader.ReadException -= exceptionAction;
            return tag;
        }

        public ComPortInfo SelectedComPort { get; set; }
        public List<string> Regions { get; set; }

        public double ReadPower
        {
            get { return _readPower; }
            set
            {
                _readPower = value;
                Task.Factory.StartNew(SetReadPower);
                OnPropertyChanged("ReadPower");
            }
        }

        public IList<ComPortInfo> ComPorts{ get; set; }

        public string SelectedRegion
        {
            get { return _selectedRegion; }
            set
            {
                _selectedRegion = value;
                OnPropertyChanged("SelectedRegion");
            }
        }

        public string SingleReadResult
        {
            get { return _singleReadResult; }
            set
            {
                _singleReadResult = value;
                OnPropertyChanged("SingleReadResult");
            }
        }

        public string TagToWrite { get; set; }
        public IList<string> BaudRates
        {
            get
            {
                return new List<string>
                {
                    "Select","9600", "19200", "38400", "115200", "230400", "460800","921600"
                };
            }
        }

        public string ConnectButtonContent
        {
            get
            {
                return _isConnected ? "Disconnect" : "Connect";
            }
        }

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

        public ICommand ReadTagCommand { get; set; }
        public ICommand ReadMultipleTagsCommand { get; set; }

        public ICommand WriteTagCommand { get; set; }
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                _isConnected = value;
                OnPropertyChanged("IsConnected");
                OnPropertyChanged("ConnectButtonContent");
            }
        }


        public bool WriteTag(uint tag)
        {
            try
            {
                CheckParams();

                var data = ByteConv.EncodeU32(tag);
                //_reader.WriteTag(null, new TagData(epcTag));
                _reader.ExecuteTagOp(new Gen2.WriteTag(new Gen2.TagData(data)), null);
                Trace.TraceInformation("Tag " + tag + " has been written.");
                return true;
            }
            catch (Exception exception)
            {
                var error = "Error encoding the tag " + tag + ". " + exception.Message;
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Trace.TraceError(error + exception.StackTrace);
                return false;
            }
        }

        private void CheckParams()
        {
            if (_reader.ParamGet("/reader/region/id").ToString() != SelectedRegion)
                _reader.ParamSet("/reader/region/id", Enum.Parse(typeof (Reader.Region), SelectedRegion));

            if (_reader.ParamGet("/reader/tagop/antenna").ToString() != "1")
                _reader.ParamSet("/reader/tagop/antenna", 1);
        }

        public bool VerifyTag(uint tagToVerify)
        {
            var tag = ReadTagSync();
            if (tag > 0)
            {
                return tag == tagToVerify;
            }
            return false;
        }

        bool _changingPower;
        private string _singleReadResult;
        private string _selectedRegion;

        private void SetReadPower()
        {
            try
            {
                Debug.WriteLine("Power to set = " + Convert.ToInt32(1000 + 130 * _readPower));
                if (_reader != null && !_changingPower)
                {
                    _changingPower = true;
                    _reader.ParamSet("/reader/radio/readPower", Convert.ToInt32(1000 + 130 * _readPower));
                    _changingPower = false;
                }
            }
            catch (Exception ex)
            {
                var error = ex.Message + " Check supported protocol configurations in Reader's Hardware Guide.";
                MessageBox.Show(error, "Unsupported Reader Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
                Trace.TraceError(error + ex.StackTrace);

                _changingPower = false;
            }
        }

        private void Refresh()
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(300); // small delay for progress bar to be displayed
                Application.Current.Dispatcher.Invoke(() => IsRefreshing = true);
                var ports = ComPortHelper.GetCOMPortsInfo();

                //Thread.Sleep(3000);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ComPorts = ports;
                    IsRefreshing = false;
                    OnPropertyChanged("ComPorts");
                });
            });
        }
        private void Connect()
        {
            if (_reader != null || _isConnected)
                _reader.Destroy();

            if (IsConnected)
            {
                IsConnected = false;
                OnPropertyChanged("ConnectButtonContent");
                return;
            }
            //Regions.AddRange(new[] {"LA", "NA", "BGG"});
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
                
                // HERE WE CAN SWITCH TO TEST THE FUNCIONALITY
                //_reader = new ReaderMockup();

                // Set the selected baud rate, so that api try's connecting to the 
                // module with the selected baud rate first
                SetBaudRate();

                //Show the status
                //lblshowStatus.Content = "Connecting..";                
                Mouse.SetCursor(Cursors.Wait);
                _reader.Connect();

               // Mouse.SetCursor(Cursors.Arrow);

                //readerStatus.IsEnabled = true;
               
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
                IsConnected = true;

                //CustomizedMessageBox = new URACustomMessageBoxWindow();
                
                Task.Factory.StartNew(SetDefaultRegion);
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
                Trace.TraceError("Connection error. " + ex.Message + ex.StackTrace);
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
                    if (ex.Message.IndexOf("target machine actively refused") != -1)
                    {

                        MessageBox.Show("Error connecting to reader: " + "Connection attempt failed...",
                            "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (ex is FAULT_BL_INVALID_IMAGE_CRC_Exception || ex is FAULT_BL_INVALID_APP_END_ADDR_Exception)
                    {
                        MessageBox.Show("Error connecting to reader: " + ex.Message + ". Please update the module firmware.", "Reader Error",
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
                        + "program is accessing this port", "Error!", MessageBoxButton.OK,
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

        private void SetDefaultRegion()
        {
            Thread.Sleep(300);

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    Regions.Add("Select");
                    Regions.AddRange(
                        ((Reader.Region[])_reader.ParamGet("/reader/region/supportedRegions")).Select(r => r.ToString()));

                    var regionToSet = (Reader.Region)_reader.ParamGet("/reader/region/id");
                    Trace.TraceInformation("Region to set = " + regionToSet);

                    var region = ConfigurationManager.AppSettings["DefaultRegion"];
                    if (Regions.Contains(region))
                    //(regionToSet != Reader.Region.UNSPEC)
                    {
                        //set the region on module
                        SelectedRegion = Regions[Regions.IndexOf(region)];
                        //Regions[Regions.IndexOf(regionToSet.ToString())];
                    }
                    else
                    {
                        SelectedRegion = Regions[Regions.IndexOf("Select")];
                    }
                }
                catch (Exception exception)
                {
                    var error = "Error setting default region. " + exception.Message;
                    Trace.TraceError(error + exception.StackTrace);
                }
            });
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

        /// <summary>
        /// Write the access password in the reserved memory
        /// </summary>
        public void WriteAccessPassword(string accessPassword)
        {
            try
            {
                CheckParams();
                _reader.ParamSet("/reader/tagop/protocol", TagProtocol.GEN2);
                ushort[] dataToBeWritten = null;
                dataToBeWritten = ByteConv.ToU16s(ByteFormat.FromHex(accessPassword.Replace(" ", "")));
                _reader.ExecuteTagOp(new Gen2.WriteData(Gen2.Bank.RESERVED, 2, dataToBeWritten), null);
                //MessageBox.Show("Access Password has successfully been set to 0x" + txtbxAccesspaasword.Text.Replace(" ", ""), "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var error = "Error writing the access password (" + accessPassword + "). " + ex.Message;
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Trace.TraceError(error + ex.StackTrace);
            }
        }

        /// <summary>
        /// Write the kill password in the reserved memory
        /// </summary>
        public void WriteKillPassword(string killPassword)
        {
            try
            {
                CheckParams();
                _reader.ParamSet("/reader/tagop/protocol", TagProtocol.GEN2);
                ushort[] dataToBeWritten = null;
                dataToBeWritten = ByteConv.ToU16s(ByteFormat.FromHex(killPassword.Replace(" ", "")));
                _reader.ExecuteTagOp(new Gen2.WriteData(Gen2.Bank.RESERVED, 0, dataToBeWritten), null);
                //MessageBox.Show("Access Password has successfully been set to 0x" + txtbxAccesspaasword.Text.Replace(" ", ""), "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var error = "Error writing the kill password (" + killPassword + "). " + ex.Message;
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Trace.TraceError(error + ex.StackTrace);
            }
        }
        /// <summary>
        /// Apply lock action on the tag
        /// </summary>
        public bool ApplyLockAction(Gen2.LockAction action, string accessPassword)
        {
            try
            {
                CheckParams();
                _reader.ParamSet("/reader/tagop/protocol", TagProtocol.GEN2);

                _reader.ExecuteTagOp(new Gen2.Lock(ByteConv.ToU32(
                    ByteFormat.FromHex(accessPassword.Replace(" ", "")), 0), action), null);
                return true;
            }
            catch (Exception ex)
            {
                var error = string.Format("Error applying the lock action (action = {0}, password = {1}). {2}", action,
                    accessPassword, ex.Message);
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Trace.TraceError(error + ex.StackTrace);
                return false;
            }
        }

        public bool CheckAccessPasswordIsLocked()
        {
            try
            {
                string reservedBankData = string.Empty;
                //Read access password
                var op = new Gen2.ReadData(Gen2.Bank.RESERVED, 2, 2);
                var reservedData = (ushort[])_reader.ExecuteTagOp(op, null);

                if (null != reservedData)
                    reservedBankData = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(reservedData), "", " ");

                var accessPassword = reservedBankData.Trim(' ');
                return false;
            }
            catch (Exception ex)
            {
                if (ex is FAULT_GEN2_PROTOCOL_MEMORY_LOCKED_Exception)
                {
                    return true;
                }
                Trace.TraceError("Error checking access password. " + ex.Message + ex.StackTrace);
                return false;
            }
        }

        public bool CheckEpcIsLocked(string accessPassword)
        {
            try
            {
                _reader.ParamSet("/reader/tagop/protocol", TagProtocol.GEN2);

                _reader.ExecuteTagOp(new Gen2.Lock(ByteConv.ToU32(
                    ByteFormat.FromHex(accessPassword.Replace(" ", "")), 0), new Gen2.LockAction(Gen2.LockAction.EPC_UNLOCK)), null);
                return false;
            }
            catch (Exception ex)
            {
                if (ex is FAULT_GEN2_PROTOCOL_MEMORY_LOCKED_Exception)
                {
                    return true;
                }
                Trace.TraceError("Error checking EPC. " + ex.Message + ex.StackTrace);
                return false;
            }
        }
    }
}
