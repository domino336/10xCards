using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace _10xCards.Components.Pages.Account;

public partial class Login
{
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

        try
        {
            var result = await SignInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var returnUrl = string.IsNullOrEmpty(ReturnUrl) ? "/" : ReturnUrl;
                Navigation.NavigateTo(returnUrl, forceLoad: true);
                return;
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
        }
        catch (NavigationException)
        {
            // Navigation exceptions are expected when redirecting after login
            return;
        }
        catch (Exception ex)
        {
            errorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
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
