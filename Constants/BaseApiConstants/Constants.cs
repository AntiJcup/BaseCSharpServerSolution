
namespace BaseApi.Constants
{
    public static class Configuration
    {
        public static class Sections
        {
            public static readonly string SettingsKey = "Settings";

            public static readonly string PathsKey = "Paths";

            public static readonly string UrlsKey = "UrlSettings";

            public static class Settings
            {
                public static readonly string UseAWSKey = "UseAWS";

                public static readonly string UserPoolClientIdKey = "UserPoolClientId";

                public static readonly string UserPoolIdKey = "UserPoolId";

                public static readonly string UserPoolAuthorityKey = "UserPoolAuthority";

                public static readonly string LocalAdminKey = "LocalAdmin";

                public static readonly string GoogleExternalGroupNameKey = "GoogleExternalGroupName";

                public static readonly string CorsDomainArrayKey = "CorsDomainArray";

                public static readonly string LocalAuthUserNameKey = "LocalAuthUserName";

                public static readonly string LocalAuthUserEmailKey = "LocalAuthUserEmail";
            }

            public static class Paths
            {
                public static readonly string BucketKey = "Bucket";
            }
        }
    }
}