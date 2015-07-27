using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Winston.User;

namespace Winston.Test.User
{
    [TestFixture]
    public class ConsoleUserAdapterTest
    {
        [Test]
        public async Task TestAdapter()
        {
            var writer = new StringWriter();
            var reader = new StringReader("ans1");
            var adapter = new ConsoleUserAdapter(writer, reader);
            var answer = await adapter.Ask(new Question("", "Question", "ans1", "ans2"));
            Assert.AreEqual("ans1", answer);

            adapter.Message("msg");
            var output = writer.GetStringBuilder().ToString();
            Assert.IsTrue(output.Contains("msg"), "Message not contained in output");
        }
    }
}
