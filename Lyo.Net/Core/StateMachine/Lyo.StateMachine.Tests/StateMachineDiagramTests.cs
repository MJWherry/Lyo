namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineDiagramTests
{
    [Fact]
    public void ToMermaid_IncludesStatesTransitionsGuardsAndSubstates()
    {
        const string expected = """
                                stateDiagram-v2
                                    [*] --> Idle
                                    Idle
                                    state Combat {
                                        Attacking
                                        Defending
                                    }
                                    Dead
                                    Idle --> Attacking : Engage
                                    Attacking --> Defending : Block
                                    Combat --> Idle : Disengage
                                    Combat --> Dead : Hit [health depleted]
                                    Defending --> Attacking : Strike

                                """;

        Assert.Equal(expected.ReplaceLineEndings("\n"), TestMachines.Character().Build().ToMermaid());
    }

    [Fact]
    public void ToMermaid_UnnamedGuardAndReentry_AreLabelled()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Running);
        builder.Configure(JobState.Running).PermitIf(JobTrigger.Complete, JobState.Done, _ => true).PermitReentry(JobTrigger.Heartbeat);
        var mermaid = builder.Build().ToMermaid();
        Assert.Contains("    Running --> Done : Complete [guard]\n", mermaid);
        Assert.Contains("    Running --> Running : Heartbeat\n", mermaid);
    }

    [Fact]
    public void ToDot_IncludesClustersForSubstates()
    {
        var dot = TestMachines.Character().Build().ToDot();
        Assert.StartsWith("digraph \"character\" {\n", dot);
        Assert.Contains(
            """
                    subgraph "cluster_Combat" {
                        label="Combat";
                        "Combat";
                        "Attacking";
                        "Defending";
                    }

                """.ReplaceLineEndings("\n"), dot);

        Assert.Contains("    \"__start\" -> \"Idle\";\n", dot);
        Assert.Contains("    \"Combat\" -> \"Dead\" [label=\"Hit [health depleted]\"];\n", dot);
        Assert.EndsWith("}\n", dot);
    }
}