using Lyo.Exceptions;
using Lyo.Web.Components;
using Lyo.Web.Components.LyoType;
using Lyo.Web.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Formatter.Web.Components;

/// <summary>DI helpers that register the formatter editors in this package.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="LyoFormatterValueEditor" /> as the editor for every formatter-typed parameter, with an optional host example context for autocomplete.
        /// </summary>
        /// <remarks>
        /// Needs an <see cref="IFormatterService" /> registration (<c>AddFormatterService()</c>) for the live preview.
        /// <para>
        /// There is no <c>FromConfiguration</c> overload, unlike most Lyo registrations: an example context is an object graph defined in code, not a set of
        /// configuration values.
        /// </para>
        /// </remarks>
        /// <param name="configure">Declares the host's own context keys, e.g. <c>c =&gt; c.Add("client", sample)</c>.</param>
        public IServiceCollection AddLyoFormatterValueEditor(Action<LyoFormatterExampleContext>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var context = new LyoFormatterExampleContext();
            configure?.Invoke(context);
            return services.AddLyoFormatterValueEditor(context);
        }

        /// <summary>Registers <see cref="LyoFormatterValueEditor" /> for formatter-typed parameters, using an already-built example context.</summary>
        public IServiceCollection AddLyoFormatterValueEditor(LyoFormatterExampleContext exampleContext)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(exampleContext);
            services.AddLyoValueEditors();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ILyoValueEditorDescriptor, LyoFormatterValueEditorDescriptor>());
            services.TryAddSingleton(exampleContext);
            return services;
        }
    }
}
