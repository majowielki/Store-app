@echo off
echo 🚀 Initializing Store App databases...

REM This script initializes PostgreSQL databases for the Store App microservices
REM It should be run inside the PostgreSQL Docker container

docker exec -it store-postgres psql -U %POSTGRES_USER% -d %POSTGRES_DB% -c "
-- Create databases for each microservice
CREATE DATABASE IF NOT EXISTS store_identity_db;
CREATE DATABASE IF NOT EXISTS store_product_db;
CREATE DATABASE IF NOT EXISTS store_cart_db;
CREATE DATABASE IF NOT EXISTS store_order_db;
CREATE DATABASE IF NOT EXISTS store_audit_db;

-- Grant privileges to the user for all databases
GRANT ALL PRIVILEGES ON DATABASE store_identity_db TO %POSTGRES_USER%;
GRANT ALL PRIVILEGES ON DATABASE store_product_db TO %POSTGRES_USER%;
GRANT ALL PRIVILEGES ON DATABASE store_cart_db TO %POSTGRES_USER%;
GRANT ALL PRIVILEGES ON DATABASE store_order_db TO %POSTGRES_USER%;
GRANT ALL PRIVILEGES ON DATABASE store_audit_db TO %POSTGRES_USER%;
"

echo ✅ Database initialization completed successfully!
echo 📊 Created databases:
echo    - store_identity_db (Identity Service)
echo    - store_product_db (Product Service)
echo    - store_cart_db (Cart Service)
echo    - store_order_db (Order Service)
echo    - store_audit_db (Audit Log Service)

pause
