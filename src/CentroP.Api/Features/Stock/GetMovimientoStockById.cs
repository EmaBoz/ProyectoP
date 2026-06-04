using CentroP.Api.Common.Exceptions;
using CentroP.Api.Common.Interfaces;
using Dapper;
using MediatR;

namespace CentroP.Api.Features.Stock;

public sealed record GetMovimientoStockByIdQuery(int IdSucursal, int Id)
    : IRequest<MovimientoStockDetailDto>;

public sealed record MovimientoStockDetailDto(
    int Id,
    int IdSucursal,
    int IdProducto,
    string CodigoProducto,
    string NombreProducto,
    int IdTipoMovimiento,
    string? TipoMovimiento,
    DateTime Fecha,
    string? NroReferencia,
    int Variacion,
    int StockAnterior,
    int StockResultante,
    bool? Conciliado,
    string? Comentario,
    IReadOnlyList<MovimientoLoteDto> Lotes);

public sealed record MovimientoLoteDto(
    string CodigoLote,
    DateTime? Vencimiento,
    int Cantidad);

public sealed class GetMovimientoStockByIdHandler(IDbConnectionFactory dbFactory)
    : IRequestHandler<GetMovimientoStockByIdQuery, MovimientoStockDetailDto>
{
    public async Task<MovimientoStockDetailDto> Handle(
        GetMovimientoStockByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = await dbFactory.CreateAsync(cancellationToken);

        const string sql = """
            SELECT
                m.Id,
                m.IdSucursal,
                m.IdProducto,
                p.Codigo                        AS CodigoProducto,
                p.NombreCompleto                AS NombreProducto,
                m.IdTipoMovimiento,
                t.Nombre                        AS TipoMovimiento,
                m.Fecha,
                m.NroReferencia,
                m.Variacion,
                m.StockAnterior,
                m.StockAnterior + m.Variacion   AS StockResultante,
                m.Conciliado,
                m.Comentario
            FROM stk_MovimientoStock m
            INNER JOIN pro_Producto p ON p.Id = m.IdProducto
            LEFT  JOIN stk_TipoMovimiento t ON t.Id = m.IdTipoMovimiento
            WHERE m.IdSucursal = @IdSucursal AND m.Id = @Id;

            SELECT CodigoLote, Vencimiento, Cantidad
            FROM stk_MovimientoStockLoteVencimiento
            WHERE IdSucursal = @IdSucursal AND Id = @Id;
            """;

        using var multi = await connection.QueryMultipleAsync(
            sql, new { request.IdSucursal, request.Id });

        var header = await multi.ReadSingleOrDefaultAsync<MovimientoStockHeaderRow>();

        if (header is null)
            throw new NotFoundException("MovimientoStock", request.Id);

        var lotes = (await multi.ReadAsync<MovimientoLoteDto>()).ToList();

        return new MovimientoStockDetailDto(
            header.Id,
            header.IdSucursal,
            header.IdProducto,
            header.CodigoProducto,
            header.NombreProducto,
            header.IdTipoMovimiento,
            header.TipoMovimiento,
            header.Fecha,
            header.NroReferencia,
            header.Variacion,
            header.StockAnterior,
            header.StockResultante,
            header.Conciliado,
            header.Comentario,
            lotes);
    }

    // Intermediate record for Dapper mapping before lotes are available
    private sealed record MovimientoStockHeaderRow(
        int Id,
        int IdSucursal,
        int IdProducto,
        string CodigoProducto,
        string NombreProducto,
        int IdTipoMovimiento,
        string? TipoMovimiento,
        DateTime Fecha,
        string? NroReferencia,
        int Variacion,
        int StockAnterior,
        int StockResultante,
        bool? Conciliado,
        string? Comentario);
}
