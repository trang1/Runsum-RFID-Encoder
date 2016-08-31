using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ThingMagic;

namespace RfidEncoder.ViewModels
{
    public class RacesViewModel : ViewModelBase
    {
        private int _nextRaceNumber;
        private uint _nextTagNumber;
        private TotalRaceInfo _totalRaceInfo;
        private RaceInfo _selectedRace;
        private bool _isEncoding;
        private string _statusBarText;
        private Brush _statusBarBackground;
        private bool _overrideTags;

        #region Public
        public TotalRaceInfo TotalRaceInfo
        {
            get { return _totalRaceInfo; }
            set
            {
                _totalRaceInfo = value;
                OnPropertyChanged("TotalRaceInfo");
            }
        }

        public RaceInfo SelectedRace
        {
            get { return _selectedRace; }
            set
            {
                _selectedRace = value;
                OnPropertyChanged("SelectedRace");
            }
        }

        public string EncodingButtonContent
        {
            get { return IsEncoding ? "Stop encoding" : "Start encoding"; }
        }
        public TagOperationsViewModel TagOperationsViewModel { get; set; }
        public ICommand StartEncodingCommand { get; set; }
        public ICommand NewProjectCommand { get; set; }
        public ICommand SelectedTagChangedCommand { get; set; }
        public ICommand SelectedRaceChangedCommand { get; set; }

        public bool IsEncoding
        {
            get { return _isEncoding; }
            set
            {
                _isEncoding = value;
                OnPropertyChanged("IsEncoding");
                OnPropertyChanged("EncodingButtonContent");
            }
        }

        public int NextRaceNumber
        {
            get { return _nextRaceNumber; }
            set
            {
                _nextRaceNumber = value;
                TotalRaceInfo.FireSelectCell(value, null);
                OnPropertyChanged("NextRaceNumber");
            }
        }

        public bool OverrideTags
        {
            get { return _overrideTags; }
            set
            {
                _overrideTags = value;
                OnPropertyChanged("OverrideTags");
            }
        }

        public uint NextTagNumber
        {
            get { return _nextTagNumber; }
            set
            {
                _nextTagNumber = value;
                TotalRaceInfo.FireSelectCell(null, value);
                OnPropertyChanged("NextTagNumber");
            }
        }

        public string FileName { get; set; }
        public int SelectedTagIndex { get; set; }

        public string StatusBarText
        {
            get { return _statusBarText; }
            set
            {
                _statusBarText = value;
                OnPropertyChanged("StatusBarText");
            }
        }

        public Brush StatusBarBackground
        {
            get { return _statusBarBackground; }
            set
            {
                _statusBarBackground = value;
                OnPropertyChanged("StatusBarBackground");
            }
        }

        #endregion

        public RacesViewModel()
        {
            TagOperationsViewModel = new TagOperationsViewModel();

            StartEncodingCommand = new DelegateCommand(StartEncoding, ()=> TagOperationsViewModel.IsConnected &&
                !TagOperationsViewModel.IsWaitingForTagRead);
            NewProjectCommand = new DelegateCommand(NewProject);
            SelectedRaceChangedCommand = new DelegateCommand(SelectedRaceChanged);
            SelectedTagChangedCommand = new DelegateCommand(SelectedTagChanged);
        }

        private void SelectedTagChanged()
        {
            if (SelectedTagIndex >= 0)
            {
                NextTagNumber = GetNextTag();
            }
        }

        private void SelectedRaceChanged()
        {
            if (SelectedRace != null)
            {
                NextRaceNumber = SelectedRace.RaceNumber;
                NextTagNumber = GetNextTag();
            }
        }

