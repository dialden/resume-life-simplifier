using Microsoft.Extensions.Configuration;

namespace DialDen.RLS.Tests.Extensions;

public static class IConfigurationExtensions
{
    public static T GetRequiredValue<T>(this IConfiguration config, string key)
    {
        var value = config.GetValue<T>(key);

        if (value == null || value.Equals(default(T)))
        {
            throw new InvalidOperationException($"Missing required configuration: {key}");
        }

        return value;
    }

}
