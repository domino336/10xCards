using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace _10xCards.Components.Pages.Account;

public partial class Register
{
    [Inject] private UserManager<IdentityUser> UserManager { get; set; } = default!;
    [Inject] private SignInManager<IdentityUser> SignInManager { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private RegisterFormModel model = new();
    private bool isLoading;
    private string? errorMessage;

    private async Task HandleRegister()
    {
        isLoading = true;
        errorMessage = null;

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email
        };

        var result = await UserManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await SignInManager.SignInAsync(user, isPersistent: false);
            // forceLoad: true throws NavigationException which bubbles up and triggers redirect
            // This is EXPECTED behavior in Static SSR mode
            Navigation.NavigateTo("/", forceLoad: true);
            return; // This line won't be reached, but keeps code clean
        }
        else
        {
            errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
        }

        isLoading = false;
    }

    public class RegisterFormModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
