using System.IO;
using System.Threading.Tasks;
using NSpec;
using NUnit.Framework;
using Winston.User;

namespace Winston.Test.User
{
    class ConsoleUserAdapterTest : nspecAsync
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
            it["can ask and receive an answer"] = async () =>
            {
                var answer = await adapter.Ask(new Question("", "Question", "ans1", "ans2"));
                answer.should_be("ans1");
            };

            itSync["can receive a message"] = () =>
            {
                adapter.Message("msg");
                var output = writer.GetStringBuilder().ToString();
                output.should_contain("msg");
            };
        }
    }
}
