using CommandLine.Text;
using CommandLine;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VstsSyncMigrator.Engine;
using VstsSyncMigrator.Engine.ComponentContext;
using System.IO;
using VstsSyncMigrator.Engine.Configuration;
using VstsSyncMigrator.Engine.Configuration.FieldMap;
using VstsSyncMigrator.Engine.Configuration.Processing;
using Microsoft.ApplicationInsights.DataContracts;
using NuGet;
using System.Net.NetworkInformation;
using VstsSyncMigrator.Commands;

namespace VstsSyncMigrator.ConsoleApp
{
    public class Program
    {
        [Verb("init", HelpText = "Creates initial config file")]
        class InitOptions
        {
            //normal options here
        }
        [Verb("execute", HelpText = "Record changes to the repository.")]
        class RunOptions
        {
            [Option('c', "config", Required = true, HelpText = "Configuration file to be processed.")]
            public string ConfigFile { get; set; }
        }

        static DateTime startTime = DateTime.Now;
        static Stopwatch mainTimer = new Stopwatch();


        public static int Main(string[] args)
        {
            mainTimer.Start();
            Telemetry.Current.TrackEvent("ApplicationStart");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;            
            /////////////////////////////////////////////////
            var logsPath = CreateLogsPath();
            //////////////////////////////////////////////////
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Trace.Listeners.Add(new TextWriterTraceListener(Path.Combine(logsPath, "migration.log"), "myListener"));
            //////////////////////////////////////////////////
            ///
            
            Trace.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, "[Info]");
            var thisVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Trace.WriteLine(string.Format("Running version detected as {0}", thisVersion), "[Info]");
            if (IsOnline())
            {
                var latestVersion = GetLatestVersion();
                Trace.WriteLine(string.Format("Latest version detected as {0}", latestVersion), "[Info]");
                if (latestVersion > thisVersion)
                {
                    Trace.WriteLine(
                        string.Format("You are currently running version {0} and a newer version ({1}) is available. You should upgrade now using Chocolatey command 'choco upgrade vsts-sync-migrator' from the command line.",
                        thisVersion, latestVersion
                        ),
                        "[Warning]");
#if !DEBUG

                    Console.WriteLine("Do you want to continue? (y/n)");
                    if (Console.ReadKey().Key != ConsoleKey.Y)
                    {
                        Trace.WriteLine("User aborted to update version", "[Warning]");
                        return 2;
                    }
#endif
                }
            }
            
            Trace.WriteLine(string.Format("Telemitery Enabled: {0}", Telemetry.Current.IsEnabled().ToString()), "[Info]");
            Trace.WriteLine("Telemitery Note: We use Application Insights to collect telemitery on perfomance & feature usage for the tools to help our developers target features. This data us tied to a session ID that is generated and shown in the logs. This can help with debugging.");
            Trace.WriteLine(string.Format("SessionID: {0}", Telemetry.Current.Context.Session.Id), "[Info]");
            Trace.WriteLine(string.Format("User: {0}", Telemetry.Current.Context.User.Id), "[Info]");
            Trace.WriteLine(string.Format("Start Time: {0}", startTime.ToUniversalTime().ToLocalTime()), "[Info]");
            Trace.WriteLine("------------------------------START-----------------------------", "[Info]");
            //////////////////////////////////////////////////
            var result = (int)Parser.Default.ParseArguments<InitOptions, RunOptions, ExportADGroupsOptions>(args).MapResult(
                (InitOptions opts) => RunInitAndReturnExitCode(opts),
                (RunOptions opts) => RunExecuteAndReturnExitCode(opts),
                (ExportADGroupsOptions opts) => ExportADGroupsCommand.Run(opts, logsPath),
                errs => 1);
            //////////////////////////////////////////////////
            Trace.WriteLine("-------------------------------END------------------------------", "[Info]");
            mainTimer.Stop();
            Telemetry.Current.TrackEvent("ApplicationEnd", null,
                new Dictionary<string, double> {
                        { "ApplicationDuration", mainTimer.ElapsedMilliseconds }
                });
            if (Telemetry.Current != null)
            {
                Telemetry.Current.Flush();
                // Allow time for flushing:
                System.Threading.Thread.Sleep(1000);
            }
            Trace.WriteLine(string.Format("Duration: {0}", mainTimer.Elapsed.ToString("c")), "[Info]");
            Trace.WriteLine(string.Format("End Time: {0}", DateTime.Now.ToUniversalTime().ToLocalTime()), "[Info]");
#if DEBUG
            Console.ReadKey();
#endif
            return result;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var excTelemetry = new ExceptionTelemetry((Exception)e.ExceptionObject);
            excTelemetry.SeverityLevel = SeverityLevel.Critical;
            excTelemetry.HandledAt = ExceptionHandledAt.Unhandled;
            Telemetry.Current.TrackException(excTelemetry);
            Telemetry.Current.Flush();
            System.Threading.Thread.Sleep(1000);
        }

