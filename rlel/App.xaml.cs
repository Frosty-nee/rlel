using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;

namespace rlel {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        /// <summary>The unique mutex name.</summary>
        private const string UniqueMutexName = "{2E0604DE-AF17-4FD9-89F9-5762BD2030E8}";
        private static SingleInstance singleInstance;

        /// <summary>The app on startup.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void AppOnStartup (object sender, StartupEventArgs e) {
            if (singleInstance == null) {
                Guid guid = new Guid(UniqueMutexName);
                singleInstance = new SingleInstance(guid);
            }
            if (singleInstance.IsFirstInstance) {
                singleInstance.ArgumentsReceived += singleInstance_ArgumentsReceived;
                singleInstance.ListenForArgumentsFromSuccessiveInstances( );

                MainWindow mainView = new MainWindow();
                mainView.Show();
                mainView.addCommandLineArgs(Environment.GetCommandLineArgs( ));
            }
            else {
                // Pass Arguments to first Instance
                singleInstance.PassArgumentsToFirstInstance(Environment.GetCommandLineArgs( ));
                // Terminate this instance.
                this.Shutdown( );
            }

            return;
        }

        private void singleInstance_ArgumentsReceived (object sender, ArgumentsReceivedEventArgs e) {
            Current.Dispatcher.BeginInvoke(
                                (Action)(() => ((MainWindow)Current.MainWindow).addCommandLineArgs(e.Args)));
        }


        private void AppOnExit (object sender, ExitEventArgs e) {
            // make sure the used mutex is not collected, otherwise another instance can start.
            GC.KeepAlive(singleInstance);
        }

    }
}
