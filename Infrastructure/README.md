# Store App - Docker Compose Setup

This document provides instructions for running the Store App microservices architecture using Docker Compose.

## 🏗️ Architecture Overview

The Store App consists of the following services:

| Service | Port | Database | Purpose |
|---------|------|----------|---------|
| **API Gateway** | 5000 | - | Routes requests to microservices |
| **Identity Service** | 5001 | store_identity_db | User authentication & authorization |
| **Product Service** | 5002 | store_product_db | Product catalog & inventory |
| **Cart Service** | 5003 | store_cart_db | Shopping cart management |
| **Order Service** | 5004 | store_order_db | Order processing & history |
| **Audit Service** | 5005 | store_audit_db | System audit trails |
| **Frontend** | 3000 | - | React UI application |
| **PostgreSQL** | 5432 | - | Primary database |
| **Redis** | 6379 | - | Caching & session storage |

## 🚀 Quick Start

### Prerequisites
- Docker Desktop installed and running
- Docker Compose v3.8 or higher
- At least 4GB RAM available for containers

### 1. Clone and Navigate
```bash
cd d:\Projekty\store-app\Infrastructure
```

### 2. Build and Start All Services
```bash
# Build and start all services
docker-compose up --build

# Or run in background
docker-compose up --build -d
```

### 3. Initialize Databases (if needed)
```bash
# Run database initialization script
docker exec -it store-postgres psql -U store_user -d store_db -f /docker-entrypoint-initdb.d/init-databases.sql
```

### 4. Access Services
- **Frontend**: http://localhost:3000
- **API Gateway**: http://localhost:5000
- **Identity Service**: http://localhost:5001
- **Product Service**: http://localhost:5002
- **Cart Service**: http://localhost:5003
- **Order Service**: http://localhost:5004
- **Audit Service**: http://localhost:5005

## 📋 Service Management

### Check Service Status
```bash
# View running services
docker-compose ps

# View service logs
docker-compose logs [service-name]

# Follow logs for all services
docker-compose logs -f

# Follow logs for specific service
docker-compose logs -f identity-service
```

### Restart Services
```bash
# Restart all services
docker-compose restart

# Restart specific service
docker-compose restart identity-service

# Rebuild and restart specific service
docker-compose up --build identity-service
```

### Stop Services
```bash
# Stop all services
docker-compose down

# Stop and remove volumes (⚠️ This will delete all data)
docker-compose down -v

# Stop, remove containers, networks, and images
docker-compose down --rmi all
```

## 🔧 Development Configuration

### Environment Variables
Each service uses environment variables for configuration:

```yaml
# Example for Identity Service
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_URLS=http://+:5001
  - ConnectionStrings__DefaultConnection=Host=postgres;Database=store_identity_db;Username=store_user;Password=StrongPassword123!;Port=5432;
  - ConnectionStrings__Redis=redis:6379
  - JwtSettings__SecretKey=your-very-long-secret-key-here-at-least-32-characters-long-for-store-app
```

### Database Connection Strings
Services use the following connection string format:
```
Host=postgres;Database=[database_name];Username=store_user;Password=StrongPassword123!;Port=5432;
```

### Service Communication
Services communicate using internal Docker network:
- **Identity Service**: `http://identity-service:5001`
- **Product Service**: `http://product-service:5002`
- **Cart Service**: `http://cart-service:5003`
- **Order Service**: `http://order-service:5004`

## 🏥 Health Checks

All services include health check endpoints:

```bash
# Check service health
curl http://localhost:5001/health  # Identity Service
curl http://localhost:5002/health  # Product Service
curl http://localhost:5003/health  # Cart Service
curl http://localhost:5004/health  # Order Service
curl http://localhost:5005/health  # Audit Service
curl http://localhost:5000/health  # API Gateway
```

## 🗄️ Database Management

### Access PostgreSQL
```bash
# Connect to PostgreSQL container
docker exec -it store-postgres psql -U store_user -d store_db

# List all databases
\l

# Connect to specific database
\c store_identity_db

# List tables in current database
\dt
```

### Database Migrations
Entity Framework migrations are applied automatically on service startup.

### Backup and Restore
```bash
# Backup all databases
docker exec store-postgres pg_dumpall -U store_user > backup.sql

# Restore from backup
docker exec -i store-postgres psql -U store_user < backup.sql
```

## 🐛 Troubleshooting

### Common Issues

#### Port Already in Use
```bash
# Find process using port
netstat -ano | findstr :5001

# Kill process (replace PID)
taskkill /F /PID [PID]
```

#### Service Won't Start
```bash
# Check service logs
docker-compose logs [service-name]

# Rebuild service
docker-compose build [service-name]
docker-compose up [service-name]
```

#### Database Connection Issues
```bash
# Check PostgreSQL logs
docker-compose logs postgres

# Verify database exists
docker exec -it store-postgres psql -U store_user -d store_db -c "\l"

# Recreate databases
docker exec -it store-postgres psql -U store_user -d store_db -f /docker-entrypoint-initdb.d/init-databases.sql
```

#### Memory Issues
```bash
# Check Docker resource usage
docker stats

# Increase Docker Desktop memory allocation in settings
# Recommended: 4GB+ RAM for all services
```

### Service Dependencies
Services start in the following order due to dependencies:
1. PostgreSQL & Redis
2. Identity Service
3. Product Service
4. Cart Service (depends on Identity & Product)
5. Order Service (depends on Identity, Product & Cart)
6. Audit Service (depends on Identity)
7. API Gateway (depends on all services)
8. Frontend (depends on API Gateway)

## 🔒 Security Notes

- **Default Passwords**: Change default passwords in production
- **JWT Secret**: Use a secure, randomly generated secret key
- **HTTPS**: Enable HTTPS for production deployment
- **Database Access**: Restrict database access in production
- **Container Security**: Services run as non-root users

## 📈 Monitoring

### View Resource Usage
```bash
# Real-time resource usage
docker stats

# View specific service resources
docker stats store-identity-service
```

### Application Logs
```bash
# View application logs
docker-compose logs -f --tail=100

# View specific service logs
docker-compose logs -f identity-service
```

## 🚢 Production Deployment

For production deployment:

1. **Environment Variables**: Use secure values for all secrets
2. **SSL/TLS**: Configure HTTPS certificates
3. **Database**: Use managed database services
4. **Monitoring**: Implement comprehensive logging and monitoring
5. **Scaling**: Use Docker Swarm or Kubernetes for scaling
6. **Security**: Implement proper security measures and access controls

## 📞 Support

If you encounter issues:

1. Check service logs: `docker-compose logs [service-name]`
2. Verify all services are running: `docker-compose ps`
3. Check database connectivity: `docker exec -it store-postgres psql -U store_user -d store_db`
4. Restart problematic services: `docker-compose restart [service-name]`
5. Rebuild if necessary: `docker-compose up --build [service-name]`
