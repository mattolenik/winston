using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace Winston.User
{
    public class Question
    {
        public string Preamble { get; set; }

        public string Query { get; }
        public IEnumerable<string> Answers { get; }
#pragma warning disable CC0052 // Make field readonly
        internal WriteOnceBlock<string> answerBlock;
#pragma warning restore CC0052 // Make field readonly

        public Question(string preamble, string query, params string[] answers) :
            this(preamble, query, answers as IEnumerable<string>)
        {
        }

        public Question(string preamble, string query, IEnumerable<string> answers)
        {
            Preamble = preamble;
            Answers = answers;
            Query = query;
            answerBlock = null;
        }
    }
}