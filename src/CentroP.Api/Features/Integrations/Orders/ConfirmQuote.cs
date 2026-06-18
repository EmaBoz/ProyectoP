using CentroP.Api.Common.Interfaces;
using CentroP.Api.Common.Messaging;
using Dapper;
using MediatR;

namespace CentroP.Api.Features.Integrations.Orders;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record PatientDto(
    string FirstName,
    string LastName,
    string Dni,
    string BirthDate);

public sealed record ProviderDto(
    string Name,
    string Branch,
    string Cufe);

public sealed record CredentialDto(
    string CredentialNumber,
    string AffiliateNumber,
    string Financier,
    string Plan);

public sealed record PrescriberDto(
    string LicenseNumber,
    string Name);

public sealed record QuoteItemRequestDto(
    string TroquelCode,
    string AlphabetaCode,
    string BarCode,
    int Quantity);

public sealed record PrescriptionRequestDto(
    string PrescriptionType,
    string PrescriptionId,
    string PrescriptionNumber,
    string Date,
    CredentialDto Credential,
    PrescriberDto Prescriber,
    IReadOnlyList<QuoteItemRequestDto> Items);

public sealed record ConfirmQuoteRequestPayload(
    string OrderNumber,
    PatientDto Patient,
    ProviderDto Provider,
    IReadOnlyList<PrescriptionRequestDto> Prescriptions,
    string Observation,
    string CreatedDate);

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record ConfirmQuoteQuery(
    RequestEnvelope<ConfirmQuoteRequestPayload> Envelope)
    : IRequest<ResponseEnvelope<ConfirmQuoteResponsePayload>>;

// ── Response DTOs ─────────────────────────────────────────────────────────────

public sealed record CredentialResultDto(
    string CredentialNumber,
    string AffiliateNumber,
    string Financier,
    string Plan,
    bool RequiresToken);

public sealed record QuoteItemResponseDto(
    string TroquelCode,
    string AlphabetaCode,
    string BarCode,
    int Quantity,
    decimal UnitPrice,
    decimal CoveragePercentage,
    decimal CoverageAmount,
    decimal PharmacyDiscountPercentage,
    decimal PharmacyDiscountAmount);

public sealed record QuotationResultDto(
    string ReferenceNumber,
    string GeneralResponseCode,
    string Description);

public sealed record PrescriptionResponseDto(
    string PrescriptionType,
    string PrescriptionId,
    string PrescriptionNumber,
    string Date,
    CredentialResultDto Credential,
    PrescriberDto Prescriber,
    IReadOnlyList<QuoteItemResponseDto> Items,
    QuotationResultDto QuotationResult);

public sealed record ConfirmQuoteResponsePayload(
    string OrderNumber,
    PatientDto Patient,
    ProviderDto Provider,
    IReadOnlyList<PrescriptionResponseDto> Prescriptions,
    string Observation,
    string CreatedDate);

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ConfirmQuoteHandler(IDbConnectionFactory dbFactory)
    : IRequestHandler<ConfirmQuoteQuery, ResponseEnvelope<ConfirmQuoteResponsePayload>>
{
    private const decimal CoveragePercent = 40m;

    public async Task<ResponseEnvelope<ConfirmQuoteResponsePayload>> Handle(
        ConfirmQuoteQuery request, CancellationToken cancellationToken)
    {
        var payload = request.Envelope.Data;

        var allBarCodes = payload.Prescriptions
            .SelectMany(p => p.Items)
            .Select(i => i.BarCode)
            .Distinct()
            .ToArray();

        using var connection = await dbFactory.CreateAsync(cancellationToken);

        const string sql = """
            WITH ranked AS (
                SELECT
                    CodigoProducto,
                    VentaSugerido,
                    ROW_NUMBER() OVER (PARTITION BY CodigoProducto ORDER BY Prioridad ASC) AS rn
                FROM dbo.vw_pre_Precio
                WHERE CodigoProducto IN @Codigos
            )
            SELECT CodigoProducto, VentaSugerido
            FROM ranked
            WHERE rn = 1;
            """;

        var priceRows = await connection.QueryAsync<PriceRow>(
            sql, new { Codigos = allBarCodes });

        var priceMap = priceRows.ToDictionary(r => r.CodigoProducto, r => r.VentaSugerido);

        var prescriptionResults = payload.Prescriptions
            .Select(prescription =>
            {
                var items = prescription.Items
                    .Select(item =>
                    {
                        var unitPrice = priceMap.GetValueOrDefault(item.BarCode, 0m);
                        var coverageAmount = Math.Round(unitPrice * item.Quantity * CoveragePercent / 100m, 2);

                        return new QuoteItemResponseDto(
                            item.TroquelCode,
                            item.AlphabetaCode,
                            item.BarCode,
                            item.Quantity,
                            UnitPrice: unitPrice,
                            CoveragePercentage: CoveragePercent,
                            CoverageAmount: coverageAmount,
                            PharmacyDiscountPercentage: 0m,
                            PharmacyDiscountAmount: 0m);
                    })
                    .ToList();

                var credentialResult = new CredentialResultDto(
                    prescription.Credential.CredentialNumber,
                    prescription.Credential.AffiliateNumber,
                    prescription.Credential.Financier,
                    prescription.Credential.Plan,
                    RequiresToken: true);

                return new PrescriptionResponseDto(
                    prescription.PrescriptionType,
                    prescription.PrescriptionId,
                    prescription.PrescriptionNumber,
                    prescription.Date,
                    credentialResult,
                    prescription.Prescriber,
                    items,
                    QuotationResult: new QuotationResultDto(
                        ReferenceNumber: "ADESFA-MOCK-999",
                        GeneralResponseCode: "0",
                        Description: "OK"));
            })
            .ToList();

        var data = new ConfirmQuoteResponsePayload(
            payload.OrderNumber,
            payload.Patient,
            payload.Provider,
            prescriptionResults,
            payload.Observation,
            payload.CreatedDate);

        return new ResponseEnvelope<ConfirmQuoteResponsePayload>(
            Metadata: new EventMetadata(
                EventId: Guid.NewGuid().ToString(),
                TraceId: request.Envelope.Metadata.TraceId),
            Reply: new EventReply(InReplyTo: request.Envelope.Metadata.EventId),
            Data: data);
    }

    private sealed record PriceRow(string CodigoProducto, decimal VentaSugerido);
}
