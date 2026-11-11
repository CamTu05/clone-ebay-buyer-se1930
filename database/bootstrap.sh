#!/usr/bin/env bash
set -euo pipefail
SQLCMD=/opt/mssql-tools18/bin/sqlcmd
existing="$($SQLCMD -S sqlserver -U sa -C -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('CloneEbayBuyer') IS NULL THEN 0 ELSE 1 END" | tr -d '[:space:]')"
if [ "$existing" = "0" ]; then
 "$SQLCMD" -S sqlserver -U sa -C -I -b -i /scripts/CloneEbayBuyer.sql
 "$SQLCMD" -S sqlserver -U sa -C -I -b -i /scripts/CloneEbayBuyer.Seed.sql
 "$SQLCMD" -S sqlserver -U sa -C -I -b -i /scripts/CloneEbayBuyer.Passwords.sql
fi
"$SQLCMD" -S sqlserver -U sa -C -I -b -d CloneEbayBuyer -i /scripts/upgrades/001-buyer-completion.sql
