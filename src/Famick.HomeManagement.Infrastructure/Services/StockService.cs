using AutoMapper;
using Famick.HomeManagement.Core.DTOs.Stock;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Domain.Enums;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Famick.HomeManagement.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly HomeManagementDbContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantProvider _tenantProvider;

    public StockService(HomeManagementDbContext context, IMapper mapper, ITenantProvider tenantProvider)
    {
        _context = context;
        _mapper = mapper;
        _tenantProvider = tenantProvider;
    }

    public async Task<StockEntryDto> AddStockAsync(AddStockRequest request, CancellationToken cancellationToken = default)
    {
        // Validate product exists
        var product = await _context.Products
            .Include(p => p.QuantityUnitStock)
            .Include(p => p.Barcodes)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new EntityNotFoundException(nameof(Product), request.ProductId);
        }

        // Validate location if provided
        if (request.LocationId.HasValue)
        {
            var locationExists = await _context.Locations
                .AnyAsync(l => l.Id == request.LocationId.Value, cancellationToken);
            if (!locationExists)
            {
                throw new EntityNotFoundException(nameof(Location), request.LocationId.Value);
            }
        }

        var entry = new StockEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId ?? throw new InvalidOperationException("Tenant ID not set"),
            ProductId = request.ProductId,
            Amount = request.Amount,
            BestBeforeDate = request.BestBeforeDate,
            PurchasedDate = request.PurchasedDate ?? DateTime.UtcNow,
            StockId = Guid.NewGuid().ToString(),
            Price = request.Price,
            Open = false,
            LocationId = request.LocationId ?? product.LocationId,
            ShoppingLocationId = request.ShoppingLocationId,
            Note = request.Note
        };

        _context.Stock.Add(entry);

        // Create stock log entry
        var log = CreateStockLog(entry, "purchase", request.Amount);
        _context.StockLog.Add(log);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entry.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created stock entry");
    }

    public async Task<StockEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Stock
            .Include(s => s.Product)
                .ThenInclude(p => p!.QuantityUnitStock)
            .Include(s => s.Product)
                .ThenInclude(p => p!.Barcodes)
            .Include(s => s.Location)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entry == null) return null;

        return MapToDto(entry);
    }

    public async Task<List<StockEntryDto>> ListAsync(StockFilterRequest? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Stock
            .Include(s => s.Product)
                .ThenInclude(p => p!.QuantityUnitStock)
            .Include(s => s.Product)
                .ThenInclude(p => p!.Barcodes)
            .Include(s => s.Location)
            .AsQueryable();

        if (filter != null)
        {
            if (filter.ProductId.HasValue)
                query = query.Where(s => s.ProductId == filter.ProductId.Value);

            if (filter.LocationId.HasValue)
                query = query.Where(s => s.LocationId == filter.LocationId.Value);

            if (filter.Open.HasValue)
                query = query.Where(s => s.Open == filter.Open.Value);

            if (filter.ExpiredOnly == true)
                query = query.Where(s => s.BestBeforeDate.HasValue && s.BestBeforeDate.Value < DateTime.UtcNow);

            if (filter.ExpiringWithinDays.HasValue)
            {
                var expiryDate = DateTime.UtcNow.AddDays(filter.ExpiringWithinDays.Value);
                query = query.Where(s => s.BestBeforeDate.HasValue && s.BestBeforeDate.Value <= expiryDate);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(s => s.Product != null && s.Product.Name.ToLower().Contains(term));
            }

            // Sorting
            query = filter.SortBy?.ToLower() switch
            {
                "amount" => filter.Descending ? query.OrderByDescending(s => s.Amount) : query.OrderBy(s => s.Amount),
                "bestbeforedate" => filter.Descending ? query.OrderByDescending(s => s.BestBeforeDate) : query.OrderBy(s => s.BestBeforeDate),
                "purchaseddate" => filter.Descending ? query.OrderByDescending(s => s.PurchasedDate) : query.OrderBy(s => s.PurchasedDate),
                "productname" => filter.Descending ? query.OrderByDescending(s => s.Product!.Name) : query.OrderBy(s => s.Product!.Name),
                _ => query.OrderBy(s => s.BestBeforeDate).ThenBy(s => s.PurchasedDate) // Default: FEFO then FIFO
            };
        }
        else
        {
            query = query.OrderBy(s => s.BestBeforeDate).ThenBy(s => s.PurchasedDate);
        }

        var entries = await query.ToListAsync(cancellationToken);
        return entries.Select(MapToDto).ToList();
    }

    public async Task<List<StockEntryDto>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await ListAsync(new StockFilterRequest { ProductId = productId }, cancellationToken);
    }

    public async Task<List<StockEntryDto>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await ListAsync(new StockFilterRequest { LocationId = locationId }, cancellationToken);
    }

    public async Task<List<StockEntryDto>> GetByProductAndLocationAsync(Guid productId, Guid locationId, CancellationToken cancellationToken = default)
    {
        return await ListAsync(new StockFilterRequest { ProductId = productId, LocationId = locationId }, cancellationToken);
    }

    public async Task<StockEntryDto> AdjustStockAsync(Guid id, AdjustStockRequest request, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Stock
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entry == null)
        {
            throw new EntityNotFoundException(nameof(StockEntry), id);
        }

        // Log old values
        var oldLog = CreateStockLog(entry, "stock-edit-old", entry.Amount);
        _context.StockLog.Add(oldLog);

        // Update entry
        var oldAmount = entry.Amount;
        entry.Amount = request.Amount;

        if (request.BestBeforeDate.HasValue)
            entry.BestBeforeDate = request.BestBeforeDate;

        if (request.LocationId.HasValue)
            entry.LocationId = request.LocationId;

        if (request.Note != null)
            entry.Note = request.Note;

        entry.UpdatedAt = DateTime.UtcNow;

        // Log new values
        var newLog = CreateStockLog(entry, "stock-edit-new", entry.Amount);
        newLog.CorrelationId = oldLog.CorrelationId;
        _context.StockLog.Add(newLog);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated stock entry");
    }

    public async Task<StockEntryDto> OpenProductAsync(Guid id, OpenProductRequest request, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Stock
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entry == null)
        {
            throw new EntityNotFoundException(nameof(StockEntry), id);
        }

        if (entry.Open)
        {
            throw new InvalidOperationException("Stock entry is already open");
        }

        // Store original amount before opening
        entry.OriginalAmount = entry.Amount;
        entry.Open = true;
        entry.OpenedDate = DateTime.UtcNow;
        entry.OpenTrackingMode = request.TrackingMode;
        entry.Amount = request.RemainingAmount;
        entry.UpdatedAt = DateTime.UtcNow;

        // Create log entry
        var log = CreateStockLog(entry, "product-opened", request.RemainingAmount);
        _context.StockLog.Add(log);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated stock entry");
    }

    public async Task ConsumeStockAsync(Guid id, ConsumeStockRequest request, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Stock
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entry == null)
        {
            throw new EntityNotFoundException(nameof(StockEntry), id);
        }

        var consumeAmount = request.Amount ?? entry.Amount;

        if (consumeAmount > entry.Amount)
        {
            throw new InsufficientStockException(entry.ProductId, consumeAmount, entry.Amount);
        }

        // Create log entry
        var log = CreateStockLog(entry, "consume", -consumeAmount);
        log.Spoiled = request.Spoiled ? 1 : 0;
        log.RecipeId = request.RecipeId;
        log.UsedDate = DateTime.UtcNow;
        _context.StockLog.Add(log);

        if (consumeAmount >= entry.Amount)
        {
            // Remove the entire entry
            _context.Stock.Remove(entry);
        }
        else
        {
            // Reduce the amount
            entry.Amount -= consumeAmount;
            entry.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Stock.FindAsync(new object[] { id }, cancellationToken);

        if (entry == null)
        {
            throw new EntityNotFoundException(nameof(StockEntry), id);
        }

        // Create inventory correction log
        var log = CreateStockLog(entry, "inventory-correction", -entry.Amount);
        _context.StockLog.Add(log);

        _context.Stock.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private StockLog CreateStockLog(StockEntry entry, string transactionType, decimal amount)
    {
        return new StockLog
        {
            Id = Guid.NewGuid(),
            TenantId = entry.TenantId,
            ProductId = entry.ProductId,
            Amount = amount,
            BestBeforeDate = entry.BestBeforeDate,
            PurchasedDate = entry.PurchasedDate,
            StockId = entry.StockId,
            TransactionType = transactionType,
            Price = entry.Price,
            LocationId = entry.LocationId,
            ShoppingLocationId = entry.ShoppingLocationId,
            StockRowId = entry.Id,
            OpenedDate = entry.OpenedDate,
            CorrelationId = Guid.NewGuid().ToString(),
            Note = entry.Note,
            UserId = _tenantProvider.UserId ?? throw new InvalidOperationException("User ID not set")
        };
    }

    private StockEntryDto MapToDto(StockEntry entry)
    {
        return new StockEntryDto
        {
            Id = entry.Id,
            ProductId = entry.ProductId,
            ProductName = entry.Product?.Name ?? string.Empty,
            ProductBarcode = entry.Product?.Barcodes?.FirstOrDefault()?.Barcode,
            Amount = entry.Amount,
            BestBeforeDate = entry.BestBeforeDate,
            PurchasedDate = entry.PurchasedDate,
            StockId = entry.StockId,
            Price = entry.Price,
            Open = entry.Open,
            OpenedDate = entry.OpenedDate,
            OpenTrackingMode = entry.OpenTrackingMode,
            OriginalAmount = entry.OriginalAmount,
            LocationId = entry.LocationId,
            LocationName = entry.Location?.Name,
            ShoppingLocationId = entry.ShoppingLocationId,
            Note = entry.Note,
            QuantityUnitName = entry.Product?.QuantityUnitStock?.Name ?? string.Empty,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt ?? entry.CreatedAt
        };
    }
}
