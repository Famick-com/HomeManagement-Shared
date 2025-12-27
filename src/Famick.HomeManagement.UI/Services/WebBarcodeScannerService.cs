using Famick.HomeManagement.Core.Interfaces;

namespace Famick.HomeManagement.UI.Services;

/// <summary>
/// Web/WASM implementation of barcode scanner (not available - camera access not supported in web).
/// For mobile devices, use MauiBarcodeScannerService instead.
/// </summary>
public class WebBarcodeScannerService : IBarcodeScannerService
{
    /// <summary>
    /// Always returns false for web platform.
    /// Camera-based barcode scanning is only available on mobile (MAUI).
    /// </summary>
    public bool IsAvailable => false;

    /// <summary>
    /// Not implemented for web platform.
    /// </summary>
    public Task<string?> ScanBarcodeAsync(CancellationToken ct = default)
    {
        // Camera-based barcode scanning is not available on web
        return Task.FromResult<string?>(null);
    }
}
