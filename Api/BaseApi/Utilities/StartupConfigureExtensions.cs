

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace BaseApi
{
    public static class StartupConfigureExtensions
    {
        public static IApplicationBuilder ConfigureDefaultDevelopmentEnv(this IApplicationBuilder app, IConfiguration configuration)
        {
            return app
                .UseDeveloperExceptionPage()
                .UseHttpsRedirection()
                .UseAuthentication()
                .UseMvc()
                .UseCors("baseapi")
                .ConfigureSwagger(configuration);
        }

        public static IApplicationBuilder ConfigureDefaultProdEnv(this IApplicationBuilder app, IConfiguration configuration)
        {
            return app
                .UseHsts()
                .UseHttpsRedirection()
                .UseAuthentication()
                .UseMvc()
                .UseCors("baseapi")
                .ConfigureSwagger(configuration);
        }

        public static IApplicationBuilder ConfigureSwagger(this IApplicationBuilder app, IConfiguration configuration)
        {
            var swaggerTitle = configuration.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string>(Constants.Configuration.Sections.Settings.ProjectNameKey);
            var swaggerVersion = configuration.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string>(Constants.Configuration.Sections.Settings.ProjectVersionKey);
            return app.UseSwagger()
                    .UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{swaggerTitle} v{swaggerVersion}");
                    });
        }
    }
}