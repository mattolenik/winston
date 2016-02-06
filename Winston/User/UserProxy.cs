using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Winston.User
{
    /// <summary>
    /// Provides a proxy mechanism by which application components can notify the user
    /// or ask the user questions and receive responses.
    /// </summary>
    public sealed class UserProxy : IDisposable
    {
        readonly BufferBlock<Question> buf = new BufferBlock<Question>();
        readonly CancellationTokenSource cancel = new CancellationTokenSource();
        readonly IUserAdapter adapter;

        public UserProxy(IUserAdapter adapter)
        {
            this.adapter = adapter;
            Task.Run(async () =>
            {
                while (!cancel.Token.IsCancellationRequested)
                {
                    var q = await buf.ReceiveAsync(cancel.Token);
                    var answer = await adapter.AskAsync(q);
                    await q.answerBlock.SendAsync(answer);
                }
            });
        }

        public async Task<string> AskAsync(Question question)
        {
            var b = new WriteOnceBlock<string>(s => s);
            question.answerBlock = b;
            // TODO: handle if buffer refuses message? If task.Result is false
            var task = await buf.SendAsync(question);
            return await b.ReceiveAsync();
        }

        public void Message(string message)
        {
            adapter.Message(message);
        }

        public Progress NewProgress(string name)
        {
            return adapter.NewProgress(name);
        }

        public void Dispose() => cancel.Dispose();
    }
}
