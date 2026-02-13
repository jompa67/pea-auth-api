namespace AuthApi.Extensions;

public static class ConfigurationExtensions
{
    public static T ReadConfig<T>(this IServiceCollection services, IConfiguration configuration)
        where T : class, new()
    {
        var section = configuration.GetSection(typeof(T).Name);
        if (!section.Exists())
            throw new InvalidOperationException($"Configuration section '{typeof(T).Name}' is missing.");

        var settings = section.Get<T>() ?? throw new InvalidOperationException($"Failed to bind '{typeof(T).Name}'.");

        return settings;
    }

    public static IServiceCollection AddConfig<T>(this IServiceCollection services, IConfiguration configuration)
        where T : class, new()
    {
        var section = configuration.GetSection(typeof(T).Name);
        if (!section.Exists())
            throw new InvalidOperationException($"Configuration section '{typeof(T).Name}' is missing.");

        services.Configure<T>(section);

        return services;
    }
}