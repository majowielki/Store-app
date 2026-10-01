using Anthropic;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Configuration;

namespace Store.ReviewService.Moderation;

/// <summary>Registration of the model that reads new reviews first (ADR 019).</summary>
public static class ReviewModelExtensions
{
    public static IServiceCollection AddReviewModel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStoreOptions<ReviewModelOptions>(configuration, ReviewModelOptions.SectionName);

        services.AddSingleton<IAnthropicClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ReviewModelOptions>>().Value;
            return new AnthropicClient { ApiKey = options.ApiKey, Timeout = options.Timeout, MaxRetries = options.MaxRetries };
        });

        services.AddSingleton<IReviewModel>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ReviewModelOptions>>().Value;
            if (options.IsOn)
            {
                return ActivatorUtilities.CreateInstance<ClaudeReviewModel>(provider);
            }

            if (options.Enabled)
            {
                provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ReviewModelExtensions))
                    .LogWarning("The review model is enabled but has no API key; every review waits for the administrator");
            }

            return new NoReviewModel();
        });

        services.AddScoped<ReviewModelDispatcher>();
        return services;
    }
}
