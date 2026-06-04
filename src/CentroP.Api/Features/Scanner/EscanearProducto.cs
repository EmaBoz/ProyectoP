using CentroP.Api.Common.Exceptions;
using CentroP.Api.Common.Interfaces;
using Dapper;
using FluentValidation;
using MediatR;

namespace CentroP.Api.Features.Scanner;

public sealed record EscanearProductoQuery(int IdSucursal, string CodigoBarra)
    : IRequest<ResultadoEscanerDto>;

public sealed record ResultadoEscanerDto(
    int IdProducto,
    string Codigo,
    string NombreCompleto,
    bool Habilitado,
    string CodigoBarra,
    int? StockActual,
    int? StockMinimo,
    int? StockReservado,
    string? CodigoTroquel,
    bool? Trazable,
    bool? Frio,
    bool? Cronico,
    string? Gtin,
    IReadOnlyList<PrecioListaDto> Precios);

public sealed record PrecioListaDto(
    int IdListaPrecio,
    string NombreListaPrecio,
    decimal Precio,
    decimal? VentaSugerido,
    decimal? CostoInformado);

public sealed class EscanearProductoValidator : AbstractValidator<EscanearProductoQuery>
{
    public EscanearProductoValidator()
    {
        RuleFor(x => x.IdSucursal)
            .GreaterThan(0)
            .WithMessage("IdSucursal debe ser mayor a 0.");

        RuleFor(x => x.CodigoBarra)
            .NotEmpty()
            .WithMessage("El código de barras no puede estar vacío.");
    }
}

public sealed class EscanearProductoHandler(IDbConnectionFactory dbFactory)
    : IRequestHandler<EscanearProductoQuery, ResultadoEscanerDto>
{
    public async Task<ResultadoEscanerDto> Handle(
        EscanearProductoQuery request, CancellationToken cancellationToken)
    {
        using var connection = await dbFactory.CreateAsync(cancellationToken);

        const string sql = """
            SELECT TOP 1
                p.Id                AS IdProducto,
                p.Codigo,
                p.NombreCompleto,
                p.Habilitado,
                cb.CodigoBarra,
                s.StockActual,
                s.StockMinimo,
                s.StockReservado,
                dm.CodigoTroquel,
                dm.Trazable,
                dm.Frio,
                dm.Cronico,
                dm.GTIN             AS Gtin
            FROM pro_CodigosBarra cb
            INNER JOIN pro_Producto p ON p.Id = cb.IdProducto
            LEFT  JOIN stk_Stock s
                ON  s.IdProducto = p.Id
                AND s.IdSucursal  = @IdSucursal
            LEFT  JOIN pro_DetalleMedicamento dm ON dm.IdProducto = p.Id
            WHERE cb.CodigoBarra = @CodigoBarra;

            SELECT
                pr.IdListaPrecio,
                lp.Nombre           AS NombreListaPrecio,
                pr.Precio,
                pr.VentaSugerido,
                pr.CostoInformado
            FROM pro_CodigosBarra cb
            INNER JOIN pro_Producto p   ON p.Id  = cb.IdProducto
            INNER JOIN pre_Precio pr    ON pr.IdProducto = p.Id AND pr.FechaBaja IS NULL
            INNER JOIN pre_ListaPrecio lp ON lp.Id = pr.IdListaPrecio AND lp.Habilitada = 1
            WHERE cb.CodigoBarra = @CodigoBarra
            ORDER BY lp.Sistema DESC, lp.Prioridad ASC;
            """;

        using var multi = await connection.QueryMultipleAsync(
            sql, new { request.IdSucursal, request.CodigoBarra });

        var producto = await multi.ReadFirstOrDefaultAsync<ProductoEscanerRow>();

        if (producto is null)
            throw new NotFoundException("CodigoBarra", request.CodigoBarra);

        var precios = (await multi.ReadAsync<PrecioListaDto>()).ToList();

        return new ResultadoEscanerDto(
            producto.IdProducto,
            producto.Codigo,
            producto.NombreCompleto,
            producto.Habilitado,
            producto.CodigoBarra,
            producto.StockActual,
            producto.StockMinimo,
            producto.StockReservado,
            producto.CodigoTroquel,
            producto.Trazable,
            producto.Frio,
            producto.Cronico,
            producto.Gtin,
            precios);
    }

    private sealed record ProductoEscanerRow(
        int IdProducto,
        string Codigo,
        string NombreCompleto,
        bool Habilitado,
        string CodigoBarra,
        int? StockActual,
        int? StockMinimo,
        int? StockReservado,
        string? CodigoTroquel,
        bool? Trazable,
        bool? Frio,
        bool? Cronico,
        string? Gtin);
}
