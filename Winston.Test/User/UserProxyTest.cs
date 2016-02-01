using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Winston.User;

namespace Winston.Test.User
{
    public class UserProxyTest : IDisposable
    {
        readonly UserProxy proxy;
        readonly TestAdapter adapter;

        class TestAdapter : IUserAdapter
        {
            public string Answer;

            public Task<string> Ask(Question question)
            {
                return Task.FromResult(Answer);
            }

            public void Message(string message)
            {
            }

            public Progress NewProgress(string name)
            {
                return new Progress();
            }
        }

        public UserProxyTest()
        {
            adapter = new TestAdapter();
            proxy = new UserProxy(adapter);
        }

        public void Dispose()
        {
            proxy?.Dispose();
        }

        [Fact]
        public void GetsAnAnswer()
        {
            adapter.Answer = "ans1";
            var ans = Task.Run(() => proxy.Ask(new Question("what is the answer", "ans1", "ans2")));
            ans.Wait();
            ans.Result.Should().Be("ans1");

            adapter.Answer = "ans2";
            ans = Task.Run(() => proxy.Ask(new Question("what is the answer", "ans1", "ans2")));
            ans.Wait();
            ans.Result.Should().Be("ans2");
        }
    }
}