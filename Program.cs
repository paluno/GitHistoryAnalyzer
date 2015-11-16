using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ExpertiseExplorer.Algorithms.RepositoryManagement;

namespace GitHistoryAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2 && args.Length != 3 ||
                !File.Exists(args[0]))
            {
                PrintUsage();
            }
            else
            {
                string[] gitLog = File.ReadAllLines(args[0]);

                DateTime lastDate = DateTime.MinValue;
                ISet<string> existingDevelopers = new HashSet<string>();    // these are no newcomers

                AliasFinder af = new AliasFinder();
                if (args.Length == 3)
                {
                    if (!File.Exists(args[2]))
                    {       // author list does not exist yet, will be created
                        IEnumerable<IEnumerable<string>> authorAliasList =
                            af.Consolidate(
                                gitLog
                                    .Where(line => line.StartsWith("Author: "))
                                    .Select(line => line.Substring("Author: ".Length))
                                    .ToArray()
                            );
                        File.WriteAllLines(args[2],
                                authorAliasList
                                    .Select(aliases => string.Join(";", aliases))
                                    .OrderBy(s => s)
                            );
                    }
                    af.InitializeMappingFromAuthorList(File.ReadAllLines(args[2]));
                }

                using (StreamWriter swOutputCSV = File.CreateText(args[1]))
                {
                    swOutputCSV.WriteLine("author;date");

                    foreach (string logLine in gitLog.Reverse())    // proceed chronologically
                    {
                        if (logLine.StartsWith("Date: "))
                            if (lastDate != DateTime.MinValue)
                                throw new InvalidDataException("Two dates without author in between!");
                            else
                                lastDate = DateTime.Parse(logLine.Substring("Date: ".Length));

                        if (logLine.StartsWith("Author: "))
                        {
                            if (lastDate == DateTime.MinValue)
                                throw new InvalidDataException("Author without date!");
                            else foreach (string deanonymizedAuthor in af.DeanonymizeAuthor(logLine.Substring("Author: ".Length)))
                                if (existingDevelopers.Add(deanonymizedAuthor))
                                    swOutputCSV.WriteLine(deanonymizedAuthor + ";" + lastDate.ToString("u"));
                            lastDate = DateTime.MinValue;
                        }
                    }
                }


            }
        }

        static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("FindGitNewcomers.exe -- find all contributors to a git repository from its log and the date of their first commit");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("FindGitNewcomers GITLOGFILE OUTPUTCSVFILE [AUTHORLIST]");
            Console.WriteLine();
        }
    }
}
