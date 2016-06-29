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
            StartEncodingCommand = new DelegateCommand(StartEncoding, ()=>true/*/ MainWindowViewModel.Instance.TagOperationsViewModel.IsConnected*/);
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
                TotalRaceInfo = new TotalRaceInfo(null) {TagsPerRaceCount = 1, CodeLength = 2};

            var wnd = new RacesSettings {Owner = Application.Current.MainWindow, IsEnabled = !IsEncoding};
            var model = new RacesSettingsViewModel(TotalRaceInfo) { FrameworkElement = wnd };
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

            if (string.IsNullOrEmpty(MainWindowViewModel.Instance.TagOperationsViewModel.SelectedRegion) ||
                MainWindowViewModel.Instance.TagOperationsViewModel.SelectedRegion == "Select")
            {
                MessageBox.Show("Please, select the region first.", "Information", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            Task.Factory.StartNew(() =>
            {
                do
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsEncoding = true;
                        StatusBarText = "Waiting for tag...";
                        StatusBarBackground = Brushes.Yellow;
                    });

                    //MessageBox.Show("Tag " + NextTagNumber + " encoded.");
                    var tag = MainWindowViewModel.Instance.TagOperationsViewModel.ReadTagSync();
                    if (tag.HasValue && !OverrideTags && CheckRepeatedTag(tag))
                    {
                        StatusBarText = "Tag "+tag+" is already encoded";
                        StatusBarBackground = Brushes.OrangeRed;
                        Speak("Already encoded");
                        Thread.Sleep(1000);
                        continue;
                    }

                    var encoded = MainWindowViewModel.Instance.TagOperationsViewModel.WriteTag(NextTagNumber);
                    if (encoded)
                    {
                        StatusBarText = "Verifying...";
                        StatusBarBackground = Brushes.Orange;
                        encoded = MainWindowViewModel.Instance.TagOperationsViewModel.VerifyTag(NextTagNumber);
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

                    if (encoded || Debugger.IsAttached)
                    {
                        SayNumber(NextTagNumber);
                        WriteToFile();
                        TotalRaceInfo.FireNextTag(NextTagNumber);
                    }

                    Thread.Sleep(1000);
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
            Speak(digits.First().ToString());
            Speak(digits.Last().ToString());
        }

        private void Speak(String str)
        {
            using (var ss= new SpeechSynthesizer{Rate = -4})
            {
                ss.Speak(str);
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
        public delegate void TagEventHandler (object sender, TagEventArgs tea);
        public void FireNextTag(uint lastTag)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(200);
                Application.Current.Dispatcher.Invoke(() => NextTag(this, new TagEventArgs(lastTag)));
            });

        }
    }

    public class TagEventArgs : EventArgs
    {
        public uint EncodedTag { get; set; }

        public TagEventArgs(uint encodedTag)
        {
            EncodedTag = encodedTag;
        }
    }
}
