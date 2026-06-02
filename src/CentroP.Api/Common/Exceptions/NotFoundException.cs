namespace CentroP.Api.Common.Exceptions;

public sealed class NotFoundException(string entityName, object id)
    : Exception($"{entityName} con id '{id}' no fue encontrado.");
