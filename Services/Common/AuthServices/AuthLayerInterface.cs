using System.Threading.Tasks;
using BaseApi.Models.Common;

namespace BaseApi.Services.Common
{
    public interface AuthLayerInterface
    {
        Task<User> GetUser(string accountToken);
    }
}
