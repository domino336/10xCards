using _10xCards.Application.DTOs.Proposals;
using _10xCards.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace _10xCards.Components.Pages;

[Authorize]
public partial class GenerateCards
{
    private GenerateProposalsFormModel model = new();
    private bool isGenerating;
    private string? errorMessage;

    private async Task HandleGenerate()
    {
        isGenerating = true;
        errorMessage = null;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await ProposalService.GenerateProposalsAsync(
                userId,
                model.SourceText,
                CancellationToken.None);

            isGenerating = false;

            if (result.IsSuccess && result.Value != null)
            {
                Navigation.NavigateTo($"/proposals/{result.Value.SourceTextId}");
            }
            else
            {
                errorMessage = result.Error ?? "An error occurred while generating proposals.";
            }
        }
        catch (Exception ex)
        {
            isGenerating = false;
            errorMessage = $"An unexpected error occurred: {ex.Message}";
        }
    }

    private async Task<Guid> GetCurrentUserIdAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);

        if (claim == null)
        {
            throw new InvalidOperationException("User ID claim not found.");
        }

        return Guid.Parse(claim.Value);
    }

    public class GenerateProposalsFormModel
    {
        [Required]
        [StringLength(20000, MinimumLength = 50, ErrorMessage = "Source text must be between 50 and 20,000 characters")]
        public string SourceText { get; set; } = string.Empty;
    }
}

