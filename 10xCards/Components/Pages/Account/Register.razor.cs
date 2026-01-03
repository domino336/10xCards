using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;

namespace _10xCards.Components.Pages.Account;

public partial class Register
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private ILogger<Register> Logger { get; set; } = default!;

    private RegisterFormModel model = new();
    private bool isLoading;
    private string? errorMessage;

    private async Task HandleRegister()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            // Send JSON request to API endpoint
            var registerRequest = new
            {
                email = model.Email,
                password = model.Password
            };

            using var httpClient = HttpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(Navigation.BaseUri);
            
            var response = await httpClient.PostAsJsonAsync("/api/auth/register", registerRequest);
            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            if (result?.Success == true)
            {
                // Redirect to login with success message
                Navigation.NavigateTo($"/Account/Login?registered=true", forceLoad: true);
            }
            else
            {
                errorMessage = result?.Errors != null && result.Errors.Any()
                    ? string.Join(" ", result.Errors)
                    : "Registration failed. Please try again.";
                isLoading = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during registration");
            errorMessage = "An error occurred during registration. Please try again.";
            isLoading = false;
        }
    }

    private class RegisterResponse
    {
        public bool Success { get; set; }
        public IEnumerable<string>? Errors { get; set; }
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
