# Store App Configuration Summary

## ✅ Fixed Issues

### 1. Database Connection Standardization
**Problem**: Inconsistent database credentials and connection strings across services.

**Solution**: Standardized all services to use:
- **Username**: `store_user`
- **Password**: `StrongPassword123!`
- **Database naming**: `store_{service}_db` format
- **Connection format**: `Host=localhost;Database={db_name};Username=store_user;Password=StrongPassword123!;Port=5432`

### 2. Port Configuration Consistency
**Problem**: Mismatched ports between Docker containers, appsettings, and launch settings.

**Solution**: Standardized port allocation:
- **API Gateway**: Port 5000 (External), 5100 (HTTPS dev)
- **Identity Service**: Port 5001 (External), 5101 (HTTPS dev)
- **Product Service**: Port 5002 (External), 5102 (HTTPS dev)
- **Cart Service**: Port 5003 (External), 5103 (HTTPS dev)
- **Order Service**: Port 5004 (External), 5104 (HTTPS dev)
- **Audit Service**: Port 5005 (External), 5105 (HTTPS dev)

### 3. Docker Compose Configuration
**Problem**: Root `docker-compose.yml` had incorrect configurations.

**Solution**: 
- Updated to use `build` context instead of pre-built images
- Fixed database environment variables
- Corrected inter-service communication URLs
- Aligned health check endpoints

### 4. JWT Settings Standardization
**Problem**: Inconsistent JWT secret keys across services.

**Solution**: Unified JWT configuration:
```json
{
  "JwtSettings": {
    "SecretKey": "your-very-long-secret-key-here-at-least-32-characters-long-for-store-app",
    "Issuer": "Store.API",
    "Audience": "Store.Client",
    "Authority": "https://localhost:5001",
    "ExpirationInMinutes": 60
  }
}
```

### 5. Service Inter-Communication
**Problem**: Services referencing incorrect ports for other services.

**Solution**: Corrected service URLs:
- Identity Service: `http://identity-service:5001` (Docker) / `https://localhost:5001` (Local)
- Product Service: `http://product-service:5002` (Docker) / `https://localhost:5002` (Local)
- Cart Service: `http://cart-service:5003` (Docker) / `https://localhost:5003` (Local)
- Order Service: `http://order-service:5004` (Docker) / `https://localhost:5004` (Local)
- Audit Service: `http://audit-service:5005` (Docker) / `https://localhost:5005` (Local)

## 📁 Updated Files

### Configuration Files
- ✅ `docker-compose.yml` - Root compose file with corrected configurations
- ✅ All `appsettings.Development.json` files - Standardized database connections
- ✅ All `launchSettings.json` files - Consistent port mappings

### Database Scripts
- ✅ `Infrastructure/scripts/init-databases.sh` - Fixed PostgreSQL syntax

## 🚀 How to Run

### Using Docker Compose (Recommended)
```bash
# From the root directory
docker-compose up --build

# Or using the Infrastructure directory
cd Infrastructure
docker-compose up --build
```

### Running Individual Services (Development)
Each service can be run individually using:
```bash
cd Services/{ServiceName}
dotnet run
```

## 🌐 Service Endpoints

When running with Docker Compose:
- **API Gateway**: http://localhost:5000
- **Identity Service**: http://localhost:5001/swagger
- **Product Service**: http://localhost:5002/swagger
- **Cart Service**: http://localhost:5003/swagger
- **Order Service**: http://localhost:5004/swagger  
- **Audit Service**: http://localhost:5005/swagger
- **PostgreSQL**: localhost:5432
- **Redis**: localhost:6379

## 🎯 Next Steps & Recommendations

### 1. Environment Variables
Consider moving sensitive configurations to environment variables:
```bash
POSTGRES_PASSWORD=StrongPassword123!
JWT_SECRET_KEY=your-secret-key-here
REDIS_CONNECTION=localhost:6379
```

### 2. Health Checks
All services include health check endpoints at `/health`. Monitor these for service availability.

### 3. Database Migrations
Run database migrations for each service:
```bash
cd Services/{ServiceName}
dotnet ef database update
```

### 4. SSL Certificates (Production)
For production deployment, configure proper SSL certificates and update connection strings to use HTTPS.

### 5. Monitoring & Logging
Consider implementing:
- **Application Insights** for Azure deployments
- **Serilog** for structured logging
- **OpenTelemetry** for distributed tracing

### 6. Security Enhancements
- Store JWT secrets in Azure Key Vault or similar
- Implement proper CORS policies
- Add rate limiting to the API Gateway
- Use Azure AD for authentication in production

## 🐛 Troubleshooting

### Common Issues
1. **Database Connection Failed**: Ensure PostgreSQL is running and databases are created
2. **Port Conflicts**: Check if ports 5000-5005 are available
3. **Service Dependencies**: Start services in order: Database → Identity → Others → Gateway

### Useful Commands
```bash
# Check container logs
docker-compose logs [service-name]

# Rebuild specific service  
docker-compose up --build [service-name]

# Reset everything
docker-compose down -v
docker-compose up --build
```

## 📊 Database Schema
Each service has its own dedicated database:
- `store_identity_db` - User accounts, roles, authentication
- `store_product_db` - Product catalog, inventory  
- `store_cart_db` - Shopping cart data
- `store_order_db` - Order processing, history
- `store_audit_db` - System audit logs, tracking

All databases include the `uuid-ossp` extension for UUID generation.
