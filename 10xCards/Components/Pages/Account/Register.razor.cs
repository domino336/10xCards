using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace _10xCards.Components.Pages.Account;

public partial class Register
{
    private RegisterFormModel model = new();
    private bool isLoading;
    private string? errorMessage;

    private async Task HandleRegister()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await UserManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await SignInManager.SignInAsync(user, isPersistent: false);
                Navigation.NavigateTo("/", forceLoad: true);
            }
            else
            {
                errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            }

            isLoading = false;
        }
        catch (Exception ex)
        {
            isLoading = false;
            errorMessage = $"An error occurred: {ex.Message}";
        }
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
