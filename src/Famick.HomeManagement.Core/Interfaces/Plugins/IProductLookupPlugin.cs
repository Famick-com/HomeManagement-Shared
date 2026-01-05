namespace Famick.HomeManagement.Core.Interfaces.Plugins;

/// <summary>
/// Interface for product lookup plugins that search external databases
/// (USDA FoodData Central, Open Food Facts, etc.)
/// Plugins are called in the order defined in plugins/config.json
/// </summary>
public interface IProductLookupPlugin : IPlugin
{

    /// <summary>
    /// Process the lookup pipeline. Called for each plugin in config.json order.
    /// The plugin can:
    /// - Add new results to context.Results
    /// - Find and enrich existing results using context.FindMatchingResult()
    /// - Access the original query via context.Query
    /// - Check search type via context.SearchType (Barcode or Name)
    /// </summary>
    /// <param name="context">Pipeline context with accumulated results from previous plugins</param>
    /// <param name="ct">Cancellation token</param>
    Task ProcessPipelineAsync(ProductLookupPipelineContext context, CancellationToken ct = default);
}
