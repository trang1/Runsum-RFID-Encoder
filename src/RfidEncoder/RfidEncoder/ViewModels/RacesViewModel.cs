using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RfidEncoder.ViewModels
{
    public class RacesViewModel : ViewModelBase
    {
        private int _nextRaceNumber;
        private int _nextTagNumber;
        private TotalRaceInfo _totalRaceInfo;
        private RaceInfo _selectedRace;

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

        public ICommand StartEncodingCommand { get; set; }
        public ICommand NewProjectCommand { get; set; }
        public ICommand SelectedTagChangedCommand { get; set; }
        public ICommand SelectedRaceChangedCommand { get; set; }
        public bool IsEncoding { get; set; }
        public int NextRaceNumber
        {
            get { return _nextRaceNumber; }
            set
            {
                _nextRaceNumber = value;
                OnPropertyChanged("NextRaceNumber");
            }
        }

        public int NextTagNumber
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
        public string StatusBarText { get; set; }

        public RacesViewModel()
        {
            StartEncodingCommand = new DelegateCommand(StartEncoding, ()=>true/* MainWindowViewModel.Instance.IsConnected*/);
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
                TotalRaceInfo = new TotalRaceInfo(null) {TagsPerRaceCount = 1};

            var wnd = new RacesSettings {Owner = Application.Current.MainWindow};
            var model = new RacesSettingsViewModel(TotalRaceInfo) { FrameworkElement = wnd };
            wnd.DataContext = model;

            if (wnd.ShowDialog().GetValueOrDefault(false))
            {
                var info = model.TotalRaceInfo;

                for (var i = info.StartNumber; i <= info.EndNumber; i++)
                {
                    var list = new List<int?>();
                    for (var j = 0; j < info.TagsPerRaceCount; j++)
                        list.Add(null);

                    var ri = new RaceInfo {RaceNumber = i, TagList = new ObservableCollection<int?>(list)};
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
            do
            {
                IsEncoding = true;

                StatusBarText = "Waiting for tag...";

                //MessageBox.Show("Tag " + NextTagNumber + " encoded.");
                MainWindowViewModel.Instance.EncodeTag(NextTagNumber);


                SelectedRace.TagList[SelectedTagIndex] = NextTagNumber;
                Task.Factory.StartNew(() => SayNumber(NextTagNumber));

                TotalRaceInfo.FireNextTag();

                Thread.Sleep(240);
            } while (IsEncoding);
        }

        private void SayNumber(int nextTagNumber)
        {
            var ss = new SpeechSynthesizer {Rate = -5};
            var digits = nextTagNumber.ToString().Reverse().Take(2).Reverse();
            ss.Speak(digits.First().ToString());
            ss.Speak(digits.Last().ToString());
        }

        private int GetNextTag()
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

            return int.Parse(sb.Append(NextRaceNumber.ToString().PadLeft(TotalRaceInfo.CodeLength, '0')).ToString());

        }
    }

    public class RaceInfo
    {
        public int RaceNumber { get; set; }

        public ObservableCollection<int?> TagList { get; set; }
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

        public event EventHandler NextTag = (sender, args) => {};

        public void FireNextTag()
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(200);
                Application.Current.Dispatcher.Invoke(() => NextTag(this, new EventArgs()));
            });

        }
    }
}
