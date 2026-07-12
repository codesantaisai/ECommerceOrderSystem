<p align="right"><strong>Candidate:</strong> Jathurshan Santhirasekaram</p>

# ECommerce Order System

An ASP.NET Core MVC application for managing products and customer orders. The application supports customer registration and ordering, administrator product management, role-based authorization, and order status workflows.

- **Live application:** [ecommerceordersystem.runasp.net](http://ecommerceordersystem.runasp.net/)
- **GitHub repository:** [codesantaisai/ECommerceOrderSystem](https://github.com/codesantaisai/ECommerceOrderSystem)

Zip folder also attached.

The project is deployed to the MonsterASP.NET cloud hosting provider using Web Deploy.

## Technology stack

- .NET 8 / ASP.NET Core MVC
- Entity Framework Core 8 with SQL Server
- ASP.NET Core Identity and JWT bearer authentication
- Razor views, Bootstrap, and jQuery validation
- Serilog file and console logging

## Prerequisites

Install the following before running the project locally:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (Developer/Express/LocalDB) or access to a SQL Server instance
- Optional: Visual Studio 2022 with the **ASP.NET and web development** workload

## Setup

1. Clone the repository and enter its directory:

   ```bash
   git clone https://github.com/codesantaisai/ECommerceOrderSystem.git
   cd ECommerceOrderSystem
   ```

2. Restore the solution dependencies:

   ```bash
   dotnet restore ECommerceOrderSystem.sln
   ```

3. Configure the database connection and local secrets as described below.

4. Apply the existing EF Core migrations:

   ```bash
   dotnet tool restore
   dotnet ef database update --project ECommerceOrderSystem.Infrastructure --startup-project ECommerceOrderSystem
   ```

5. Run the web application:

   ```bash
   dotnet run --project ECommerceOrderSystem
   ```

   With the checked-in launch profile, the application is available at `https://localhost:7150` and `http://localhost:5027`. If local HTTPS is not trusted, run `dotnet dev-certs https --trust` or use the HTTP address.

## Connection string and secrets

The application reads the SQL Server connection from `ConnectionStrings:DefaultConnection`. The checked-in `appsettings.json` value is intended only as a local example. Change the server and authentication options to match your SQL Server installation.

Example using Windows authentication:

```text
Server=localhost;Database=ECommerceOrderSystem;Trusted_Connection=True;TrustServerCertificate=True
```

Example using SQL Server authentication:

```text
Server=localhost;Database=ECommerceOrderSystem;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True
```

On startup, the application creates the `ADMIN` and `CUSTOMER` roles if needed and seeds the default administrator account if it does not already exist. The default credentials are:

- **Email:** `admin@gmail.com`
- **Password:** `Admin@12345`

These credentials are configured in the `Admin` section of `ECommerceOrderSystem/appsettings.json`. Database migrations must be applied before the first start.

## Architecture decisions

The solution uses a small layered architecture:

- **`ECommerceOrderSystem.Domain`** contains entities, shared models, enums, and view models.
- **`ECommerceOrderSystem.Application`** contains service contracts and business/application services for products, orders, lifecycle rules, and JWT creation.
- **`ECommerceOrderSystem.Infrastructure`** contains the EF Core database context, entity configurations, migrations, and identity seed data.
- **`ECommerceOrderSystem`** is the MVC presentation and composition layer. It configures dependency injection, authentication, logging, routing, controllers, Razor views, and static assets.

Controllers depend on service interfaces rather than directly implementing business workflows. EF Core configurations keep database mapping separate from the domain entities, while dependency injection wires the layers together in the web application's composition root.

## Trade-offs

- **Layered solution vs. simplicity:** Separate projects improve separation of concerns and make business logic easier to test or replace, but add references and ceremony for a relatively small application.
- **Startup seeding:** Automatically ensuring roles and the initial admin account makes first-run setup easy, but production credentials must be supplied securely and rotated independently of source control.
- **File logging:** Rolling local log files are convenient during development and simple hosting, but distributed deployments should use centralized, structured log storage.

## Useful commands

Build the complete solution:

```bash
dotnet build ECommerceOrderSystem.sln
```

Create a migration after changing the data model:

```bash
dotnet ef migrations add MigrationName --project ECommerceOrderSystem.Infrastructure --startup-project ECommerceOrderSystem
```

Apply pending migrations:

```bash
dotnet ef database update --project ECommerceOrderSystem.Infrastructure --startup-project ECommerceOrderSystem
```
