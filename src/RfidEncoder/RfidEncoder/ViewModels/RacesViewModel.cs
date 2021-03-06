﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// <summary>
    ///     Viewmodel which encodes chips for races
    /// </summary>
    public class RacesViewModel : ViewModelBase
    {
        private bool _isEncoding;
        private int _nextRaceNumber;
        private uint _nextTagNumber;
        private bool _overrideTags;
        private RaceInfo _selectedRace;
        private Brush _statusBarBackground;
        private string _statusBarText;
        private TotalRaceInfo _totalRaceInfo;

        public RacesViewModel()
        {
            TagOperationsViewModel = new TagOperationsViewModel();

            StartEncodingCommand = new DelegateCommand(StartEncoding, () => TagOperationsViewModel.IsConnected &&
                                                                            !TagOperationsViewModel.IsWaitingForTagRead);
            NewProjectCommand = new DelegateCommand(NewProject);
            SelectedRaceChangedCommand = new DelegateCommand(SelectedRaceChanged);
            SelectedTagChangedCommand = new DelegateCommand(SelectedTagChanged);
        }

        #region Public properties

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

        #region private methods

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
                    AccessPassword = "00000000",
                    SetNewAccessPassword = false,
                    NewAccessPassword = "00000000"
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

        // main algorythm realization
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
                var newAccessPassword = "";
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
                            (new Gen2.LockAction(Gen2.LockAction.ACCESS_UNLOCK), string.IsNullOrEmpty(newAccessPassword)
                                ? _totalRaceInfo.AccessPassword
                                : newAccessPassword))
                        {
                            //open dialog
                            var dialogResult = false;

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var dialog = new NewPasswordWnd(_totalRaceInfo.AccessPassword);
                                dialog.Owner = Application.Current.MainWindow;
                                dialogResult = dialog.ShowDialog().GetValueOrDefault(false);
                                newAccessPassword = dialog.Password;
                            });
                            if (dialogResult)
                            {
                                if (!TagOperationsViewModel.ApplyLockAction
                                    (new Gen2.LockAction(Gen2.LockAction.ACCESS_UNLOCK), newAccessPassword))
                                {
                                    // failed again
                                    // check if epc is locked
                                    // if locked -> invalid chip -> continue
                                    newAccessPassword = "";
                                    if (TagOperationsViewModel.CheckEpcIsLocked(newAccessPassword))
                                    {
                                        StatusBarText = "Invalid chip, continue...";
                                        StatusBarBackground = Brushes.Red;
                                        Speak("Invalid chip");
                                        Thread.Sleep(300);
                                        continue;
                                    }
                                        //if not locked ask for continue without locking
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
                            else
                            {
                                if (MessageBox.Show("Continue without locking?", "Question",
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

                    //var isLockingNeeded = _totalRaceInfo.AccessPassword != "00000000";
                    if (!IsEncoding) return;
                    var currentAP = _totalRaceInfo.AccessPassword;
                    //4b. if access password is not locked, encode access password = 
                    //#8 digits from access password dialogue needed in 'new project' screen# .
                    // we can write a new access password to the chip here
                    if (!apLocked && _totalRaceInfo.SetNewAccessPassword) // && isLockingNeeded)
                    {
                        if (TagOperationsViewModel.WriteAccessPassword(_totalRaceInfo.NewAccessPassword))
                            currentAP = _totalRaceInfo.NewAccessPassword;
                        //our current access password has changed
                    }

                    // 5. Unlock EPC and encode tag to proper number
                    TagOperationsViewModel.ApplyLockAction(
                        new Gen2.LockAction(Gen2.LockAction.EPC_UNLOCK), currentAP);

                    var encoded = TagOperationsViewModel.WriteTag(NextTagNumber);

                    if (encoded && !apLocked) // && isLockingNeeded)
                    {
                        //6. lock epc memory (tag) with write lock. Gen2.LockAction.EPC_LOCK
                        TagOperationsViewModel.ApplyLockAction(
                            new Gen2.LockAction(Gen2.LockAction.EPC_LOCK), currentAP);

                        //7. lock access password with read/write lock. Gen2.LockAction.ACCESS_LOCK 
                        TagOperationsViewModel.ApplyLockAction(
                            new Gen2.LockAction(Gen2.LockAction.ACCESS_LOCK), currentAP);

                        // if we have a kill password
                        if (!string.IsNullOrEmpty(_totalRaceInfo.KillPassword))
                        {
                            //8. set kill password =#8 digits from kill password dialogue needed in 'new project' screen#
                            TagOperationsViewModel.WriteKillPassword(_totalRaceInfo.KillPassword);

                            //9. lock kill password with read/write lock. Gen2.LockAction.LILL_LOCK
                            TagOperationsViewModel.ApplyLockAction(
                                new Gen2.LockAction(Gen2.LockAction.KILL_LOCK), currentAP);
                        }

                        // if we chose a permalock feature
                        if (_totalRaceInfo.Permalock)
                        {
                            TagOperationsViewModel.ApplyPermalockAction(currentAP);
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
            var s1 = digits.First() + " " + digits.Last();
            Speak(s1);
//			Speak(digits.First().ToString());
//            Speak(digits.Last().ToString());
        }

        private void Speak(string str)
        {
            using (var ss = new SpeechSynthesizer {Rate = 5})
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

        #endregion
    }
}