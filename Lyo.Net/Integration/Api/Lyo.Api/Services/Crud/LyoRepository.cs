using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Delete;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud;

public class LyoRepository<TContext>(
    IQueryService<TContext> query,
    ICreateService<TContext> create,
    IUpdateService<TContext> update,
    IPatchService<TContext> patch,
    IDeleteService<TContext> delete,
    IUpsertService<TContext> upsert)
    : ILyoRepository<TContext>
    where TContext : DbContext
{
    public IQueryService<TContext> Query => query;
    public ICreateService<TContext> Create => create;
    public IUpdateService<TContext> Update => update;
    public IPatchService<TContext> Patch => patch;
    public IDeleteService<TContext> Delete => delete;
    public IUpsertService<TContext> Upsert => upsert;
}
