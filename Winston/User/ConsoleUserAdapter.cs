using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winston.User
{
    class ConsoleUserAdapter : IUserAdapter
    {
        public async Task<string> Ask(Question question) => await Task.Run(() =>
        {
            Console.WriteLine(question.Preamble);
            bool match = false;
            string response = null;
            while (!match)
            {
                Console.WriteLine(question.Query);
                response = Console.ReadLine()?.Trim();
                match = question.Answers.Any(a => a.EqualsOrdIgnoreCase(response));
                if (!match)
                {
                    var answers = string.Join(", ", question.Answers);
                    Console.WriteLine($"\nAnswer must be one of these values: {answers}");
                }
            }
            return response;
        });

        public void Message(string message)
        {
            Console.WriteLine(message);
        }
    }
}