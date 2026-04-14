using System.ComponentModel;

namespace Lyo.Api.Models.Enums;

[Obsolete("Only used in old opus")]
public enum RecordStatusEnum
{
    [Description("Active")]
    A, [Description("Disabled")]
    D, [Description("Inactive")]
    I, [Description("Job Failed")]
    JF, [Description("Job In Progress")]
    JI, [Description("Job Successfully Completed")]
    JS, [Description("Temporary")]
    T
}