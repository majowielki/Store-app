# Docker Compose Setup Guide

## 📋 Overview

The Store App uses Docker Compose for local development and testing, providing a complete microservices environment with all dependencies.

## 🏗️ Architecture

### Services Included
- **Infrastructure Services**: PostgreSQL, Redis, RabbitMQ
- **Microservices**: Identity, Product, Cart, Order, Audit services
- **API Gateway**: Central entry point for all requests
- **Optional Tools**: PgAdmin for database management

### Network Architecture
```
┌─────────────────────────────────────────────────────────────────┐
│                        store-network                            │
│                                                                 │
│  ┌─────────────┐    ┌─────────────────────────────────────────┐  │
│  │   Client    │───▶│         API Gateway                     │  │
│  │  (Port 80)  │    │         (Port 5000)                     │  │
│  └─────────────┘    └─────────────────────────────────────────┘  │
│                                       │                         │
│                     ┌─────────────────┼─────────────────┐       │
│                     ▼                 ▼                 ▼       │
│  ┌─················┐ ┌···············┐ ┌···············┐        │
│  │ Identity Service│ │Product Service│ │  Cart Service │        │
│  │   (Port 5001)   │ │  (Port 5002)  │ │  (Port 5003) │        │
│  └─················┘ └···············┘ └···············┘        │
│                     ┌─────────────────┼─────────────────┐       │
│                     ▼                 ▼                 ▼       │
│  ┌─················┐ ┌···············┐ ┌···············┐        │
│  │  Order Service  │ │ Audit Service │ │               │        │
│  │   (Port 5004)   │ │  (Port 5005)  │ │               │        │
│  └─················┘ └···············┘ └···············┘        │
│                     ┌─────────────────┼─────────────────┐       │
│                     ▼                 ▼                 ▼       │
│  ┌─················┐ ┌···············┐ ┌···············┐        │
│  │   PostgreSQL    │ │     Redis     │ │   RabbitMQ    │        │
│  │   (Port 5432)   │ │  (Port 6379)  │ │  (Port 5672)  │        │
│  └─················┘ └···············┘ └···············┘        │
└─────────────────────────────────────────────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and running
- At least 4GB RAM available for containers
- Ports 5000-5005, 5432, 6379, 5672, 15672 available

### Option 1: Using Quick Start Scripts
```bash
# Windows
.\quick-start.bat

# Linux/Mac
chmod +x quick-start.sh
./quick-start.sh
```

### Option 2: Using Management Script
```powershell
# Build and start all services
.\manage.ps1 build

# Start infrastructure only (for local .NET development)
.\manage.ps1 infra-only

# Show help
.\manage.ps1 help
```

### Option 3: Direct Docker Compose
```bash
# Copy environment file
cp .env.example .env

# Start all services
docker-compose up --build -d

# Start infrastructure only
docker-compose -f docker-compose.infra.yml up -d
```

## 📁 File Structure

```
Store-app/
├── docker-compose.yml              # Main compose file
├── docker-compose.override.yml     # Development overrides (auto-loaded)
├── docker-compose.prod.yml         # Production configuration
├── docker-compose.infra.yml        # Infrastructure services only
├── .env.example                    # Environment variables template
├── .env                           # Your environment variables (create from template)
├── manage.ps1                     # PowerShell management script
├── quick-start.bat                # Windows quick start
└── quick-start.sh                 # Linux/Mac quick start
```

## ⚙️ Configuration

### Environment Variables

The application uses environment variables for configuration. Copy `.env.example` to `.env` and customize:

```bash
# Database
POSTGRES_PASSWORD=StrongPassword123!
POSTGRES_PORT=5432

# JWT Configuration
JWT_SECRET_KEY=your-very-long-secret-key-here
JWT_ISSUER=Store.API
JWT_AUDIENCE=Store.Client

# Service Ports
IDENTITY_SERVICE_PORT=5001
PRODUCT_SERVICE_PORT=5002
# ... etc
```

### Service Configuration

Each service can be configured via environment variables:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ConnectionStrings__DefaultConnection=Host=postgres;Database=store_identity_db;...
  - JwtSettings__SecretKey=${JWT_SECRET_KEY}
```

## 🔧 Development Workflows

### 1. Full Stack Development
Start all services with Docker Compose:
```bash
docker-compose up --build -d
```

### 2. Backend Development
Start infrastructure services, run .NET services locally:
```bash
# Start infrastructure
docker-compose -f docker-compose.infra.yml up -d

# Run services locally
cd Services/IdentityService
dotnet run
```

### 3. Service-Specific Development
Start dependencies, develop specific service:
```bash
# Start dependencies
docker-compose up postgres redis identity-service -d

# Develop product service locally
cd Services/ProductService
dotnet run
```

