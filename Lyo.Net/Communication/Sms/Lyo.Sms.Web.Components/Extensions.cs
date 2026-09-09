using Lyo.Exceptions;
using Lyo.Web.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Sms.Web.Components;

/// <summary>DI helpers for SMS UI components.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the carrier delivery palette so <see cref="LyoStatusChip" /> paints message statuses with the same colours as <see cref="SmsColorHelper" />.
        /// Optional: skip it and a chip uses the shared status vocabulary, which treats <c>accepted</c> and <c>sent</c> differently.
        /// </summary>
        public IServiceCollection AddLyoSmsStatusPalette()
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddLyoStatusPalette<SmsStatusPalette>();
        }
    }
}
