using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AttendanceApp.Api.Common.Requests.Users;
using AttendanceApp.Domain.Enums;
using AttendanceApp.IntegrationTests.Fixtures;
using AttendanceApp.IntegrationTests.Helpers;

namespace AttendanceApp.IntegrationTests.Controllers;

public sealed class AuthMvcControllerIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private HttpClient _httpClient = null!;

    public AuthMvcControllerIntegrationTests()
    {
        _factory = new IntegrationTestWebApplicationFactory();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _httpClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();
        await _factory.CleanupAsync();
        await _factory.DisposeAsync();
    }

    // ---------- Login Page Tests ----------

    [Fact]
    public async Task Login_Get_ReturnsSuccessAndHtml()
    {
        var response = await _httpClient.GetAsync("/auth/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_Get_ContainsLoginForm()
    {
        var response = await _httpClient.GetAsync("/auth/login");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Should contain login-related content
        Assert.Contains("login", content, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Register Page Tests ----------

    [Fact]
    public async Task Register_Get_ReturnsSuccessAndHtml()
    {
        var response = await _httpClient.GetAsync("/auth/register");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Register_Get_ContainsRegisterForm()
    {
        var response = await _httpClient.GetAsync("/auth/register");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Should contain register-related content
        Assert.Contains("register", content, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Route Tests ----------

    [Fact]
    public async Task Auth_InvalidRoute_ReturnsNotFound()
    {
        var response = await _httpClient.GetAsync("/auth/invalid-route");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
