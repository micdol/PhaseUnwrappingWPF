using System;
using System.Windows.Input;

namespace PhaseUnwrapping
{
    public class RelayCommand : ICommand
    {
        private Action<object> mParamAction = null;
        private Action mAction = null;

        public event EventHandler CanExecuteChanged = (sender, e) => { };

        public RelayCommand(Action a) => mAction = a;

        public RelayCommand(Action<object> a) => mParamAction = a;

        public bool CanExecute(object parameter) => parameter == null ? mAction != null : mParamAction != null;

        public void Execute(object parameter)
        {
            mAction?.Invoke();
            mParamAction?.Invoke(parameter);
        }
    }
}
