# 10xCards E2E Tests

## Overview

This project contains end-to-end tests for the 10xCards application using Playwright for browser automation.

## Prerequisites

- .NET 9 SDK
- Playwright browsers installed

## Initial Setup

After building the project for the first time, you need to install Playwright browsers:

```bash
# From the solution root directory
pwsh 10xCards.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install
```

Or on Linux/macOS:
```bash
# From the solution root directory
10xCards.E2E.Tests/bin/Debug/net9.0/playwright.sh install
```

Alternatively, you can run:
```bash
dotnet build 10xCards.E2E.Tests
playwright install
```

## Running Tests

### Starting the Application

Before running E2E tests, you need to start the 10xCards application:

```bash
# From the solution root directory
dotnet run --project 10xCards
```

The application should be running on `http://localhost:5077` (default from launchSettings.json).

### Running All E2E Tests

```bash
# From the solution root directory
dotnet test 10xCards.E2E.Tests
```

### Running Specific Test Class

```bash
dotnet test 10xCards.E2E.Tests --filter "FullyQualifiedName~LoginTests"
```

### Running with Detailed Output

```bash
dotnet test 10xCards.E2E.Tests --verbosity normal
```

### Running in Headed Mode (with visible browser)

Modify the `PlaywrightTestBase.cs` file and set `Headless = false` in the browser launch options:

```csharp
Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false  // Change this to see the browser
});
```

## Test Structure

### Base Class

- **PlaywrightTestBase**: Base class for all E2E tests
  - Handles Playwright initialization and cleanup
  - Provides `Page`, `Browser`, `Context` objects
  - Configures base URL (`http://localhost:5077` by default)
  - Includes helper methods like `IsUserLoggedInAsync()`

### Test Classes

- **LoginTests**: Tests for authentication functionality
  - Login with test user credentials
  - Login with admin user credentials
  - Login with invalid credentials
  - Login with returnUrl parameter
  - Remember me functionality
  - Registration success message display

## Test Accounts

The following test accounts are automatically seeded in Development environment:

- **Test User**
  - Email: `test@10xcards.local`
  - Password: `Test123!`
  - Role: Regular user

- **Admin User**
  - Email: `admin@10xcards.local`
  - Password: `Admin123!`
  - Role: Admin

## Configuration

The tests are configured to:
- Run against `http://localhost:5077` by default
- Use Chromium browser in headless mode
- Ignore HTTPS errors (for local development)
- Wait for NetworkIdle state after navigation

You can modify these settings in the `PlaywrightTestBase.cs` file.

## Troubleshooting

### Playwright Not Installed

If you get an error about Playwright not being installed:
```
Executable doesn't exist at ...
```

Run the installation command:
```bash
pwsh 10xCards.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install
```

### Application Not Running

If tests fail with connection errors, make sure the application is running:
```bash
dotnet run --project 10xCards
```

### Port Already in Use

If port 5077 is already in use, you can:
1. Stop the conflicting process
2. Or modify `BaseUrl` in `PlaywrightTestBase.cs` to use a different port
3. Update `launchSettings.json` in the main 10xCards project

### Database Issues

The tests use the same SQLite database as the application. If you encounter data-related issues:
```bash
# Reset the database (from solution root)
reset db.bat
```

Or manually delete `cards.db` and restart the application to recreate test users.

## Adding New Tests

1. Create a new test class that inherits from `PlaywrightTestBase`
2. Use xUnit `[Fact]` or `[Theory]` attributes
3. Access the page via the `Page` property
4. Follow the Arrange-Act-Assert pattern

Example:
```csharp
public class MyNewTests : PlaywrightTestBase
{
    [Fact]
    public async Task MyTest_Description()
    {
        // Arrange
        await Page!.GotoAsync($"{BaseUrl}/some-page");
        
        // Act
        await Page.ClickAsync("button");
        
        // Assert
        Assert.Contains("expected-text", Page.Url);
    }
}
```

## CI/CD Integration

These tests can be integrated into GitHub Actions or other CI/CD pipelines:

1. Build the application
2. Start the application in background
3. Install Playwright browsers
4. Run E2E tests
5. Stop the application

Example GitHub Actions workflow snippet:
```yaml
- name: Install Playwright Browsers
  run: pwsh 10xCards.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install

- name: Start Application
  run: |
    dotnet run --project 10xCards &
    sleep 10

- name: Run E2E Tests
  run: dotnet test 10xCards.E2E.Tests
```

## Resources

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [xUnit Documentation](https://xunit.net/)
- [Test Plan](.ai/test-plan.md)
