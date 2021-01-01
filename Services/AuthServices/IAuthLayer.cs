using System.Threading.Tasks;
using BaseApi.Models;

namespace BaseApi.Services
{
    public interface IAuthLayer
    {
        Task<User> GetUser(string accountToken);
    }
}
