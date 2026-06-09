# ERP Platform - Backend

University ERP SaaS Platform backend built with ASP.NET Core 8.

## Stack
- **Framework**: ASP.NET Core 8 (Web API)
- **ORM**: EF Core 8 + MySQL (Pomelo)
- **Patterns**: MediatR 12, FluentValidation, AutoMapper
- **Auth**: JWT (15min access / 7day refresh), BCrypt (work factor 12), Redis sessions
- **Background Jobs**: Hangfire
- **Logging**: Serilog
- **Tests**: xUnit + NetArchTest + Testcontainers

## Prerequisites
- .NET 8 SDK
- Docker & Docker Compose

## Quick Start

```bash
# Start infrastructure
docker-compose up -d mysql redis

# Run migrations
cd src/ERP.Host
dotnet ef database update

# Run API
dotnet run
```

## Docker (full stack)
```bash
docker-compose up -d
```

## Running Tests
```bash
dotnet test
```

## Project Structure

```
src/
  ERP.Host/          - ASP.NET Core entry point, middleware
  ERP.Shared/        - Shared kernel (base entities, abstractions, common)
  Modules/
    ERP.Tenants/     - Multi-tenancy management
    ERP.Auth/        - Authentication (JWT, refresh tokens)
    ERP.RBAC/        - Role-based access control, permissions, menus
    ERP.Users/       - User profiles management

tests/
  ERP.ArchTests/         - Architecture constraint tests
  ERP.UnitTests/         - Unit tests for handlers
  ERP.IntegrationTests/  - Integration tests with Testcontainers
```

## API Endpoints

### Auth
- `POST /api/auth/login` - Login, returns JWT + refresh token
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Revoke refresh token

### Tenants (Super-admin)
- `GET  /api/tenants` - List all tenants
- `GET  /api/tenants/{id}` - Get tenant by ID
- `POST /api/tenants` - Create tenant
- `PUT  /api/tenants/{id}/branding` - Update branding
- `POST /api/tenants/{id}/suspend` - Suspend tenant

### Users
- `GET  /api/users` - List users (tenant-scoped)
- `GET  /api/users/{id}` - Get user
- `POST /api/users` - Create user
- `PUT  /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Deactivate user

### RBAC
- `GET  /api/roles` - List roles
- `POST /api/roles` - Create role
- `POST /api/roles/{roleId}/permissions` - Assign permission
- `POST /api/users/{userId}/roles` - Assign user role
- `GET  /api/menu` - Get menu for current user

## Environment Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__Write` | MySQL write connection |
| `ConnectionStrings__Read` | MySQL read connection |
| `Jwt__Key` | JWT signing key (min 32 chars) |
| `Redis__ConnectionString` | Redis connection |
