using Microsoft.Extensions.Configuration;

namespace BaseApi.Services
{
    public class FileDataAccessService
    {

        private readonly FileDataLayerInterface dataLayer_;

        private readonly IConfiguration configuration_;

        public FileDataAccessService(IConfiguration configuration, FileDataLayerInterface dataLayer)
        {
            configuration_ = configuration;
            dataLayer_ = dataLayer;
        }
    }
}
