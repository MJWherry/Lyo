using Lyo.Job.Models.Enums;
using Lyo.Notification;

namespace Lyo.Job.Alerts;

/// <summary>Alert payload published to <c>job.notifications.alert</c> and consumed by <see cref="JobAlertConsumer" />.</summary>
public sealed record JobAlertEvent(Guid DefinitionId, Guid? RunId, JobAlertType AlertType, string Message, DateTime Timestamp) : INotification;
