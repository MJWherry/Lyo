using Lyo.Benchmarking;

[assembly:
    BenchmarkReport(
        "query", "Query & CRUD",
        Description = "Lyo.Query engine internals plus end-to-end CRUD. In-memory suites run the where-clause engine " +
            "(expression build, filtering, single-entity match), ordering, object-to-DTO mapping, and the projection " +
            "pipeline over generated BenchPerson rows (RowCount). The CRUD suite runs Query/Get/Patch/Create/Delete " +
            "against a real PostgreSQL database (Testcontainers, Docker) paging Amount rows. RootQueryBenchmarks exercises " +
            "From/Joins root /Query (flat select, left-join fan-out collapse, exact count). Each class's data shape " +
            "captures the entity/model structure being exercised, including nested collections.")]

BenchmarkEntry.Run(args);