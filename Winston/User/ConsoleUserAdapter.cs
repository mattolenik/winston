using System;
using System.IO;
using System.Linq;
using System.Threading;
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
        int startRow;

        /// <summary>
        /// It seems to be impossible to predict whether or not the program runs in an actual
        /// terminal window, so provide a wrapper for terminal-specific operations that will
        /// fail when not inside a terminal. E.g. moving the cursor on screen for progress reporting.
        /// </summary>
        /// <param name="a">function to run</param>
        static void Try(Action a)
        {
            try
            {
                a?.Invoke();
            }
#pragma warning disable CC0004 // Catch block cannot be empty
            catch { }
#pragma warning restore CC0004 // Catch block cannot be empty
        }

        public ConsoleUserAdapter(TextWriter output, TextReader input)
        {
            this.output = output;
            this.input = input;
            Try(() => this.startRow = Console.CursorTop);
        }

        public async Task<string> AskAsync(Question question) => await Task.Run(() =>
        {
            output.WriteLine(question.Preamble);
            var match = false;
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
            Try(() => ConsoleEx.Move(0, startRow + lastPrintRow + lastProgressRow));
            output.WriteLine(message);
            lastPrintRow++;
        }

        public Progress NewProgress(string name)
        {
            var result = new Progress { Name = name };
            result.UpdateDownload = p =>
            {
                lock (progressLock)
                {
                    var pos = RoundDotPos(p, result.ProgressPrefix);
                    for (int i = result.Last ?? 0; i < pos; i++)
                    {
                        Try(() => ConsoleEx.WriteAt(result.ProgressPrefix.Length + i, result.Row, "."));
                    }
                    Console.Out.Flush();
                    result.Last = pos;
                }
            };
            result.UpdateInstall = p =>
            {
                lock (progressLock)
                {
                    var pos = RoundDotPos(p, result.ProgressPrefix);
                    for (int i = result.Last ?? 0; i < pos; i++)
                    {
                        Try(() => ConsoleEx.WriteAt(result.ProgressPrefix.Length + i, result.Row, "."));
                    }
                    Console.Out.Flush();
                    result.Last = pos;
                }
            };
            result.CompletedDownload = () =>
            {
                lock (progressLock)
                {
                    Try(() => ConsoleEx.WriteAt(70, result.Row, "downloaded"));
                    Console.Out.Flush();
                }
            };
            result.CompletedInstall = () =>
            {
                lock (progressLock)
                {
                    Try(() => ConsoleEx.WriteAt(70, result.Row, ".installed"));
                    Console.Out.Flush();
                }
            };

            result.Row = startRow + lastProgressRow;
            result.ProgressPrefix = result.Name + ": ";
            Try(() => ConsoleEx.WriteAt(0, result.Row, result.ProgressPrefix));
            lastProgressRow++;
            return result;
        }

        static int RoundDotPos(int p, string prefix)
        {
            return (int)Math.Round((p / 100.0) * (80.0 - prefix.Length));
        }
    }
}