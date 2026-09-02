-- Creates one database per service (database-per-service, ADR-0008).
--
-- Postgres runs everything in /docker-entrypoint-initdb.d exactly once, when the data directory is
-- first initialised — so this fires on a fresh volume and after `./scripts/dev-down.sh -v`, which is
-- precisely when these databases are missing. It is not re-run on an ordinary container restart.
--
-- Without this the databases did not exist, and every `db-migrate.sh` run logged two
-- `Connection[20004] An error occurred using the connection to database 'x'` errors per service
-- before EF quietly created the database itself as a side effect of MigrateAsync(). Provisioning a
-- database is not the application's job — in a deployed environment Terraform does it, and the
-- service is only ever handed a connection string to one that already exists.
--
-- POSTGRES_DB (eventplatform) is still created by the image itself and left unused; these nine are
-- what the services actually connect to.

CREATE DATABASE catalog;
CREATE DATABASE communication;
CREATE DATABASE identity;
CREATE DATABASE inventory;
CREATE DATABASE ordering;
CREATE DATABASE payments;
CREATE DATABASE queue;
CREATE DATABASE ticketing;
CREATE DATABASE venue;
