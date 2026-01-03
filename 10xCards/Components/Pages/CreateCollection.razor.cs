using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using _10xCards.Application.DTOs.Cards;
using _10xCards.Application.Services;

namespace _10xCards.Components.Pages;

public partial class CreateCollection
{
    [Inject] private ICollectionService CollectionService { get; set; } = default!;
    [Inject] private ICardService CardService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private CreateCollectionModel model = new();
    private List<CardListItemDto>? availableCards;
    private HashSet<Guid> selectedCardIds = new();
    private bool isLoadingCards = true;
    private bool isSubmitting;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadAvailableCards();
    }

    private async Task LoadAvailableCards()
    {
        isLoadingCards = true;
        errorMessage = null;
        
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CardService.GetCardsAsync(userId, CardFilter.All, 1, 1000, CancellationToken.None);

            if (result.IsSuccess)
            {
                availableCards = result.Value.Cards.ToList();
            }
            else
            {
                errorMessage = $"Failed to load cards: {result.Error}";
                availableCards = new List<CardListItemDto>();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load cards: {ex.Message}";
            availableCards = new List<CardListItemDto>();
        }
        finally
        {
            isLoadingCards = false;
        }
    }

    private void ToggleCard(Guid cardId)
    {
        if (!selectedCardIds.Add(cardId))
        {
            selectedCardIds.Remove(cardId);
        }
        StateHasChanged();
    }

    private void SelectAll()
    {
        if (availableCards != null)
        {
            selectedCardIds = availableCards.Select(c => c.Id).ToHashSet();
            StateHasChanged();
        }
    }

    private void DeselectAll()
    {
        selectedCardIds.Clear();
        StateHasChanged();
    }

    private async Task HandleSubmit()
    {
        if (selectedCardIds.Count == 0)
        {
            errorMessage = "Please select at least one flashcard.";
            return;
        }

        isSubmitting = true;
        errorMessage = null;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CollectionService.CreateCollectionAsync(
                userId,
                model.Name.Trim(),
                model.Description?.Trim() ?? string.Empty,
                selectedCardIds.ToList(),
                CancellationToken.None
            );

            if (result.IsSuccess)
            {
                Navigation.NavigateTo($"/collections/{result.Value}");
            }
            else
            {
                errorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to create collection: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength) + "...";
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

    private class CreateCollectionModel
    {
        [Required(ErrorMessage = "Collection name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
    }
}
