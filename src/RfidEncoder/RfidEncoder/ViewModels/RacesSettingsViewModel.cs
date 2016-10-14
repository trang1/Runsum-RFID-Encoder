using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace RfidEncoder.ViewModels
{
    /// <summary>
    ///     Viewmodel for editing the project settings
    /// </summary>
    public class RacesSettingsViewModel : ViewModelBase
    {
        public RacesSettingsViewModel(TotalRaceInfo totalRaceInfo)
        {
            TotalRaceInfo = new TotalRaceInfo(totalRaceInfo);

            SaveCommand = new DelegateCommand(Save);
            ChooseFileCommand = new DelegateCommand(ChooseFile);
        }

        public TotalRaceInfo TotalRaceInfo { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand ChooseFileCommand { get; set; }

        private void ChooseFile()
        {
            var dialog = new OpenFileDialog();
            dialog.CheckFileExists = false;
            dialog.FileName = TotalRaceInfo.FileName;
            if (dialog.ShowDialog().GetValueOrDefault(false))
            {
                TotalRaceInfo.FileName = dialog.FileName;
            }
        }

        private void Save()
        {
            if (TotalRaceInfo.StartNumber >= TotalRaceInfo.EndNumber)
            {
                MessageBox.Show("End number must be greater than start number.", "Error");
                return;
            }

            if (!IsPathValid(TotalRaceInfo.FileName))
            {
                if (MessageBox.Show("File path is invalid. Continue anyway?", "Question",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    TotalRaceInfo.FileName = null;
                }
                else
                {
                    return;
                }
            }

            if (TotalRaceInfo.StartNumber.ToString().Length > TotalRaceInfo.CodeLength ||
                TotalRaceInfo.EndNumber.ToString().Length > TotalRaceInfo.CodeLength)
            {
                MessageBox.Show("Code length is too small.", "Error");
                return;
            }

            int ap;
            if (TotalRaceInfo.AccessPassword.Length < 8 || !int.TryParse(TotalRaceInfo.AccessPassword, out ap))
            {
                MessageBox.Show("The access password has to be minimum 8 digits long.", "Error");
                return;
            }

            if (FrameworkElement is Window)
                ((Window) FrameworkElement).DialogResult = true;
        }

        public bool IsPathValid(string pathString)
        {
            Uri pathUri;
            var isValidUri = Uri.TryCreate(pathString, UriKind.Absolute, out pathUri);

            return isValidUri && pathUri != null && pathUri.IsLoopback;
        }
    }
}