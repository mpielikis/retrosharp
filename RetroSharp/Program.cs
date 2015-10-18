using Microsoft.CodeAnalysis.MSBuild;
using NDesk.Options;
using System;
using System.Threading.Tasks;

namespace RetroSharp
{
    static class Program
    {
        static void Main(string[] args)
        {
            var showHelp = false;
            var solutionPath = string.Empty;

            var options = new OptionSet
            {
                {
                        "s|solution=", "The path to solution file",
                        t => solutionPath = t
                },
                {
                        "h|help", "Show this message and exit",
                        h => showHelp = h != null
                },
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try 'RetroSharp --help' for more information.");
                return;
            }

            showHelp |= string.IsNullOrEmpty(solutionPath);

            if (showHelp)
            {
                ShowHelp(options);
                return;
            }

            var retro = RetroSolution(solutionPath);

            retro.Wait();

            if (retro.IsFaulted)
            {
                Console.WriteLine(retro.Exception);
                Environment.ExitCode = 1;
            }
            else
            {
                Environment.ExitCode = 0;
            }
        }

        static async Task RetroSolution(string solutionPath)
        {
            var ws = MSBuildWorkspace.Create();

            var solution = await ws.OpenSolutionAsync(solutionPath);

            foreach (var prj in solution.Projects)
            {
                var retroProject = await Generator.MakeRetro(prj);

                if (retroProject != prj)
                    ws.TryApplyChanges(retroProject.Solution);
            } 
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: RetroSharp [-s Solution Path]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }
}
