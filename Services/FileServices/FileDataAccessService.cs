using Microsoft.Extensions.Configuration;

namespace BaseApi.Services
{
    public class FileDataAccessService
    {

        private readonly IFileDataLayer dataLayer_;

        private readonly IConfiguration configuration_;

        public FileDataAccessService(IConfiguration configuration,
                                     IFileDataLayer dataLayer)
        {
            configuration_ = configuration;
            dataLayer_ = dataLayer;
        }
    }
}
