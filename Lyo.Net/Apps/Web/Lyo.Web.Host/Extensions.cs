using Blazored.LocalStorage;
using Lyo.Exceptions;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor.Services;

namespace Lyo.Web.Host;

/// <summary>DI helpers for a Lyo Blazor host.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds MudBlazor, local storage, clipboard/timezone interop, <see cref="ClientStore" />, the status-palette resolver, and an empty workbench
        /// registry. Domain palettes and workbenches still register themselves.
        /// </summary>
        /// <param name="configureMud">Optional MudBlazor tweak. When omitted, snackbars sit bottom-right and popovers close on an outside click.</param>
        public IServiceCollection AddLyoWebShell(Action<MudServicesConfiguration>? configureMud = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddMudServices(configureMud ?? DefaultMud);
            services.AddBlazoredLocalStorage();
            services.TryAddScoped<IJsInterop, JsInterop>();
            services.TryAddScoped<ILyoTimeZone, LyoBrowserTimeZone>();
            services.AddLyoClientStore();
            services.AddLyoStatusPalettes();
            services.AddLyoWorkbenches();
            services.TryAddScoped(sp => {
                var client = new HttpClient();
                if (sp.GetService<NavigationManager>() is { } navigation)
                    client.BaseAddress = new Uri(navigation.BaseUri);

                return client;
            });
            return services;
        }
    }

    private static void DefaultMud(MudServicesConfiguration config)
    {
        config.PopoverOptions.ModalOverlay = true;
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
        config.SnackbarConfiguration.PreventDuplicates = false;
        config.SnackbarConfiguration.NewestOnTop = false;
        config.SnackbarConfiguration.ShowCloseIcon = true;
        config.SnackbarConfiguration.VisibleStateDuration = 5000;
        config.SnackbarConfiguration.HideTransitionDuration = 500;
        config.SnackbarConfiguration.ShowTransitionDuration = 500;
        config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        config.SnackbarConfiguration.ErrorIcon = Icons.Material.Filled.BugReport;
    }
}
