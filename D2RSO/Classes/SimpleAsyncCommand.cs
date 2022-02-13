using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace D2RSO.Classes
{
    public sealed class SimpleAsyncCommand : ICommand
    {
        public Func<object, Task<bool>> CanExecuteDelegate { get; set; }
        public Func<object, bool> CanExecuteDelegateEx { get; set; }
        public Func<object, Task> ExecuteDelegate { get; set; }

        public SimpleAsyncCommand(Func<object, Task<bool>> can, Func<object, Task> ex)
        {
            CanExecuteDelegate = can;
            ExecuteDelegate = ex;
        }

        public SimpleAsyncCommand(Func<object, bool> can, Func<object, Task> ex)
        {
            CanExecuteDelegateEx = can;
            ExecuteDelegate = ex;
        }

        #region ICommand Members

        public bool CanExecute()
        {
            return CanExecute(null);
        }

        public bool CanExecute(object parameter)
        {
            if(CanExecuteDelegateEx != null)
                return CanExecuteDelegateEx(parameter);

            return CanExecuteDelegate == null || CanExecuteDelegate(parameter).GetAwaiter().GetResult();
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
            ExecuteDelegate(parameter).GetAwaiter().GetResult();
            if (Executed != null)
                Executed(this, null);
        }

        #endregion
    }
}
