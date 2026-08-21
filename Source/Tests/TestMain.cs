using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Google.FlatBuffers;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using YamlDotNet.Core.Tokens;

namespace SyncnetPlatform.Tests;

[Collection("AspireCollection")]
public partial class ABMGS_TestMain : IAsyncLifetime
{
    private readonly AspireAppFixture _appFixture;
    private readonly ITestOutputHelper _output;
    private readonly Random _random = new Random();
    private HttpClient _frontendHttpClient = null!;
    protected CancellationTokenSource defaultTimeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(100000));

    public ABMGS_TestMain(AspireAppFixture fixture, ITestOutputHelper output)
    {
        _appFixture = fixture;
        _output = output;
    }
    public async Task InitializeAsync()
    {
        _frontendHttpClient = await _appFixture.CreateHttpClientToFrontEnd("orleans-frontend");
    }
    public Task DisposeAsync()
    {
        _frontendHttpClient?.Dispose();
        return Task.CompletedTask;
    }
}


