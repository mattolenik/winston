using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSpec;

namespace Winston.Test
{
    class question_queue : nspec
    {
        QuestionQueue queue;

        void before_each()
        {
            queue = new QuestionQueue();
        }

        void describe_asking()
        {
            it["gets an answer"] = () =>
            {
                var ans = Task.Run(() => queue.Ask(new Question("what is the answer", "ans1", "ans2")));
                ans.Wait();
                ans.Result.should_be("ans1");
            };
        }
    }
}