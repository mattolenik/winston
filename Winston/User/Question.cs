using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace Winston.User
{
    public class Question
    {
        public string Preamble { get; set; }

        public string Query { get; }
        public IEnumerable<string> Answers { get; }
        internal WriteOnceBlock<string> answerBlock;

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