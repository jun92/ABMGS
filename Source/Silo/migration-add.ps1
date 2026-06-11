
$MigrationName = Read-Host "Enter the migration name"

#dotnet ef migrations add $MigrationName -p ..\Package\ -s .
dotnet ef migrations add $MigrationName