## 📊 Service Management

### Health Monitoring
All services provide health check endpoints:
- **Detailed Health**: `http://localhost:5001/health`
- **Liveness Probe**: `http://localhost:5001/health/live`
- **Readiness Probe**: `http://localhost:5001/health/ready`

### Logging
View service logs:
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f identity-service

# Recent logs with timestamps
docker-compose logs --tail=100 -t identity-service
```

### Scaling Services
Scale specific services:
```bash
# Scale product service to 3 instances
docker-compose up --scale product-service=3 -d
```

## 🐛 Troubleshooting

### Common Issues

#### Port Conflicts
```bash
# Check what's using a port
netstat -ano | findstr :5001  # Windows
lsof -i :5001                 # Linux/Mac

# Use different ports via environment variables
IDENTITY_SERVICE_PORT=5011 docker-compose up identity-service
```

#### Service Won't Start
```bash
# Check service logs
docker-compose logs identity-service

# Rebuild specific service
docker-compose build identity-service
docker-compose up identity-service
```

#### Database Connection Issues
```bash
# Check PostgreSQL logs
docker-compose logs postgres

# Connect to database manually
docker exec -it store-postgres psql -U store_user -d store_identity_db

# Reset database
docker-compose down -v
docker-compose up postgres -d
```

#### Out of Memory
```bash
# Check resource usage
docker stats

# Increase Docker Desktop memory limit
# Docker Desktop > Settings > Resources > Memory
```

### Debugging Services

#### Attach Debugger to Running Container
```bash
# Run service in debug mode
docker-compose -f docker-compose.yml -f docker-compose.debug.yml up identity-service
```

#### Access Service Logs
```bash
# Real-time logs
docker-compose logs -f identity-service

# Export logs to file
docker-compose logs identity-service > identity-service.log
```

## 🔒 Security Considerations

### Development Environment
- Default passwords are used for convenience
- Services are exposed on localhost
- Debug logging is enabled

### Production Environment
- Use `docker-compose.prod.yml` for production settings
- Set strong passwords via environment variables
- Configure proper network policies
- Enable TLS/SSL certificates
- Set appropriate log levels

### Secrets Management
```bash
# Use Docker secrets for production
echo "strong_password" | docker secret create postgres_password -

# Reference in compose file
secrets:
  - postgres_password
```

## 📈 Performance Optimization

### Resource Limits
Production compose file includes resource limits:
```yaml
deploy:
  resources:
    limits:
      cpus: '0.5'
      memory: 512M
    reservations:
      cpus: '0.1'
      memory: 128M
```

### Database Optimization
```yaml
postgres:
  environment:
    - POSTGRES_SHARED_PRELOAD_LIBRARIES=pg_stat_statements
  command: >
    postgres
    -c shared_buffers=256MB
    -c effective_cache_size=1GB
```

### Caching Strategy
```yaml
redis:
  command: >
    redis-server
    --maxmemory 256mb
    --maxmemory-policy allkeys-lru
    --appendonly yes
```

## 🚢 Deployment Options

### Local Development
```bash
docker-compose up --build -d
```

### Staging Environment
```bash
ASPNETCORE_ENVIRONMENT=Staging docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Production Environment
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### CI/CD Pipeline
```yaml
# Example GitHub Actions workflow
- name: Deploy to Production
  run: |
    docker-compose -f docker-compose.yml -f docker-compose.prod.yml pull
    docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## 📋 Maintenance

### Regular Tasks
```bash
# Update base images
docker-compose pull

# Clean up unused resources
docker system prune -f

# Backup database
docker exec store-postgres pg_dump -U store_user store_db > backup.sql

# Update application
git pull
docker-compose build
docker-compose up -d
```

### Monitoring
```bash
# Resource usage
docker stats

# Service health
curl http://localhost:5000/health

# Database connections
docker exec store-postgres psql -U store_user -d store_db -c "SELECT * FROM pg_stat_activity;"
```

## 🆘 Support

### Useful Commands
```bash
# View all containers
docker-compose ps

# Stop all services
docker-compose down

# Remove volumes (⚠️ This deletes data)
docker-compose down -v

# Rebuild everything
docker-compose build --no-cache
docker-compose up --force-recreate
```

### Getting Help
1. Check service logs: `docker-compose logs [service-name]`
2. Verify service health: `curl http://localhost:5001/health`
3. Check resource usage: `docker stats`
4. Review configuration: `docker-compose config`
5. Consult documentation in the `docs/` folder

---

*For more detailed information about the Store App architecture and development, see the main [README.md](../README.md) file.*
