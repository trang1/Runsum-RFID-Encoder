using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using RfidEncoder.ViewModels;

namespace RfidEncoder
{
    public class RaceGridView: DataGrid
    {
        #region DependencyProperties
        public ICommand SelectionChangedCommand
        {
            get { return (ICommand)GetValue(SelectionChangedCommandProperty); }
            set { SetValue(SelectionChangedCommandProperty, value); }
        }

        public static readonly DependencyProperty SelectionChangedCommandProperty =
        DependencyProperty.Register(
            "SelectionChangedCommand",
            typeof(ICommand),
            typeof(RaceGridView)
        );
        public int SelectedTagIndex
        {
            get { return (int)GetValue(SelectedTagIndexProperty); }
            set { SetValue(SelectedTagIndexProperty, value); }
        }

        public static readonly DependencyProperty SelectedTagIndexProperty =
        DependencyProperty.Register(
            "SelectedTagIndex", 
            typeof(int),
            typeof(RaceGridView)
        );
        public ICommand SelectedTagChangedCommand
        {
            get { return (ICommand)GetValue(SelectedTagChangedCommandProperty); }
            set { SetValue(SelectedTagChangedCommandProperty, value); }
        }

        public static readonly DependencyProperty SelectedTagChangedCommandProperty =
        DependencyProperty.Register(
            "SelectedTagChangedCommand",
            typeof(ICommand),
            typeof(RaceGridView)
        );
        #endregion

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            if (newValue is TotalRaceInfo)
            {
                this.Columns.Clear();

                FillColumns(newValue as TotalRaceInfo);
            }
        }

        private void FillColumns(TotalRaceInfo totalRaceInfo)
        {
            var raceColumn = new DataGridTextColumn();
            raceColumn.Header = "Race #";
            raceColumn.Binding = new Binding("RaceNumber");
            Columns.Add(raceColumn);

            for (int index = 0; index < totalRaceInfo.TagsPerRaceCount; index++)
            {
                var column = new DataGridTextColumn();
                column.Header = "Tag " + (index + 1) + " code";
                column.Binding = new Binding("TagList["+index+"]");

                Columns.Add(column);
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if(SelectionChangedCommand != null && SelectionChangedCommand.CanExecute(null))
                SelectionChangedCommand.Execute(null);
        }

        protected override void OnCurrentCellChanged(EventArgs e)
        {
            base.OnCurrentCellChanged(e);

            var cell = CurrentCell;

            if (cell.Column != null)
            {
                SelectedTagIndex = cell.Column.DisplayIndex - 1;

                if (SelectedTagChangedCommand != null && SelectedTagChangedCommand.CanExecute(null))
                    SelectedTagChangedCommand.Execute(null);
            }
        }
    }
}
