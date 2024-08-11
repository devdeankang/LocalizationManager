using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LocalizationManager
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(App));

        public App()
        {
            this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Exception ex = default(Exception);
            ex = (Exception)e.Exception;

            log.Error(string.Format("[{0}] {1}\n{2}", "TaskScheduler_UnobservedTaskException", ex.Message, ex.StackTrace));
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = default(Exception);
            ex = (Exception)e.Exception;

            log.Error(string.Format("[{0}] {1}\n{2}", "Current_DispatcherUnhandledException", ex.Message, ex.StackTrace));
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = default(Exception);
            ex = (Exception)e.ExceptionObject;

            log.Error(string.Format("[{0}] {1}\n{2}", "CurrentDomain_UnhandledException", ex.Message, ex.StackTrace));
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = default(Exception);
            ex = (Exception)e.Exception;

            log.Error(string.Format("[{0}] {1}\n{2}", "Dispatcher_UnhandledException", ex.Message, ex.StackTrace));
        }
    }
}
