using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scripty.Core;
using Scripty.Core.Output;

namespace Scripty
{
    public class Program
    {
        private static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEvent;
            var program = new Program();
            return program.Run(args);
        }

        private static void UnhandledExceptionEvent(object sender, UnhandledExceptionEventArgs e)
        {
            // Exit with a error exit code
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                Console.Error.WriteLine(exception.ToString());
            }
            Environment.Exit((int) ExitCode.UnhandledError);
        }

        private readonly Settings _settings = new Settings();

        private int Run(string[] args)
        {
            // Parse the command line
            try
            {
                bool hasParseArgsErrors;
                if (!_settings.ParseArgs(args, out hasParseArgsErrors))
                {
                    return hasParseArgsErrors ? (int)ExitCode.CommandLineError : (int)ExitCode.Normal;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return (int)ExitCode.CommandLineError;
            }

            // Setup all the script sources and evaluation tasks
            var engine = new ScriptEngine(_settings.ProjectFilePath);
            var tasks = new ConcurrentBag<Task<ScriptResult>>();
            Parallel.ForEach(_settings.ScriptFilePaths
                .Select(x => Path.Combine(Path.GetDirectoryName(_settings.ProjectFilePath), x))
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(),
                x =>
                {
                    if (File.Exists(x))
                    {
                        tasks.Add(engine.Evaluate(new ScriptSource(x, File.ReadAllText(x))));
                    }
                });

            // Evaluate all the scripts
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException aggregateException)
            {
                foreach (var ex in aggregateException.InnerExceptions)
                {
                    Console.Error.WriteLine(ex.ToString());
                }
            }

            // Iterate over the completed tasks
            foreach (var task in tasks.Where(x => x.Status == TaskStatus.RanToCompletion))
            {
                // Check for any errors
                foreach (var error in task.Result.Errors)
                {
                    Console.Error.WriteLine($"{error.Message} [{error.Line},{error.Column}]");
                }

                // Output the set of generated files w/ build actions
                foreach (var outputFile in task.Result.OutputFiles)
                {
                    Console.WriteLine($"{outputFile.BuildAction}|{outputFile.FilePath}");
                }
            }

            return (int) ExitCode.Normal;
        }
    }
}
