using Microsoft.Extensions.Configuration;

namespace CodeExplainer.Shared.Utils;

public class UrlHelper
{
    public static string GetBackendUrl(IConfiguration configuration)
    {
        var backendUrl = Environment.GetEnvironmentVariable("BACKEND_URL");
        if (string.IsNullOrWhiteSpace(backendUrl))
        {
            backendUrl = configuration["URLs:BackendURL"];
            if (string.IsNullOrWhiteSpace(backendUrl))
            {
                throw new Exception("Backend URL not configured");
            }
        }
        return backendUrl;
    }

    public static string GetFrontendUrl(IConfiguration configuration)
    {
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            frontendUrl = configuration["URLs:FrontendURL"];
            if (string.IsNullOrWhiteSpace(frontendUrl))
            {
                throw new Exception("Frontend URL not configured");
            }
        }
        return frontendUrl;
    }
}