using System.IO;
using NSpec;
using Winston.User;

namespace Winston.Test.User
{
    class ConsoleUserAdapterTest : nspec
    {
        StringWriter writer;
        StringReader reader;
        ConsoleUserAdapter adapter;

        void before_each()
        {
            writer = new StringWriter();
            reader = new StringReader("ans1");
            adapter = new ConsoleUserAdapter(writer, reader);
        }

        void describe_adapter()
        {
            itAsync["can ask and receive an answer"] = async () =>
            {
                var answer = await adapter.Ask(new Question("", "Question", "ans1", "ans2"));
                answer.should_be("ans1");
            };

            it["can receive a message"] = () =>
            {
                adapter.Message("msg");
                var output = writer.GetStringBuilder().ToString();
                output.should_contain("msg");
            };
        }
    }
}
