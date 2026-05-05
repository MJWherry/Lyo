# Lyo.Job.Worker

Worker SDK for the Lyo job system. Provides a base class that handles job lifecycle (fetch, start, execute, finish, cancellation) so workers only implement ExecuteAsync.

## Related projects
- [`Lyo.Api.Client`](../../Api/Lyo.Api.Client/README.md)
- [`Lyo.Job.Models`](../Lyo.Job.Models/README.md)
- [`Lyo.MessageQueue`](../../../Communication/MessageQueue/Lyo.MessageQueue/README.md)
