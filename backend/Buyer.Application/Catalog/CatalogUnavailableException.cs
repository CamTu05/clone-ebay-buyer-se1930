namespace Buyer.Application.Catalog;

public sealed class CatalogUnavailableException : Exception
{
    public CatalogUnavailableException() : base("The catalog database is not configured.") { }
}
