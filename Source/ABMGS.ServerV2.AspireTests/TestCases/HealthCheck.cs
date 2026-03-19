using System;
using System.Collections.Generic;
using System.Text;

namespace ABMGS.ServerV2.AspireTest;

public partial class ABMGS_TestMain : IAsyncLifetime
{
    [Fact]
    public async Task HeathCheck()
    {
        var response = await _frontendHttpClient.GetAsync("/api/healthy");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}