        private static object RunExecuteAndReturnExitCode(RunOptions opts)
        {
            Telemetry.Current.TrackEvent("ExecuteCommand");
            EngineConfiguration ec;
            if (opts.ConfigFile == string.Empty)
            {
                opts.ConfigFile = "configuration.json";
            }

            if (!File.Exists(opts.ConfigFile))
            {
                Trace.WriteLine("The config file does not exist, nor doe the default 'configuration.json'. Use 'init' to create a configuration file first", "[Error]");
                return 1;
            }
            else
            {
                Trace.WriteLine("Loading Config");
                var sr = new StreamReader(opts.ConfigFile);
                var configurationjson = sr.ReadToEnd();
                sr.Close();
                ec = JsonConvert.DeserializeObject<EngineConfiguration>(configurationjson, 
                    new FieldMapConfigJsonConverter(),
                    new ProcessorConfigJsonConverter());
            }
            Trace.WriteLine("Config Loaded, creating engine", "[Info]");
            var me = new MigrationEngine(ec);
            Trace.WriteLine("Engine created, running...", "[Info]");
            me.Run();
            Trace.WriteLine("Run complete...", "[Info]");
            return 0;
        }

        private static object RunInitAndReturnExitCode(InitOptions opts)
        {
            Telemetry.Current.TrackEvent("InitCommand");
            if (!File.Exists("configuration.json"))
            {
                var json = JsonConvert.SerializeObject(EngineConfiguration.GetDefault(),
                    new FieldMapConfigJsonConverter(),
                    new ProcessorConfigJsonConverter());
                var sw = new StreamWriter("configuration.json");
                sw.WriteLine(json);
                sw.Close();
                Trace.WriteLine("New configuration.json file has been created", "[Info]");
            }
            return 0;
        }

        private static Version GetLatestVersion()
        {
            var startTime = DateTime.Now;
            var mainTimer = new Stopwatch();
            mainTimer.Start();
            //////////////////////////////////
            var packageID = "vsts-sync-migrator";
            var version = SemanticVersion.Parse("0.0.0.0");
            var sucess = false;
            try
            {
                //Connect to the official package repository
                var repo = PackageRepositoryFactory.Default.CreateRepository("https://chocolatey.org/api/v2/");
                version = repo.FindPackagesById(packageID).Max(p => p.Version);
                sucess = true;
            }
            catch (Exception ex)
            {
                Telemetry.Current.TrackException(ex);
                sucess = false;
            }
            /////////////////
            mainTimer.Stop();
            Telemetry.Current.TrackDependency(new DependencyTelemetry("PackageRepository", "chocolatey.org", "vsts-sync-migrator", version.ToString(), startTime, mainTimer.Elapsed, null, sucess));
            return new Version(version.ToString());
        }

        private static bool IsOnline()
        {
            var startTime = DateTime.Now;
            var mainTimer = new Stopwatch();
            mainTimer.Start();
            //////////////////////////////////
            var isOnline = false;
            var responce = "none";
            try
            {
                var myPing = new Ping();
                var host = "8.8.4.4";
                var buffer = new byte[32];
                var timeout = 1000;
                var pingOptions = new PingOptions();
                var reply = myPing.Send(host, timeout, buffer, pingOptions);
                responce = reply.Status.ToString();
                if (reply.Status == IPStatus.Success)
                {
                    isOnline = true;
                }
            }
            catch (Exception ex)
            {
                // Likley no network is even available
                Telemetry.Current.TrackException(ex);
                responce = "error";
                isOnline = false;
            }
            /////////////////
            mainTimer.Stop();
            Telemetry.Current.TrackDependency(new DependencyTelemetry("Ping","GoogleDNS", "IsOnline", null, startTime, mainTimer.Elapsed, responce, true));
            return isOnline;
        }

        private static string CreateLogsPath()
        {
            string exportPath;
            var assPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            exportPath = Path.Combine(Path.GetDirectoryName(assPath), "logs", DateTime.Now.ToString("yyyyMMddHHmmss"));
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            return exportPath;
        }
    }
}
