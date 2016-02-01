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
        readonly int startRow;

        public ConsoleUserAdapter(TextWriter output, TextReader input)
        {
            this.output = output;
            this.input = input;
            try
            {
                this.startRow = Console.CursorTop;
            }
            catch (Exception) { }
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
            try
            {
                ConsoleEx.Move(0, startRow + lastPrintRow + lastProgressRow);
            }
            catch (Exception) { }
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
                    var pos = roundDotPos(p, result.ProgressPrefix);
                    try
                    {
                        for (int i = result.Last.Value; i < pos; i++)
                        {
                            ConsoleEx.WriteAt(result.ProgressPrefix.Length + i, result.Row, ".");
                        }
                        Console.Out.Flush();
                    }
                    catch (Exception) { }
                    result.Last = pos;
                }
            };
            result.UpdateInstall = p =>
            {
                lock (progressLock)
                {
                    var pos = roundDotPos(p, result.ProgressPrefix);
                    try
                    {
                        for (int i = result.Last.Value; i < pos; i++)
                        {
                            ConsoleEx.WriteAt(result.ProgressPrefix.Length + i, result.Row, ".");
                        }
                        Console.Out.Flush();
                    }
                    catch (Exception) { }
                    result.Last = pos;
                }
            };
            result.CompletedDownload = () =>
            {
                lock (progressLock)
                {
                    ConsoleEx.WriteAt(70, result.Row, "downloaded");
                    Console.Out.Flush();
                }
            };
            result.CompletedInstall = () =>
            {
                lock (progressLock)
                {
                    ConsoleEx.WriteAt(70, result.Row, ".installed");
                    Console.Out.Flush();
                }
            };

            result.Row = startRow + lastProgressRow;
            result.ProgressPrefix = result.Name + ": ";
            ConsoleEx.WriteAt(0, result.Row, result.ProgressPrefix);
            lastProgressRow++;
            return result;
        }

        int roundDotPos(int p, string prefix)
        {
            return (int)Math.Round((p / 100.0) * (80.0 - prefix.Length));
        }
    }
}