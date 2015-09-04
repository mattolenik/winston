using System;
using System.Threading.Tasks;
using NSpec;
using Winston.User;

namespace Winston.Test.User
{
    class UserProxyTest : nspecAsync
    {
        UserProxy proxy;
        TestAdapter adapter;

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

        void before_each()
        {
            adapter = new TestAdapter();
            proxy = new UserProxy(adapter);
        }

        void after_each()
        {
            proxy?.Dispose();
        }

        void describe_asking()
        {
            itSync["gets an answer"] = () =>
            {
                adapter.Answer = "ans1";
                var ans = Task.Run(() => proxy.Ask(new Question("what is the answer", "ans1", "ans2")));
                ans.Wait();
                ans.Result.should_be("ans1");

                adapter.Answer = "ans2";
                ans = Task.Run(() => proxy.Ask(new Question("what is the answer", "ans1", "ans2")));
                ans.Wait();
                ans.Result.should_be("ans2");
            };
        }
    }
}