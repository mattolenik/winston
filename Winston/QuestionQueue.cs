using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Winston
{
    // Takes questions from other code that gets stuck and requires
    // user intervention. E.g. another class can ask, "should I reinstall this package?"
    // and QuestionQueue will block that call until the user responds through a UI.
    class QuestionQueue : IDisposable
    {
        readonly BufferBlock<Question> buf = new BufferBlock<Question>();
        readonly CancellationTokenSource cancel = new CancellationTokenSource();

        public QuestionQueue()
        {
            Task.Run(async () =>
            {
                while (!cancel.Token.IsCancellationRequested)
                {
                    var q = await buf.ReceiveAsync(cancel.Token);
                    await q.answerBlock.SendAsync(q.Answers.FirstOrDefault());
                }
            });
        }

        public async Task<string> Ask(Question question)
        {
            var b = new WriteOnceBlock<string>(s => s);
            question.answerBlock = b;
            // TODO: handle if buffer refuses message? If task.Result is false
            var task = await buf.SendAsync(question);
            return await b.ReceiveAsync();
        }

        public void Dispose() => cancel.Cancel(true);
    }

    struct Question
    {
        public string Query { get; }
        public IEnumerable<string> Answers { get; }
        internal WriteOnceBlock<string> answerBlock;

        public Question(string query, params string[] answer)
        {
            Answers = answer;
            Query = query;
            answerBlock = null;
        }
    }
}
