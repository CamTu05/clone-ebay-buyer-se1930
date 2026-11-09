namespace Buyer.Application.Interfaces.ListingCatalog;

public sealed class CatalogUnavailableException : Exception
{
    public CatalogUnavailableException() : base("The catalog database is not configured.") { }
}
