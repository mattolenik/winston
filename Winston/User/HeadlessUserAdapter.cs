using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winston.User
{
    class HeadlessUserAdapter:IUserAdapter
    {
        public Task<string> Ask(Question question)
        {
            return null;
        }

        public void Message(string message)
        {
        }

        public Progress NewProgress(string name)
        {
            return null;
        }
    }
}
