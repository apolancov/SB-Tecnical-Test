#!/bin/bash

set -e

SQL_SERVER="sqlserver"
SQL_PORT="1433"
SQL_USER="sa"
SQL_DATABASE="${SQL_DATABASE:-ApplicationDb}"

echo "Waiting for SQL Server..."

until /opt/mssql-tools18/bin/sqlcmd \
    -S "${SQL_SERVER},${SQL_PORT}" \
    -U "${SQL_USER}" \
    -P "${MSSQL_SA_PASSWORD}" \
    -C \
    -Q "SELECT 1" \
    > /dev/null 2>&1
do
    echo "SQL Server is not ready. Retrying in 2 seconds..."
    sleep 2
done

echo "SQL Server is ready."

echo "Ensuring database '${SQL_DATABASE}' exists..."

/opt/mssql-tools18/bin/sqlcmd \
    -S "${SQL_SERVER},${SQL_PORT}" \
    -U "${SQL_USER}" \
    -P "${MSSQL_SA_PASSWORD}" \
    -C \
    -Q "IF DB_ID(N'${SQL_DATABASE}') IS NULL CREATE DATABASE [${SQL_DATABASE}];"

echo "Database '${SQL_DATABASE}' is ready."

echo "Creating database schema..."

/opt/mssql-tools18/bin/sqlcmd \
    -S "${SQL_SERVER},${SQL_PORT}" \
    -U "${SQL_USER}" \
    -P "${MSSQL_SA_PASSWORD}" \
    -d "${SQL_DATABASE}" \
    -C \
    -i ./schema.sql

echo "Database schema created."

echo "Seeding database..."

/opt/mssql-tools18/bin/sqlcmd \
    -S "${SQL_SERVER},${SQL_PORT}" \
    -U "${SQL_USER}" \
    -P "${MSSQL_SA_PASSWORD}" \
    -d "${SQL_DATABASE}" \
    -C \
    -i ./seed.sql

echo "Database seed completed."

echo "Database initialization completed successfully."