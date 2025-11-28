using Microsoft.EntityFrameworkCore;
using SyncnetPlatform.Databases;


public class SyncnetDbContextExtend : SyncnetDbContext
{
    public SyncnetDbContextExtend(DbContextOptions<SyncnetDbContext> options) : base(options)
    {
    }
}

public class PlayerDataModelExtend
{
    public int Level { get; set; }
    public ulong Exp { get; set; }
}

