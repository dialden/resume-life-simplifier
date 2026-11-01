using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.Postings;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.DependencyInjection;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddRLSCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICreatePosting, CreatePosting>();
        services.AddScoped<IPublishPosting, PublishPosting>();
        services.AddScoped<IViewPosting, ViewPosting>();
        services.AddScoped<IListPostings, ListPostings>();
        services.AddScoped<IJobSites, Postings.JobSites>();
        services.AddScoped<IRegisterJobSites, RegisterJobSites>();
        services.AddScoped<IPublishOnJobSite, PublishOnJobSite>();

        return services;
    }
}
