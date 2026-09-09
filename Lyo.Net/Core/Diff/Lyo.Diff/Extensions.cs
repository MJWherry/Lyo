using Lyo.Diff.ObjectGraph;
using Lyo.Diff.Text;
using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Diff;

/// <summary>DI helpers that register Lyo text and object-graph diff services.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="ITextTokenizer" />, <see cref="ITextDiffService" />, <see cref="IObjectGraphDiffService" />, and <see cref="IDiffService" />.</summary>
        public IServiceCollection AddLyoDiff()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<ITextTokenizer, TextTokenizer>();
            services.AddSingleton<ITextDiffService, TextDiffService>();
            services.AddSingleton<IObjectGraphDiffService, ObjectGraphDiffService>();
            services.AddSingleton<IDiffService, DiffService>();
            return services;
        }
    }
}