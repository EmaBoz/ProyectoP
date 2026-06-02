namespace CentroP.Api.Infrastructure.Cache;

public static class CacheKeys
{
    public const string SucursalesAll = "sucursales:all";
    public const string ProvinciasAll = "provincias:all";
    public const string LaboratoriosAll = "laboratorios:all";

    public static string SucursalById(int id) => $"sucursales:{id}";
}
