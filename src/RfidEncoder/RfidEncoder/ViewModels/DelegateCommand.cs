using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace RfidEncoder.ViewModels
{
    /// <summary>
    /// Provides an ICommand whose delegates do not take any parameters for Execute() and CanExecute()
    /// </summary>
    public class DelegateCommand : ViewModelBase, ICommand
    {
        private readonly Func<object, bool> _canExecute;
        private readonly Action<object> _execute;
        private readonly Func<string> _getUiName;
        private readonly Func<Visibility> _getVisibility;

        public bool CanProvideUiName
        {
            get { return _getUiName != null; }
        }

        public bool CanProvideVisibility
        {
            get { return _getVisibility != null; }
        }

        public virtual event EventHandler CanExecuteChanged
        {
            add
            {
                if (this._canExecute != null)
                {
                    CommandManager.RequerySuggested += value;
                }
            }
            remove
            {
                if (this._canExecute != null)
                {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }

        public DelegateCommand(Action execute, Func<bool> canExecute = null, Func<string> getUiName = null, Func<Visibility> getVisibility = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
            this._execute = parameter => execute();
            this._canExecute = parameter => canExecute == null || canExecute();
            _getUiName = getUiName;
            _getVisibility = getVisibility;
        }

        public bool CanExecute(object parameter)
        {
            this.OnPropertyChanged(() => UiName);
            this.OnPropertyChanged(() => Visibility);

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return _canExecute == null || _canExecute(parameter);

            return true;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
            RaiseCanExecuteChanged();
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public virtual string UiName
        {
            get { return _getUiName(); }
        }

        public virtual Visibility Visibility
        {
            get { return _getVisibility(); }
        }
    }
}
