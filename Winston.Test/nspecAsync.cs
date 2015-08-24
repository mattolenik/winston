using System;
using System.Linq;
using System.Threading.Tasks;
using NSpec;
using NSpec.Domain;
using NSpec.Domain.Formatters;
using NUnit.Framework;

/*
 * Howdy,
 * 
 * This is NSpec's DebuggerShim.  It will allow you to use TestDriven.Net or Resharper's test runner to run
 * NSpec tests that are in the same Assembly as this class.  
 * 
 * It's DEFINITELY worth trying specwatchr (http://nspecAsync.org/continuoustesting). Specwatchr automatically
 * runs tests for you.
 * 
 * If you ever want to debug a test when using Specwatchr, simply put the following line in your test:
 * 
 *     System.Diagnostics.Debugger.Launch()
 *     
 * Visual Studio will detect this and will give you a window which you can use to attach a debugger.
 */

namespace Winston.Test
{
    [TestFixture]
    public abstract class nspecAsync : global::NSpec.nspec
    {
        public new ActionRegisterAsync it { get; private set; }

        public ActionRegister itSync { get; private set; }

        protected nspecAsync()
        {
            it = new ActionRegisterAsync(base.it);
            itSync = base.it;
        }

        [Test]
        public void debug()
        {
            var currentSpec = this.GetType();
            var finder = new SpecFinder(new[] { currentSpec });
            var builder = new ContextBuilder(finder, new Tags().Parse(currentSpec.Name), new DefaultConventions());
            var runner = new ContextRunner(builder, new ConsoleFormatter(), false);
            var results = runner.Run(builder.Contexts().Build());

            //assert that there aren't any failures
            results.Failures().Count().should_be(0);
        }
    }

    public class ActionRegisterAsync
    {
        readonly ActionRegister register;

        public ActionRegisterAsync(ActionRegister register)
        {
            this.register = register;
        }

        public Func<Task> this[string key]
        {
            set { register[key] = () => Task.Run(value).Wait(); }
        }

        public Func<Task> this[string key, string tags]
        {
            set { register[key, tags] = () => Task.Run(value).Wait(); }
        }
    }
}