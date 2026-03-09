using System;
using System.Collections.Generic;

namespace ABMGS.ServerV2.Migrations.Models;

public partial class Orleansquery
{
    public string Querykey { get; set; } = null!;

    public string Querytext { get; set; } = null!;
}
