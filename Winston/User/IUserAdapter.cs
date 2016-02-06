using System.Threading.Tasks;

namespace Winston.User
{
    public interface IUserAdapter
    {
        Task<string> AskAsync(Question question);
        void Message(string message);
        Progress NewProgress(string name);
    }
}