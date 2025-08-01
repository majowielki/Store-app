#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE DATABASE store_identity_db;
    CREATE DATABASE store_product_db;
    CREATE DATABASE store_cart_db;
    CREATE DATABASE store_order_db;
    
    -- Grant privileges to the user for all databases
    GRANT ALL PRIVILEGES ON DATABASE store_identity_db TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE store_product_db TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE store_cart_db TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE store_order_db TO $POSTGRES_USER;
EOSQL
