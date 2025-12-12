using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace _10xCards.Components.Pages.Account;

public partial class Login
{
    [Inject] private SignInManager<IdentityUser> SignInManager { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromForm]
    private LoginFormModel model { get; set; } = new();
    
    private bool isLoading;
    private string? errorMessage;

    private async Task HandleLogin()
    {
        isLoading = true;
        errorMessage = null;

        var result = await SignInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var returnUrl = string.IsNullOrEmpty(ReturnUrl) ? "/" : ReturnUrl;
            // forceLoad: true throws NavigationException which bubbles up and triggers redirect
            // This is EXPECTED behavior in Static SSR mode
            Navigation.NavigateTo(returnUrl, forceLoad: true);
            return; // This line won't be reached, but keeps code clean
        }
        else if (result.IsLockedOut)
        {
            errorMessage = "Your account has been locked out. Please try again later.";
        }
        else if (result.RequiresTwoFactor)
        {
            errorMessage = "Two-factor authentication is required.";
        }
        else
        {
            errorMessage = "Invalid email or password.";
        }

        isLoading = false;
    }

    public class LoginFormModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
