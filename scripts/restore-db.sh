#!/usr/bin/env bash
# Restores the docker-compose stack's SQL Server database from a backup produced by
# backup-db.sh, overwriting whatever is currently in the database.
#
# Usage: ./scripts/restore-db.sh <backup-file-name>
#   e.g. ./scripts/restore-db.sh SecureAuthentication-20260816-120000.bak
#
# Requires the stack to already be up (`docker compose up -d`) and DB_PASSWORD to be set the
# same way docker-compose.yml itself reads it, either exported in the shell or present in .env.
#
# Stops the api container first so it can't hold a connection open (RESTORE needs exclusive
# access) or serve requests against a database that's mid-restore, then starts it back up.

set -euo pipefail
cd "$(dirname "$0")/.."

if [ -z "${1:-}" ]; then
  echo "Usage: $0 <backup-file-name>" >&2
  echo >&2
  echo "Available backups in ./backups:" >&2
  ls -1 backups 2>/dev/null | sed 's/^/  /' >&2 || echo "  (none found)" >&2
  exit 1
fi

backup_file="$1"

if [ ! -f "backups/${backup_file}" ]; then
  echo "backups/${backup_file} does not exist." >&2
  exit 1
fi

if [ -z "${DB_PASSWORD:-}" ] && [ -f .env ]; then
  DB_PASSWORD=$(grep -E '^DB_PASSWORD=' .env | cut -d '=' -f2-)
fi

if [ -z "${DB_PASSWORD:-}" ]; then
  echo "DB_PASSWORD is not set - export it or put it in .env (see .env.example)." >&2
  exit 1
fi

read -r -p "This overwrites the SecureAuthentication database with backups/${backup_file}. Continue? [y/N] " confirmation
if [[ ! "$confirmation" =~ ^[Yy]$ ]]; then
  echo "Aborted."
  exit 1
fi

echo "Stopping api so it can't hold a connection open during the restore ..."
docker compose stop api

echo "Restoring SecureAuthentication from backups/${backup_file} ..."
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$DB_PASSWORD" -C \
  -Q "ALTER DATABASE [SecureAuthentication] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
      RESTORE DATABASE [SecureAuthentication] FROM DISK = N'/var/opt/mssql/backup/${backup_file}' WITH REPLACE, RECOVERY;
      ALTER DATABASE [SecureAuthentication] SET MULTI_USER;"

echo "Restarting api ..."
docker compose start api

echo "Done."
