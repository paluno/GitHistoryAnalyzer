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
        public enum Operation { FindNewcomers, CountContributors, CountActiveContributors };
        
        static void Main(string[] args)
        {
            Operation currentOperation;

            if (args.Length != 3 && args.Length != 4 ||
                !Enum.TryParse<Operation>(args[0], true, out currentOperation) ||
                !File.Exists(args[1]))
            {
                PrintUsage();
            }
            else
            {
                string gitLogFile = args[1];
                string outputCsvFile = args[2];

                string[] gitLog = File.ReadAllLines(gitLogFile);

                AliasFinder af = new AliasFinder();
                if (args.Length == 4)
                {
                    string authorList = args[3];

                    if (!File.Exists(authorList))
                    {       // author list does not exist yet, will be created
                        IEnumerable<IEnumerable<string>> authorAliasList =
                            af.Consolidate(
                                gitLog
                                    .Where(line => line.StartsWith("Author: "))
                                    .Select(line => line.Substring("Author: ".Length))
                                    .ToArray()
                            );
                        File.WriteAllLines(authorList,
                                authorAliasList
                                    .Select(aliases => string.Join(";", aliases))
                                    .OrderBy(s => s)
                            );
                    }
                    af.InitializeMappingFromAuthorList(File.ReadAllLines(authorList));
                }

                switch (currentOperation)
                {
                    case Operation.FindNewcomers:
                        FindNewcomers(outputCsvFile, gitLog, af);
                        break;
                    case Operation.CountContributors:
                        CountContributors(outputCsvFile, gitLog, af);
                        break;
                    case Operation.CountActiveContributors:
                        CountActiveContributors(outputCsvFile, gitLog, af);
                        break;
                    default:
                        throw new NotImplementedException("Operation " + currentOperation + " is not yet implemented.");
                }
            }
        }

        private static void CountActiveContributors(string outputCsvFile, string[] gitLog, AliasFinder af)
        {
            ISet<string> devsOfTheMonth = new HashSet<string>();
            DateTime currentMonth = DateTime.MinValue;
            DateTime lastDate = DateTime.MinValue;

            using (StreamWriter swOutputCSV = File.CreateText(outputCsvFile))
            {
                swOutputCSV.WriteLine("month;number of active contributors");

                foreach (string logLine in gitLog.Reverse())    // proceed chronologically
                {
                    if (logLine.StartsWith("Date: "))
                        if (lastDate != DateTime.MinValue)
                            throw new InvalidDataException("Two dates without author in between!");
                        else
                        {
                            lastDate = DateTime.Parse(logLine.Substring("Date: ".Length));
                            if (lastDate.Month != currentMonth.Month || lastDate.Year != currentMonth.Year)
                            {

                                swOutputCSV.WriteLine(currentMonth.ToString("yyyy-MM") + ";" + devsOfTheMonth.Count);
                                devsOfTheMonth.Clear();
                                currentMonth = lastDate;
                            }
                        }

                    if (logLine.StartsWith("Author: "))
                    {
                        if (lastDate == DateTime.MinValue)
                            throw new InvalidDataException("Author without date!");
                        else
                            foreach (string deanonymizedAuthor in af.DeanonymizeAuthor(logLine.Substring("Author: ".Length)))
                                devsOfTheMonth.Add(deanonymizedAuthor);
                        lastDate = DateTime.MinValue;
                    }
                }
                swOutputCSV.WriteLine(currentMonth.ToString("yyyy-MM") + ";" + devsOfTheMonth.Count);
            }
        }

        private static void CountContributors(string outputCsvFile, string[] gitLog, AliasFinder af)
        {
            File.WriteAllText(
                outputCsvFile,
                "author;commmits\r\n" +         // a header for the CSV
                string.Join("\r\n",             // one row per author
                    gitLog
                        .Where(logLine => logLine.StartsWith("Author: "))                                   // find all authors
                        .SelectMany(logLine => af.DeanonymizeAuthor(logLine.Substring("Author: ".Length)))  // consolidate aliases
                        .GroupBy(x => x)                                                                    // count by author
                        .Select(group => group.Key + ";" + group.Count())                                   // bring to CSV format
                    )
            );
        }

        private static void FindNewcomers(string outputFileName, string[] gitLog, AliasFinder af)
        {
            ISet<string> existingDevelopers = new HashSet<string>();    // these are no newcomers
            DateTime lastDate = DateTime.MinValue;

            using (StreamWriter swOutputCSV = File.CreateText(outputFileName))
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
                        else
                            foreach (string deanonymizedAuthor in af.DeanonymizeAuthor(logLine.Substring("Author: ".Length)))
                                if (existingDevelopers.Add(deanonymizedAuthor))
                                    swOutputCSV.WriteLine(deanonymizedAuthor + ";" + lastDate.ToString("u"));
                        lastDate = DateTime.MinValue;
                    }
                }
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("GitHistoryAnalyzer.exe -- Analyzes a git history log as generated by TortoiseGit (Copy & Paste)");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("GitHistoryAnalyzer Operation GITLOGFILE OUTPUTCSVFILE [AUTHORLIST]");
            Console.WriteLine();
            Console.WriteLine("Possible Operations:");
            Console.WriteLine(" 1. FindNewcomers     - Find all contributors to a git repository by the date of their first commit");
            Console.WriteLine(" 2. CountContributors - Count the all time number of commits for each contributor");
            Console.WriteLine(" 3. CountActiveContributors - For every month, count the number unique contributors");

            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine(" GITLOGFILE      - Path to a git log file as generated from TortoiseGit per Copy & Paste");
            Console.WriteLine(" OUTPUTCSVFILE   - Path to which results will be written as CSV; overwrites existing files");
            Console.WriteLine(" AUTHORLIST      - Path to a file with authors; one author per row, semicolons separate synonyms;");
            Console.WriteLine("                   Will be created with all heuristically deanonymized author names if it does not");
            Console.WriteLine("                   exist already.");
        }
    }
}
