using Lyo.Common.Core.Identifiers;
using Lyo.Diagram.Models;
using Lyo.Diagram.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Diagram.Postgres.Tests;

public class PostgresDiagramStoreTests(DiagramPostgresFixture fixture)
{
    [Fact]
    public async Task DbContext_MigrationsApplied_SchemaExists()
    {
        var factory = fixture.ServiceProvider.GetRequiredService<IDbContextFactory<DiagramDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestCt);
        var pending = await context.Database.GetPendingMigrationsAsync(TestCt);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task Store_SaveGetListDelete_RoundTripsCells()
    {
        var store = fixture.ServiceProvider.GetRequiredService<IDiagramStore>();
        var diagram = new DiagramDocument {
            Id = LyoGuid.CreateV7(),
            Name = "Checkout " + LyoGuid.CreateV7().ToString("N"),
            Pages = [
                new() {
                    Id = "flow",
                    Name = "Flow",
                    Cells = [
                        new() {
                            Id = "start",
                            Value = "Start",
                            Style = "rounded=0;whiteSpace=wrap;html=1;",
                            IsVertex = true,
                            Geometry = new() { X = 40, Y = 40, Width = 120, Height = 60 }
                        },
                        new() {
                            Id = "go",
                            Value = "next",
                            IsEdge = true,
                            SourceId = "start",
                            TargetId = "end",
                            Geometry = new() { Relative = true, Points = [new() { X = 80, Y = 140 }] }
                        }
                    ]
                },
                new() {
                    Id = "notes",
                    Name = "Notes",
                    PageWidth = 600,
                    PageHeight = 400,
                    Cells = [new() { Id = "note", Value = "Later", IsVertex = true }]
                }
            ]
        };

        await store.SaveAsync(diagram, TestCt);
        var listed = await store.ListAsync(TestCt);
        var summary = listed.Single(item => item.Id == diagram.Id);
        Assert.Equal(diagram.Name, summary.Name);
        Assert.NotEqual(default, summary.UpdatedTimestamp);

        var loaded = await store.GetAsync(diagram.Id, TestCt);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Pages.Count);
        Assert.Equal("Start", loaded.Pages[0].Cells.Single(cell => cell.Id == "start").Value);
        Assert.Equal(40d, loaded.Pages[0].Cells.Single(cell => cell.Id == "start").Geometry!.X);
        Assert.Equal("start", loaded.Pages[0].Cells.Single(cell => cell.Id == "go").SourceId);
        Assert.Equal(80d, loaded.Pages[0].Cells.Single(cell => cell.Id == "go").Geometry!.Points[0].X);
        Assert.Equal(600d, loaded.Pages[1].PageWidth);
        Assert.Equal("Later", loaded.Pages[1].Cells[0].Value);

        var updated = loaded with {
            Pages = [
                loaded.Pages[0] with {
                    Cells = loaded.Pages[0].Cells.Select(cell => cell.Id == "start" ? cell with { Value = "Started" } : cell).ToArray()
                },
                loaded.Pages[1]
            ]
        };
        await store.SaveAsync(updated, TestCt);
        var reloaded = await store.GetAsync(diagram.Id, TestCt);
        Assert.Equal("Started", reloaded!.Pages[0].Cells.Single(cell => cell.Id == "start").Value);

        Assert.True(await store.DeleteAsync(diagram.Id, TestCt));
        Assert.Null(await store.GetAsync(diagram.Id, TestCt));
        Assert.False(await store.DeleteAsync(diagram.Id, TestCt));
    }
}
