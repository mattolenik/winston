using System.Threading.Tasks;

namespace Winston.User
{
    public interface IUserAdapter
    {
        Task<string> Ask(Question question);
        void Message(string message);
    }
}