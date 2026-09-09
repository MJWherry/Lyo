using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Web.Components.LyoType;
using Lyo.Web.Components.ParamTable;
using Lyo.Web.Primitives.DataGrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Web.Components;

/// <summary>Service registration for the shared Blazor components in this package.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Sets the host default layout for <see cref="ParamTable.LyoParameterEditor" />.</summary>
        public IServiceCollection AddLyoParameterEditor(Action<LyoParameterEditorOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new LyoParameterEditorOptions();
            configure(options);
            return services.AddLyoParameterEditor(options);
        }

        /// <summary>Sets the host default layout for <see cref="ParamTable.LyoParameterEditor" /> from configuration.</summary>
        public IServiceCollection AddLyoParameterEditorFromConfiguration(IConfiguration configuration, string sectionName = LyoParameterEditorOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            return services.AddLyoParameterEditor(LyoOptions.Bind<LyoParameterEditorOptions>(configuration, sectionName));
        }

        /// <summary>
        /// Sets the host default layout for <see cref="ParamTable.LyoParameterEditor" />. Can be skipped: editors fall back to <see cref="LyoParameterLayout.Table" /> when this is never
        /// called. A layout the user picked earlier still wins over the default.
        /// </summary>
        public IServiceCollection AddLyoParameterEditor(LyoParameterEditorOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            return services;
        }

        /// <summary>Sets the host defaults for every data grid (starting layout and the viewport that switches to cards).</summary>
        public IServiceCollection AddLyoDataGrid(Action<LyoDataGridOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new LyoDataGridOptions();
            configure(options);
            return services.AddLyoDataGrid(options);
        }

        /// <summary>Sets the host defaults for every data grid from configuration.</summary>
        public IServiceCollection AddLyoDataGridFromConfiguration(IConfiguration configuration, string sectionName = LyoDataGridOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            return services.AddLyoDataGrid(LyoOptions.Bind<LyoDataGridOptions>(configuration, sectionName));
        }

        /// <summary>
        /// Sets the host defaults for every <see cref="LyoDataGrid{T}" /> and <see cref="LyoDataGridProjected" />. Can be skipped: grids fall back to
        /// <see cref="LyoDataGridLayout.Auto" /> (table on wide screens, cards on phones) when this is never called. A layout the user picked earlier still wins.
        /// </summary>
        public IServiceCollection AddLyoDataGrid(LyoDataGridOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            return services;
        }

        /// <summary>
        /// Adds <see cref="ClientStore" /> plus the <see cref="ILyoLayoutPreferences" /> and <see cref="ILyoUiPreferences" /> views of it, so the primitives in
        /// <c>Lyo.Web.Primitives</c> can read the table/card layout and the tab the user last picked. Requires <c>AddBlazoredLocalStorage</c>.
        /// </summary>
        /// <remarks>Registering the store directly instead leaves those primitives without stored preferences, and they fall back to their defaults.</remarks>
        public IServiceCollection AddLyoClientStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddScoped<ClientStore>();
            services.TryAddScoped<ILyoLayoutPreferences>(sp => sp.GetRequiredService<ClientStore>());
            services.TryAddScoped<ILyoUiPreferences>(sp => sp.GetRequiredService<ClientStore>());
            return services;
        }

        /// <summary>
        /// Adds the value-editor catalog so <see cref="LyoTypeValueInput" /> can render editors supplied by other packages. Call this once on the host, then call the
        /// per-editor registration for each one (for example <c>AddLyoFormatterValueEditor</c> from <c>Lyo.Formatter.Web.Components</c>).
        /// </summary>
        /// <remarks>Registering an editor descriptor implies this, so calling it directly is only needed when the host wants the catalog resolvable on its own.</remarks>
        public IServiceCollection AddLyoValueEditors()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<LyoValueEditorCatalog>();
            return services;
        }
    }
}
