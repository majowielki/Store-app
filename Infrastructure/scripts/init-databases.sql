-- One database per service, owned by the user this script runs as.
--
-- docker compose mounts it into the postgres image's docker-entrypoint-initdb.d, where it runs
-- once, on a fresh volume, as POSTGRES_USER. On Azure it is run by hand with psql as the
-- application user (docs/runbooks/deploy.md). The schema itself comes from the services'
-- migrations ("--migrate"), so nothing else is created here.

CREATE DATABASE store_identity_db;
CREATE DATABASE store_product_db;
CREATE DATABASE store_cart_db;
CREATE DATABASE store_order_db;
CREATE DATABASE store_audit_db;
CREATE DATABASE store_content_db;
CREATE DATABASE store_payment_db;
