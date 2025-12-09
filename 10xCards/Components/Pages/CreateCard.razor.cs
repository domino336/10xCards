using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _10xCards.Application.DTOs.Cards;
using _10xCards.Application.Services;

namespace _10xCards.Components.Pages;

public partial class CreateCard
{
    [Inject] private ICardService CardService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private CardEditModel model = new();
    private bool isSubmitting;
    private string? errorMessage;
    private int frontCharCount;
    private int backCharCount;

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CardService.CreateManualCardAsync(
                userId,
                model.Front,
                model.Back,
                CancellationToken.None);

            if (result.IsSuccess)
            {
                Navigation.NavigateTo("/cards");
            }
            else
            {
                errorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"An error occurred while creating the card: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }

    private void UpdateFrontCharCount(ChangeEventArgs e)
    {
        frontCharCount = e.Value?.ToString()?.Length ?? 0;
    }

    private void UpdateBackCharCount(ChangeEventArgs e)
    {
        backCharCount = e.Value?.ToString()?.Length ?? 0;
    }

    private static string GetCharCountClass(int count, int min, int max)
    {
        if (count < min)
            return "text-danger";
        if (count > max)
            return "text-danger fw-bold";
        if (count >= min && count <= max - 50)
            return "text-success";
        
        return "text-warning";
    }

    private async Task<Guid> GetCurrentUserIdAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (!user.Identity?.IsAuthenticated ?? true)
            throw new InvalidOperationException("User is not authenticated");

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found");

        return Guid.Parse(userIdClaim.Value);
    }

    private sealed class CardEditModel
    {
        [Required]
        [StringLength(500, MinimumLength = 50, ErrorMessage = "Front must be between 50 and 500 characters")]
        public string Front { get; set; } = string.Empty;

        [Required]
        [StringLength(500, MinimumLength = 50, ErrorMessage = "Back must be between 50 and 500 characters")]
        public string Back { get; set; } = string.Empty;
    }
}
