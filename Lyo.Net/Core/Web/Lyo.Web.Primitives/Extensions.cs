using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Web.Primitives;

/// <summary>Registers status palettes and the workbench registry with DI.</summary>
public static class StatusPaletteExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a domain status palette so <see cref="LyoStatusChip" /> can colour that domain's statuses. Call once per domain; the shared vocabulary in
        /// <see cref="LyoDefaultStatusPalette" /> is always available and needs no extra registration.
        /// </summary>
        /// <remarks>
        /// Palettes are additive: several packages can register under different names, and the chip selects one by <see cref="LyoStatusChip.Palette" />.
        /// <code>
        /// builder.Services.AddLyoStatusPalette&lt;JobStatusPalette&gt;();
        /// </code>
        /// </remarks>
        /// <typeparam name="TPalette">Palette implementation. Must be stateless, because it is registered as a singleton.</typeparam>
        public IServiceCollection AddLyoStatusPalette<TPalette>()
            where TPalette : class, ILyoStatusPalette
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ILyoStatusPalette, TPalette>());
            services.TryAddSingleton<LyoStatusPaletteResolver>();
            return services;
        }

        /// <summary>Registers a pre-built palette, for one built from host configuration rather than resolved from DI.</summary>
        /// <param name="palette">Palette instance. Must remain safe to share across requests.</param>
        public IServiceCollection AddLyoStatusPalette(ILyoStatusPalette palette)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(palette);
            services.TryAddEnumerable(ServiceDescriptor.Singleton(palette));
            services.TryAddSingleton<LyoStatusPaletteResolver>();
            return services;
        }

        /// <summary>
        /// Registers the palette resolver by itself. Only needed when a host wants it resolvable without registering any domain palette, since
        /// <c>AddLyoStatusPalette</c> already implies it.
        /// </summary>
        public IServiceCollection AddLyoStatusPalettes()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<LyoStatusPaletteResolver>();
            return services;
        }

        /// <summary>
        /// Registers a workbench so <see cref="LyoNavMenu" /> can list it and <see cref="LyoWorkbenchHost" /> can render it. Call once per component; the registry is
        /// implied by this call.
        /// </summary>
        /// <typeparam name="TComponent">Blazor component that forms the workbench body.</typeparam>
        /// <param name="title">Title used in nav and on the page.</param>
        /// <param name="icon">Material icon shown beside the title.</param>
        /// <param name="category">Drawer group the item belongs to.</param>
        /// <param name="route">Href for the nav item. Point at an existing host page, or <c>workbench/{slug}</c>.</param>
        /// <param name="slug">Optional slug consumed by <see cref="LyoWorkbenchHost" />. Defaults to the last segment of <paramref name="route" />.</param>
        public IServiceCollection AddLyoWorkbench<TComponent>(string title, string icon, string category, string route, string? slug = null)
            where TComponent : Microsoft.AspNetCore.Components.IComponent
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(title);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(route);
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILyoWorkbenchDescriptor>(
                    new LyoWorkbenchDescriptor(title, icon ?? string.Empty, category ?? string.Empty, route, typeof(TComponent), slug)));
            services.TryAddSingleton<LyoWorkbenchRegistry>();
            return services;
        }

        /// <summary>
        /// Registers an empty workbench registry. Only needed when a host wants <see cref="LyoNavMenu" /> resolvable without any <c>AddLyoWorkbench</c> call.
        /// </summary>
        public IServiceCollection AddLyoWorkbenches()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<LyoWorkbenchRegistry>();
            return services;
        }
    }
}
