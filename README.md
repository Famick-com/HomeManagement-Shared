# Famick.HomeManagement.Shared

Shared libraries and core business logic for Famick HomeManagement - a household management system for inventory, recipes, chores, and tasks.

## 📦 NuGet Packages

This repository contains the following packages:

- **Famick.HomeManagement.Domain** - Domain entities, interfaces, and enums
- **Famick.HomeManagement.Core** - Business logic, services, and DTOs
- **Famick.HomeManagement.Infrastructure** - Data access, EF Core, and configurations
- **Famick.HomeManagement.Shared** - Utility helpers, extensions, and localization

## 🏗️ Architecture

These packages support both deployment models:
- **Self-Hosted** (single-tenant, open source)
- **Cloud SaaS** (multi-tenant, managed service)

### Configurable Multi-Tenancy

The infrastructure supports runtime configuration for tenant isolation:

```csharp
// Multi-tenant mode (cloud)
services.AddDbContext<HomeManagementDbContext>(options =>
{
    options.UseNpgsql(connectionString);
}, ServiceLifetime.Scoped);

services.AddSingleton<IMultiTenancyOptions>(new MultiTenancyOptions 
{ 
    IsMultiTenantEnabled = true 
});

// Single-tenant mode (self-hosted)
services.AddSingleton<IMultiTenancyOptions>(new MultiTenancyOptions 
{ 
    IsMultiTenantEnabled = false,
    FixedTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001")
});
```

## 🚀 Getting Started

### Installation

```bash
dotnet add package Famick.HomeManagement.Domain
dotnet add package Famick.HomeManagement.Core
dotnet add package Famick.HomeManagement.Infrastructure
```

### Basic Usage

```csharp
// Configure services
builder.Services.AddDbContext<HomeManagementDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ITenantProvider, YourTenantProvider>();
builder.Services.AddScoped<IStockService, StockService>();
```

## 📚 Documentation

- [Architecture Overview](docs/architecture.md)
- [Multi-Tenancy Guide](docs/multi-tenancy.md)
- [API Reference](docs/api-reference.md)

## 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/famick/homemanagement-shared.git

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test
```

## 📄 License

This project is licensed under the **AGPL-3.0** license - see the [LICENSE](LICENSE) file for details.

## 🔗 Related Projects

- [HomeManagement (Self-Hosted)](https://github.com/famick/homemanagement) - Open source single-tenant version
- [Grocy](https://github.com/grocy/grocy) - Original PHP project this is based on

## 💬 Support

- 📧 Email: support@famick.com
- 🐛 Issues: [GitHub Issues](https://github.com/famick/homemanagement-shared/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/famick/homemanagement-shared/discussions)
