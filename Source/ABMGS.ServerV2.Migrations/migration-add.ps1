
$MigrationName = Read-Host "Enter the migration name"

dotnet ef migrations add $MigrationName -p ..\ABMGS.Serverv2.Package\