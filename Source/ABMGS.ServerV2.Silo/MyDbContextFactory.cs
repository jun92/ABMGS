//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;
//using System;
//using System.Collections.Generic;
//using System.Text;


//namespace ABMGS.ServerV2.Silo;

//public class SyncnetDbContextExtendFactory
//    : IDesignTimeDbContextFactory<SyncnetDbContextExtend>
//{
//    public SyncnetDbContextExtend CreateDbContext(string[] args)
//    {
//        var optionsBuilder = new DbContextOptionsBuilder<SyncnetDbContextExtend>();

//        // 디자인 타임용 연결 문자열 (보통 appsettings.json에서 읽거나 하드코딩)
//        optionsBuilder.UseNpgsql(
//            "Host=localhost;Database=syncnet;Username=...;Password=...");

//        return new SyncnetDbContextExtend(optionsBuilder.Options);
//    }
//}
