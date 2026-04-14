using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Delete;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud;

public interface ILyoRepository<TContext>
    where TContext : DbContext
{
    IQueryService<TContext> Query { get; }
    ICreateService<TContext> Create { get; }
    IUpdateService<TContext> Update { get; }
    IPatchService<TContext> Patch { get; }
    IDeleteService<TContext> Delete { get; }
    IUpsertService<TContext> Upsert { get; }
}
