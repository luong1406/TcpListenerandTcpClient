using NLog.Config;
using NLog.Targets;
using NLog;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure NLog
            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget("logfile")
            {
                FileName = "logs/${shortdate}.log",
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${exception:format=tostring}"
            };
            config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);
            LogManager.Configuration = config;

            Logger.Info("Application started");
        }
    }

}
