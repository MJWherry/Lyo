using Lyo.Job.Models;
using Lyo.Job.Tests.Postgres;
using Microsoft.Extensions.Logging.Abstractions;
using ClientPublisher = Lyo.Job.Client.MqJobEventPublisher;
using ClientJobMqOptions = Lyo.Job.Client.JobMqOptions;
using PostgresPublisher = Lyo.Job.Postgres.Events.MqJobEventPublisher;
using PostgresJobMqOptions = Lyo.Job.Postgres.JobMqOptions;

namespace Lyo.Job.Tests;

/// <summary>
/// API and worker hosts declare <c>job.events</c> plus the shared job queues at startup so a fresh broker does not require a manual topology step before the first publish or
/// subscribe.
/// </summary>
public class MqJobEventPublisherSetupTests
{
    [Fact]
    public async Task SetupAsync_ClientPublisher_DeclaresJobEventExchangeAndSharedQueues()
    {
        var mq = new FakeMqService();
        var publisher = new ClientPublisher(mq, NullLogger<ClientPublisher>.Instance, new ClientJobMqOptions { WorkerTypes = ["cs"] });
        await publisher.SetupAsync(TestContext.Current.CancellationToken);
        AssertJobEventTopology(mq, "cs");
    }

    [Fact]
    public async Task SetupAsync_PostgresPublisher_DeclaresJobEventExchangeAndSharedQueues()
    {
        var mq = new FakeMqService();
        var publisher = new PostgresPublisher(mq, NullLogger<PostgresPublisher>.Instance, new PostgresJobMqOptions { WorkerTypes = ["cs"] });
        await publisher.SetupAsync(TestContext.Current.CancellationToken);
        AssertJobEventTopology(mq, "cs");
    }

    private static void AssertJobEventTopology(FakeMqService mq, string workerType)
    {
        var exchange = Assert.Single(mq.CreatedExchanges);
        Assert.Equal(Constants.Mq.JobEventExchange, exchange.Name);
        Assert.Equal(Constants.Mq.JobEventExchangeType, exchange.Type);
        Assert.True(exchange.Durable);
        Assert.False(exchange.AutoDelete);
        Assert.Contains(mq.CreatedQueues, q => q.Name == Constants.Mq.QueueJobRunFinish && q.Durable);
        Assert.Contains(mq.CreatedQueues, q => q.Name == Constants.Mq.JobDefinitionChangeKey && q.Durable);
        Assert.Contains(mq.CreatedQueues, q => q.Name == Constants.Mq.QueueGetJobRunCreated(workerType) && q.Durable);
        Assert.Contains(mq.Bindings, b => b.QueueName == Constants.Mq.JobDefinitionChangeKey && b.ExchangeName == Constants.Mq.JobEventExchange);
    }
}
