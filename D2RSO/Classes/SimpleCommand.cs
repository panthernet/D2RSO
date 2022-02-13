using System;
using System.Windows.Input;

namespace D2RSO.Classes
{
    public sealed class SimpleCommand : ICommand
    {
        public Predicate<object> CanExecuteDelegate { get; set; }
        public Action<object> ExecuteDelegate { get; set; }

        public SimpleCommand(Predicate<object> can, Action<object> ex)
        {
            CanExecuteDelegate = can;
            ExecuteDelegate = ex;
        }

        #region ICommand Members

        public bool CanExecute()
        {
            return CanExecute(null);
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteDelegate == null || CanExecuteDelegate(parameter);
        }

        public event EventHandler Executed;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void Execute()
        {
            Execute(null);
        }

        public void Execute(object parameter)
        {
            if (ExecuteDelegate == null) return;
            ExecuteDelegate(parameter);
            if (Executed != null)
                Executed(this, null);
        }

        #endregion
    }
}
