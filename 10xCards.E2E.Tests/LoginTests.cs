using Microsoft.Playwright;

namespace _10xCards.E2E.Tests;

public class LoginTests : PlaywrightTestBase
{
    [Fact]
    public async Task Login_WithValidTestUser_RedirectsToCardsPage()
    {
        // Arrange
        var email = "test@10xcards.local";
        var password = "Test123!";

        // Act
        await Page!.GotoAsync($"{BaseUrl}/Account/Login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-testid='email-input']").FillAsync(email);
        await Page.Locator("[data-testid='password-input']").FillAsync(password);
        await Page.Locator("[data-testid='login-button']").ClickAsync();

        // Wait for navigation after login
        await Page.WaitForURLAsync($"{BaseUrl}/cards", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 10000
        });

        // Assert
        Assert.Equal($"{BaseUrl}/cards", Page.Url);
        Assert.True(await IsUserLoggedInAsync());
    }

    [Fact]
    public async Task Login_WithValidAdminUser_RedirectsToCardsPage()
    {
        // Arrange
        var email = "admin@10xcards.local";
        var password = "Admin123!";

        // Act
        await Page!.GotoAsync($"{BaseUrl}/Account/Login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-testid='email-input']").FillAsync(email);
        await Page.Locator("[data-testid='password-input']").FillAsync(password);
        await Page.Locator("[data-testid='login-button']").ClickAsync();

        // Wait for navigation after login
        await Page.WaitForURLAsync($"{BaseUrl}/cards", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 10000
        });

        // Assert
        Assert.Equal($"{BaseUrl}/cards", Page.Url);
        Assert.True(await IsUserLoggedInAsync());
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShowsErrorMessage()
    {
        // Arrange
        var email = "test@10xcards.local";
        var password = "WrongPassword123!";

        // Act
        await Page!.GotoAsync($"{BaseUrl}/Account/Login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-testid='email-input']").FillAsync(email);
        await Page.Locator("[data-testid='password-input']").FillAsync(password);
        await Page.Locator("[data-testid='login-button']").ClickAsync();

        // Wait for page to reload with error
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - URL should contain error parameter
        Assert.Contains("/Account/Login", Page.Url);
        Assert.Contains("error=", Page.Url);
        
        // Check for error alert on page
        var errorAlert = await Page.Locator("[data-testid='error-message']").CountAsync();
        Assert.True(errorAlert > 0);
    }

    [Fact]
    public async Task Login_WithReturnUrl_RedirectsToSpecifiedPage()
    {
        // Arrange
        var email = "test@10xcards.local";
        var password = "Test123!";
        var returnUrl = "/generate";

        // Act
        await Page!.GotoAsync($"{BaseUrl}/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-testid='email-input']").FillAsync(email);
        await Page.Locator("[data-testid='password-input']").FillAsync(password);
        await Page.Locator("[data-testid='login-button']").ClickAsync();

        // Wait for navigation after login
        await Page.WaitForURLAsync($"{BaseUrl}{returnUrl}", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 10000
        });

        // Assert
        Assert.Equal($"{BaseUrl}{returnUrl}", Page.Url);
    }

    [Fact]
    public async Task Login_WithRememberMeChecked_PersistsSession()
    {
        // Arrange
        var email = "test@10xcards.local";
        var password = "Test123!";

        // Act
        await Page!.GotoAsync($"{BaseUrl}/Account/Login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-testid='email-input']").FillAsync(email);
        await Page.Locator("[data-testid='password-input']").FillAsync(password);
        await Page.Locator("[data-testid='remember-me-checkbox']").CheckAsync();
        await Page.Locator("[data-testid='login-button']").ClickAsync();

        // Wait for navigation
        await Page.WaitForURLAsync($"{BaseUrl}/cards", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 10000
        });

        // Get cookies
        var cookies = await Context!.CookiesAsync();
        var authCookie = cookies.FirstOrDefault(c => c.Name.Contains("Identity"));

        // Assert
        Assert.NotNull(authCookie);
        Assert.True(await IsUserLoggedInAsync());
    }

    [Fact]
    public async Task Login_AfterSuccessfulRegistration_ShowsSuccessMessage()
    {
        // Arrange - simulate registration redirect
        await Page!.GotoAsync($"{BaseUrl}/Account/Login?registered=true");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - check for success message
        var successAlert = await Page.Locator("[data-testid='success-message']").CountAsync();
        Assert.True(successAlert > 0);
        
        var successText = await Page.Locator("[data-testid='success-message']").TextContentAsync();
        Assert.Contains("Registration successful", successText);
    }

    [Fact]
    public async Task AccessProtectedPage_WithoutLogin_RedirectsToLoginPage()
    {
        // Act - try to access protected page without logging in
        await Page!.GotoAsync($"{BaseUrl}/cards");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - should be redirected to login page
        Assert.Contains("/Account/Login", Page.Url);
        Assert.Contains("ReturnUrl", Page.Url);
    }
}
