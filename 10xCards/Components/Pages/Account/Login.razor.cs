using Microsoft.AspNetCore.Components;

namespace _10xCards.Components.Pages.Account;

public partial class Login
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    
    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery]
    public bool Registered { get; set; }

    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    private bool isLoading;
    private string? errorMessage;
    private string? successMessage;

    protected override void OnInitialized()
    {
        if (Registered)
        {
            successMessage = "Registration successful! Please log in with your credentials.";
        }
        
        if (!string.IsNullOrEmpty(Error))
        {
            errorMessage = Error;
        }
    }

    private void HandleSubmit()
    {
        isLoading = true;
        // Form will submit naturally to /api/auth/login-form
    }
}

