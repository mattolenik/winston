using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.GotDotNet;

namespace Winston.User
{
    class ConsoleUserAdapter : IUserAdapter
    {
        readonly TextWriter output;
        readonly TextReader input;
        readonly object progressLock = new object();
        int lastProgressRow;
        int lastPrintRow;
        readonly int startRow;

        public ConsoleUserAdapter(TextWriter output, TextReader input)
        {
            this.output = output;
            this.input = input;
            if (Environment.UserInteractive)
            {
                this.startRow = Console.CursorTop;
            }
        }

        public async Task<string> Ask(Question question) => await Task.Run(() =>
        {
            output.WriteLine(question.Preamble);
            bool match = false;
            string response = null;
            while (!match)
            {
                output.WriteLine(question.Query);
                response = input.ReadLine()?.Trim();
                match = question.Answers.Any(a => a.EqualsOrdIgnoreCase(response));
                if (!match)
                {
                    var answers = string.Join(", ", question.Answers);
                    output.WriteLine($"\nAnswer must be one of these values: {answers}");
                }
            }
            return response;
        });

        public void Message(string message)
        {
            if (Environment.UserInteractive)
            {
                ConsoleEx.Move(0, startRow + lastPrintRow + lastProgressRow);
            }
            output.WriteLine(message);
            lastPrintRow++;
        }

        public Progress NewProgress(string name)
        {
            var result = new Progress { Name = name };
            if (!Environment.UserInteractive)
            {
                return result;
            }
            result.UpdateDownload = p =>
            {
                lock (progressLock)
                {
                    if (p != result.Last)
                    {
                        ConsoleEx.WriteAt(result.ProgressPrefix.Length, result.Row, p.ToString());
                        result.Last = p;
                    }
                }
            };
            result.CompletedDownload = () =>
            {
                lock (progressLock)
                {
                    ConsoleEx.WriteAt(result.ProgressPrefix.Length + 3, result.Row, " completed");
                }
            };
            result.UpdateInstall = p =>
            {
                lock (progressLock)
                {
                    ConsoleEx.WriteAt(result.ProgressPrefix.Length + 3, result.Row, p.ToString());
                    result.Last = p;
                }
            };
            result.CompletedInstall = () =>
            {
                lock (progressLock)
                {
                    ConsoleEx.WriteAt(result.ProgressPrefix.Length + 3, result.Row, " completed");
                }
            };
            result.Row = startRow + lastProgressRow;
            result.ProgressPrefix = result.Name + ": ";
            ConsoleEx.WriteAt(0, result.Row, result.ProgressPrefix);
            lastProgressRow++;
            return result;
        }
    }
}