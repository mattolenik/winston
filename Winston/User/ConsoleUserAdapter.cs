using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.GotDotNet;

namespace Winston.User
{
    public class Progress
    {
        public Action<int> Update { get; set; } = _ => { };

        public int Row { get; set; }
    }

    class ConsoleUserAdapter : IUserAdapter
    {
        readonly TextWriter output;
        readonly TextReader input;

        public ConsoleUserAdapter(TextWriter output, TextReader input)
        {
            this.output = output;
            this.input = input;
            Console.Clear();
            origRow = Console.CursorTop;
            origCol = Console.CursorLeft;
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
            output.WriteLine(message);
        }

        int row = 0;

        public Progress NewProgress()
        {
            var result = new Progress();
            result.Update = p =>
            {
                if (p != last)
                {
                    ConsoleEx.WriteAt(0, result.Row, p.ToString());
                    last = p;
                }
            };
            result.Row = row++;
            return result;
        }

        protected static int origRow;
        protected static int origCol;
        protected static int? last;
    }
}