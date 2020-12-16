using System.Threading.Tasks;
using BaseApi.Models;

namespace BaseApi.Services
{
    public interface AuthLayerInterface
    {
        Task<User> GetUser(string accountToken);
    }
}
