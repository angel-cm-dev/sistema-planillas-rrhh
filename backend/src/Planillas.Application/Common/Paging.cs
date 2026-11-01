using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Planillas.Application.Common;

/// <summary>Parámetros comunes de listados: página, tamaño, búsqueda libre y orden.</summary>
public class PageQuery
{
    public const int MaxPageSize = 100;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    /// <summary>"asc" o "desc".</summary>
    public string? SortDir { get; set; }

    public bool Descendente => string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class QueryableExtensions
{
    /// <summary>
    /// Pagina en la BD (COUNT + OFFSET/FETCH). El tamaño se acota a <see cref="PageQuery.MaxPageSize"/>
    /// para que un cliente no pueda pedir la tabla completa en una sola llamada.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PageQuery pq, CancellationToken ct)
    {
        var page = Math.Max(1, pq.Page);
        var size = Math.Clamp(pq.PageSize, 1, PageQuery.MaxPageSize);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PagedResult<T>(items, page, size, total);
    }

    /// <summary>
    /// Ordena usando una "lista blanca" de columnas. Nunca se ordena por un nombre arbitrario
    /// enviado por el cliente: evita errores y expone solo columnas pensadas para ello.
    /// </summary>
    public static IQueryable<T> OrdenarPor<T>(
        this IQueryable<T> query,
        PageQuery pq,
        IReadOnlyDictionary<string, Expression<Func<T, object>>> columnas,
        string columnaPorDefecto)
    {
        var clave = pq.SortBy is not null && columnas.ContainsKey(pq.SortBy.ToLowerInvariant())
            ? pq.SortBy.ToLowerInvariant()
            : columnaPorDefecto;
        var selector = columnas[clave];
        return pq.Descendente ? query.OrderByDescending(selector) : query.OrderBy(selector);
    }
}
