using Lyo.Diff.ObjectGraph;
using Lyo.Diff.Text;
using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Diff;

/// <summary>Registers Lyo diff services (text and object-graph comparison).</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="ITextTokenizer" />, <see cref="ITextDiffService" />, <see cref="IObjectGraphDiffService" />, and optional <see cref="IDiffService" />.</summary>
        public IServiceCollection AddLyoDiff()
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            services.AddSingleton<ITextTokenizer, TextTokenizer>();
            services.AddSingleton<ITextDiffService, TextDiffService>();
            services.AddSingleton<IObjectGraphDiffService, ObjectGraphDiffService>();
            services.AddSingleton<IDiffService, DiffService>();
            return services;
        }
    }
}