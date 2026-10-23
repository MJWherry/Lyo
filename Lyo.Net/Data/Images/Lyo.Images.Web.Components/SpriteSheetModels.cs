using System.Diagnostics;
using Lyo.Web.Components.Models;

namespace Lyo.Images.Web.Components;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record AnimatorSheetEntry(Guid Id, LocalBrowserFile File);