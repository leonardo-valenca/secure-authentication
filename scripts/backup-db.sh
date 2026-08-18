#!/usr/bin/env bash
# Backs up the running docker-compose stack's SQL Server database to ./backups on the host.
#
# Usage: ./scripts/backup-db.sh
#
# Requires the stack to already be up (`docker compose up -d`) and DB_PASSWORD to be set the
# same way docker-compose.yml itself reads it, either exported in the shell or present in .env,
# which `docker compose` (and this script) reads automatically.

set -euo pipefail
cd "$(dirname "$0")/.."

if [ -z "${DB_PASSWORD:-}" ] && [ -f .env ]; then
  DB_PASSWORD=$(grep -E '^DB_PASSWORD=' .env | cut -d '=' -f2-)
fi

if [ -z "${DB_PASSWORD:-}" ]; then
  echo "DB_PASSWORD is not set - export it or put it in .env (see .env.example)." >&2
  exit 1
fi

mkdir -p backups
timestamp=$(date +%Y%m%d-%H%M%S)
file_name="SecureAuthentication-${timestamp}.bak"

echo "Backing up SecureAuthentication to backups/${file_name} ..."

docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$DB_PASSWORD" -C \
  -Q "BACKUP DATABASE [SecureAuthentication] TO DISK = N'/var/opt/mssql/backup/${file_name}' WITH FORMAT, INIT, COMPRESSION;"

echo "Done: backups/${file_name}"