        private void NewProject()
        {
            if (TotalRaceInfo == null)
                TotalRaceInfo = new TotalRaceInfo(null)
                {
                    TagsPerRaceCount = 2,
                    CodeLength = 4,
                    StartNumber = 100,
                    EndNumber = 1000,
                    AddPrefix = true,
                    IsDigitInserting = true,
                    Prefix = "123",
                    AccessPassword = "00000000"
                };


            var wnd = new RacesSettings {Owner = Application.Current.MainWindow, IsEnabled = !IsEncoding};
            var model = new RacesSettingsViewModel(TotalRaceInfo) {FrameworkElement = wnd};
            wnd.DataContext = model;

            if (wnd.ShowDialog().GetValueOrDefault(false))
            {
                var info = model.TotalRaceInfo;

                for (var i = info.StartNumber; i <= info.EndNumber; i++)
                {
                    var list = new List<uint?>();
                    for (var j = 0; j < info.TagsPerRaceCount; j++)
                        list.Add(null);

                    var ri = new RaceInfo {RaceNumber = i, TagList = new ObservableCollection<uint?>(list)};
                    info.Add(ri);
                }

                TotalRaceInfo = info;

                NextRaceNumber = TotalRaceInfo.StartNumber;
                SelectedRace = TotalRaceInfo.First();
                NextTagNumber = GetNextTag();
            }
        }

