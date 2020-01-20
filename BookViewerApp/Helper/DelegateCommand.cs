﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp
{
    public class DelegateCommand : System.Windows.Input.ICommand
    {
        public event EventHandler CanExecuteChanged;

        public Func<object, bool> CanExecuteDelegate;
        public Action<object> ExecuteDelegate;

        public DelegateCommand( Action<object> executeDelegate, Func<object, bool> canExecuteDelegate=null)
        {
            ExecuteDelegate = executeDelegate ?? throw new ArgumentNullException(nameof(executeDelegate));
            CanExecuteDelegate = canExecuteDelegate;
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteDelegate?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            ExecuteDelegate?.Invoke(parameter);
        }
    }
}