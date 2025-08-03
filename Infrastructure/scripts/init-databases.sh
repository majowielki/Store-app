#!/bin/bash
set -e

echo "🚀 Initializing Store App databases..."

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    -- Create databases for each microservice
    SELECT 'CREATE DATABASE store_identity_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'store_identity_db')\gexec
    SELECT 'CREATE DATABASE store_product_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'store_product_db')\gexec
    SELECT 'CREATE DATABASE store_cart_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'store_cart_db')\gexec
    SELECT 'CREATE DATABASE store_order_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'store_order_db')\gexec
    SELECT 'CREATE DATABASE store_audit_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'store_audit_db')\gexec
    
    -- Grant privileges to the user for all databases
    GRANT ALL PRIVILEGES ON DATABASE store_identity_db TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE store_product_db TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE store_cart_db TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE store_order_db TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE store_audit_db TO $POSTGRES_USER;
    
    -- Create extensions that might be needed
    \c store_identity_db;
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
    
    \c store_product_db;
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
    
    \c store_cart_db;
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
    
    \c store_order_db;
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
    
    \c store_audit_db;
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
EOSQL

echo "✅ Database initialization completed successfully!"
echo "📊 Created databases:"
echo "   - store_identity_db (Identity Service)"
echo "   - store_product_db (Product Service)"
echo "   - store_cart_db (Cart Service)"
echo "   - store_order_db (Order Service)"
echo "   - store_audit_db (Audit Log Service)"
