-- PostgreSQL Database Initialization Script for Store App
-- This script creates all necessary databases for the microservices architecture

-- Drop databases if they exist (uncomment if you need to reset)
-- DROP DATABASE IF EXISTS store_identity_db;
-- DROP DATABASE IF EXISTS store_product_db;
-- DROP DATABASE IF EXISTS store_cart_db;
-- DROP DATABASE IF EXISTS store_order_db;
-- DROP DATABASE IF EXISTS store_audit_db;

-- Create databases for each microservice
CREATE DATABASE store_identity_db
    WITH 
    OWNER = store_user
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

CREATE DATABASE store_product_db
    WITH 
    OWNER = store_user
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

CREATE DATABASE store_cart_db
    WITH 
    OWNER = store_user
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

CREATE DATABASE store_order_db
    WITH 
    OWNER = store_user
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

CREATE DATABASE store_audit_db
    WITH 
    OWNER = store_user
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

-- Grant all privileges to the postgres user for all databases
GRANT ALL PRIVILEGES ON DATABASE store_identity_db TO store_user;
GRANT ALL PRIVILEGES ON DATABASE store_product_db TO store_user;
GRANT ALL PRIVILEGES ON DATABASE store_cart_db TO store_user;
GRANT ALL PRIVILEGES ON DATABASE store_order_db TO store_user;
GRANT ALL PRIVILEGES ON DATABASE store_audit_db TO store_user;

-- Connect to each database and create useful extensions
\c store_identity_db;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

\c store_product_db;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

\c store_cart_db;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

\c store_order_db;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

\c store_audit_db;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Display success message
SELECT 'Database initialization completed successfully!' as message;