        private void StartEncoding()
        {
            if (IsEncoding)
            {
                IsEncoding = false;
                StatusBarText = "Encoding stopped";
                StatusBarBackground = Brushes.Green;
                return;
            }

            if (string.IsNullOrEmpty(TagOperationsViewModel.SelectedRegion) ||
                TagOperationsViewModel.SelectedRegion == "Select")
            {
                MessageBox.Show("Please, select the region first.", "Information", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            Task.Factory.StartNew(() =>
            {
                do
                {
                    //1. Wait for Tag
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsEncoding = true;
                        StatusBarText = "Waiting for tag...";
                        StatusBarBackground = Brushes.Yellow;
                    });

                    //2. Read tag
                    var tag = TagOperationsViewModel.ReadTagSync();

                    if (!IsEncoding) return;

                    //3. if epc tag is in the number set to be encoded as defined by the project (i.e. 12301020) this indicates it is already encoded, 
                    //3a. show 'already encoded as 1020' 
                    //3b. return to 1.
                    if (tag.HasValue && !OverrideTags && CheckRepeatedTag(tag))
                    {
                        StatusBarText = "Tag " + tag + " is already encoded";
                        StatusBarBackground = Brushes.OrangeRed;
                        Speak("Already encoded");
                        Thread.Sleep(500);
                        continue;
                    }

                    var apLocked = false;

                    //4. determine if access password is locked. In URA I see 'Gen2 memory locked' in Reserved Memory Bank (0) access password.
                    if (TagOperationsViewModel.CheckAccessPasswordIsLocked())
                    {
                        //4a. if access password locked, try the current access password from 'new project dialogue' 
                        //which has yet to be created. if fails, open dialogue to ask for old access password. Remember this password for 
                        //future uses of this dialogue. 
                        if (!TagOperationsViewModel.ApplyLockAction
                            (new Gen2.LockAction(Gen2.LockAction.ACCESS_UNLOCK), _totalRaceInfo.AccessPassword))
                        {
                            //open dialog
                            var dialog = new NewPasswordWnd(_totalRaceInfo.AccessPassword);
                            if (dialog.ShowDialog().GetValueOrDefault(false))
                            {

                                if (!TagOperationsViewModel.ApplyLockAction
                                    (new Gen2.LockAction(Gen2.LockAction.ACCESS_UNLOCK), dialog.Password))
                                {
                                    // failed again
                                    // check if epc is locked
                                    // if locked -> invalid chip -> continue
                                    if (TagOperationsViewModel.CheckEpcIsLocked(dialog.Password))
                                    {
                                        StatusBarText = "Invalid chip, continue...";
                                        StatusBarBackground = Brushes.Red;
                                        Speak("Invalid chip");
                                        Thread.Sleep(300);
                                        continue;
                                    }
                                    //if not locked ask for continue without locking
                                    else
                                    {
                                        if (MessageBox.Show("EPC is not locked. Continue without locking?", "Question",
                                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                        {
                                            //if yes
                                            apLocked = true;    
                                        }
                                        //if no -> continue
                                        else
                                        {
                                            Thread.Sleep(300);
                                            continue;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                
                            }
                        }
                    }

                    var isLockingNeeded = _totalRaceInfo.AccessPassword != "0";
                    
                    //4b. if access password is not locked, encode access password = 
                    //#8 digits from access password dialogue needed in 'new project' screen# .
                    if (!apLocked && isLockingNeeded)
                    {
                        TagOperationsViewModel.WriteAccessPassword(_totalRaceInfo.AccessPassword);
                    }

                    //5. encode tag to proper number
                    var encoded = TagOperationsViewModel.WriteTag(NextTagNumber);

                    if (encoded && !apLocked && isLockingNeeded)
                    {
                        //6. lock epc memory (tag) with write lock. Gen2.LockAction.EPC_LOCK
                        TagOperationsViewModel.ApplyLockAction(
                            new Gen2.LockAction(Gen2.LockAction.EPC_LOCK), _totalRaceInfo.AccessPassword);
                        
                        //7. lock access password with read/write lock. Gen2.LockAction.ACCESS_LOCK 
                        TagOperationsViewModel.ApplyLockAction(
                            new Gen2.LockAction(Gen2.LockAction.ACCESS_LOCK), _totalRaceInfo.AccessPassword);

                        // if we have a kill password
                        if (!string.IsNullOrEmpty(_totalRaceInfo.KillPassword))
                        {
                            //8. set kill password =#8 digits from kill password dialogue needed in 'new project' screen#
                            TagOperationsViewModel.WriteKillPassword(_totalRaceInfo.KillPassword);

                            //9. lock kill password with read/write lock. Gen2.LockAction.LILL_LOCK
                            TagOperationsViewModel.ApplyLockAction(
                                new Gen2.LockAction(Gen2.LockAction.KILL_LOCK), _totalRaceInfo.AccessPassword);
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SelectedRace.TagList[SelectedTagIndex] = NextTagNumber;
                        if (encoded)
                        {
                            StatusBarText = "Successfully encoded!";
                            StatusBarBackground = Brushes.LawnGreen;
                        }
                        else
                        {
                            StatusBarText = "Encoding error, trying again...";
                            StatusBarBackground = Brushes.Red;
                        }   
                    });

                    if (encoded)
                    {
                        //10. re-read tag to validate that it is properly coded.
                        StatusBarText = "Verifying...";
                        StatusBarBackground = Brushes.Orange;
                        encoded = TagOperationsViewModel.VerifyTag(NextTagNumber);
                    }

                    //11. have computer speak the last two digits of the number.
                    if (encoded)
                    {
                        SayNumber(NextTagNumber);
                        WriteToFile();
                        //12. increment to next tag
                        TotalRaceInfo.FireNextTag(NextTagNumber);
                    }

                    Thread.Sleep(300);
                } while (IsEncoding);
            }
                );
        }

        private bool CheckRepeatedTag(uint? tag)
        {
            foreach (var race in TotalRaceInfo)
            {
                if (race.TagList.Contains(tag))
                    return true;
            }

            return false;
        }

        private void WriteToFile()
        {
            if (!string.IsNullOrEmpty(TotalRaceInfo.FileName))
            {
                File.AppendAllLines(TotalRaceInfo.FileName,
                    new[] {string.Format("{0},{1}", SelectedRace.RaceNumber, NextTagNumber)});
            }
        }

        private void SayNumber(uint nextTagNumber)
        {
            var digits = nextTagNumber.ToString().Reverse().Take(2).Reverse();
            string s1 = digits.First() + " " + digits.Last();
			Speak(s1);
//			Speak(digits.First().ToString());
//            Speak(digits.Last().ToString());
        }

        private void Speak(String str)
        {
            using (var ss= new SpeechSynthesizer{Rate = 5})
            {
                ss.SpeakAsync(str);
            }
        }

        private uint GetNextTag()
        {
            var sb = new StringBuilder();

            if (TotalRaceInfo.AddPrefix)
                sb.Append(TotalRaceInfo.Prefix);

            if (TotalRaceInfo.IsDigitInserting)
            {
                if (SelectedTagIndex < 0)
                    sb.Append("0");
                else
                {
                    sb.Append(SelectedTagIndex);
                }
            }

            return uint.Parse(sb.Append(NextRaceNumber.ToString().PadLeft(TotalRaceInfo.CodeLength, '0')).ToString());
        }
    }

    public class RaceInfo
    {
        public int RaceNumber { get; set; }

        public ObservableCollection<uint?> TagList { get; set; }
    }

    public class TotalRaceInfo : ObservableCollection<RaceInfo>, INotifyPropertyChanged
    {
        private int _startNumber;
        private int _endNumber;
        private int _tagsPerRaceCount;
        private bool _isDigitInserting;
        private string _fileName;
        private bool _addPrefix;
        private string _prefix;
        private int _codeLength;
        private string _killPassword;
        private string _accessPassword;
        private bool _permalock;

        public int StartNumber
        {
            get { return _startNumber; }
            set
            {
                _startNumber = value;
                OnPropertyChanged("StartNumber");
            }
        }

        public int EndNumber
        {
            get { return _endNumber; }
            set
            {
                _endNumber = value;
                OnPropertyChanged("EndNumber");
            }
        }

        public int TagsPerRaceCount
        {
            get { return _tagsPerRaceCount; }
            set
            {
                _tagsPerRaceCount = value;
                OnPropertyChanged("TagsPerRaceCount");
            }
        }

        public bool IsDigitInserting
        {
            get { return _isDigitInserting; }
            set
            {
                _isDigitInserting = value;
                OnPropertyChanged("IsDigitInserting");
            }
        }

        public bool AddPrefix
        {
            get { return _addPrefix; }
            set
            {
                _addPrefix = value;
                OnPropertyChanged("AddPrefix");
            }
        }

        public string Prefix
        {
            get { return _prefix; }
            set
            {
                _prefix = value;
                OnPropertyChanged("Prefix");
            }
        }

        public int CodeLength
        {
            get { return _codeLength; }
            set
            {
                _codeLength = value;
                OnPropertyChanged("CodeLength");
            }
        }

        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                OnPropertyChanged("FileName");
            }
        }

        public string AccessPassword
        {
            get { return _accessPassword; }
            set
            {
                _accessPassword = value;
                OnPropertyChanged("AccessPassword");
            }
        }

        public string KillPassword
        {
            get { return _killPassword; }
            set
            {
                _killPassword = value;
                OnPropertyChanged("KillPassword");
            }
        }

        public bool Permalock
        {
            get { return _permalock; }
            set
            {
                _permalock = value;
                OnPropertyChanged("Permalock");
            }
        }

        public TotalRaceInfo(TotalRaceInfo totalRaceInfo)
        {
            if(totalRaceInfo == null) return;

            _startNumber = totalRaceInfo.StartNumber;
            _endNumber = totalRaceInfo.EndNumber;
            _tagsPerRaceCount = totalRaceInfo.TagsPerRaceCount;
            _isDigitInserting = totalRaceInfo.IsDigitInserting;
            _fileName = totalRaceInfo.FileName;
            _addPrefix = totalRaceInfo.AddPrefix;
            _prefix = totalRaceInfo.Prefix;
            _codeLength = totalRaceInfo.CodeLength;
            _accessPassword = totalRaceInfo.AccessPassword;
            _killPassword = totalRaceInfo.KillPassword;
            _permalock = totalRaceInfo.Permalock;
        }

        #region INotifyPropertyChanged

        public new event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public event TagEventHandler NextTag = (sender, args) => {};
        public event TagEventHandler SelectCell = (sender, args) => { };

        public delegate void TagEventHandler (object sender, TagEventArgs tea);
        public void FireNextTag(uint lastTag)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() => NextTag(this, new TagEventArgs(lastTag)));
            });

        }

        public void FireSelectCell(int? nextRaceNumber, uint? nextTagNumber)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() => SelectCell(this, new TagEventArgs(nextTagNumber, nextRaceNumber)));
            });
        }
    }

    public class TagEventArgs : EventArgs
    {
        public uint EncodedTag { get; set; }
        public uint? NextTag { get; set; }
        public int? NextRace { get; set; }
        
        public TagEventArgs(uint encodedTag)
        {
            EncodedTag = encodedTag;
        }

        public TagEventArgs(uint? nextTag, int? nextRace)
        {
            NextTag = nextTag;
            NextRace = nextRace;
        }
    }
}
