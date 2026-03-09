using System;
using System.Collections.Generic;

namespace ABMGS.ServerV2.Migrations.Models;

public partial class Orleansmembershipversiontable
{
    public string Deploymentid { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public int Version { get; set; }

    public virtual ICollection<Orleansmembershiptable> Orleansmembershiptables { get; set; } = new List<Orleansmembershiptable>();
}
