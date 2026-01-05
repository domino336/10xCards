using Microsoft.Playwright;

namespace _10xCards.E2E.Tests;

public class PlaywrightTestBase : IAsyncLifetime
{
    protected IPlaywright? Playwright { get; private set; }
    protected IBrowser? Browser { get; private set; }
    protected IBrowserContext? Context { get; private set; }
    protected IPage? Page { get; private set; }

    protected string BaseUrl { get; set; } = "http://localhost:5077";

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        Context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        Page = await Context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (Page != null)
            await Page.CloseAsync();
        
        if (Context != null)
            await Context.CloseAsync();
        
        if (Browser != null)
            await Browser.CloseAsync();
        
        Playwright?.Dispose();
    }

    protected async Task<bool> IsUserLoggedInAsync()
    {
        try
        {
            await Page!.GotoAsync($"{BaseUrl}/cards");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var currentUrl = Page.Url;
            return !currentUrl.Contains("/Account/Login");
        }
        catch
        {
            return false;
        }
    }
}
