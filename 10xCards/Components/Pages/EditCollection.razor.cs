using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using _10xCards.Application.DTOs.Cards;
using _10xCards.Application.DTOs.Collections;
using _10xCards.Application.Services;

namespace _10xCards.Components.Pages;

public partial class EditCollection
{
    [Parameter] public Guid CollectionId { get; set; }

    [Inject] private ICollectionService CollectionService { get; set; } = default!;
    [Inject] private ICardService CardService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private CollectionDetailDto? collection;
    private EditCollectionModel model = new();
    private bool isLoading = true;
    private bool isSubmitting;
    private string? errorMessage;
    private string? successMessage;
    private HashSet<Guid> selectedCardIds = new();
    
    // For add cards modal
    private bool showAddCardsModal;
    private List<CardListItemDto>? availableCardsToAdd;
    private HashSet<Guid> cardsToAdd = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadCollection();
    }

    private async Task LoadCollection()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CollectionService.GetCollectionAsync(CollectionId, userId, CancellationToken.None);

            if (result.IsSuccess)
            {
                collection = result.Value;
                model.Name = collection.Name;
                model.Description = collection.Description;
            }
            else
            {
                errorMessage = result.Error;
                collection = null;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load collection: {ex.Message}";
            collection = null;
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleUpdate()
    {
        isSubmitting = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CollectionService.UpdateCollectionAsync(
                CollectionId,
                userId,
                model.Name.Trim(),
                model.Description?.Trim() ?? string.Empty,
                CancellationToken.None
            );

            if (result.IsSuccess)
            {
                successMessage = "Collection updated successfully.";
                await LoadCollection();
            }
            else
            {
                errorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to update collection: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private void ToggleCardSelection(Guid cardId)
    {
        if (!selectedCardIds.Add(cardId))
        {
            selectedCardIds.Remove(cardId);
        }
    }

    private void SelectAllCards()
    {
        if (collection != null)
        {
            selectedCardIds = collection.Cards.Select(c => c.CardId).ToHashSet();
        }
    }

    private void DeselectAllCards()
    {
        selectedCardIds.Clear();
    }

    private async Task RemoveSelectedCards()
    {
        if (!selectedCardIds.Any())
            return;

        isSubmitting = true;
        errorMessage = null;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CollectionService.RemoveCardsFromCollectionAsync(
                CollectionId,
                userId,
                selectedCardIds.ToList(),
                CancellationToken.None
            );

            if (result.IsSuccess)
            {
                successMessage = $"Removed {selectedCardIds.Count} card(s) from collection.";
                selectedCardIds.Clear();
                await LoadCollection();
            }
            else
            {
                errorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to remove cards: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private async Task ShowAddCardsModal()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CardService.GetCardsAsync(userId, CardFilter.All, 1, 1000, CancellationToken.None);

            if (result.IsSuccess && collection != null)
            {
                var existingCardIds = collection.Cards.Select(c => c.CardId).ToHashSet();
                availableCardsToAdd = result.Value.Cards
                    .Where(c => !existingCardIds.Contains(c.Id))
                    .ToList();
                
                showAddCardsModal = true;
                cardsToAdd.Clear();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load cards: {ex.Message}";
        }
    }

    private void HideAddCardsModal()
    {
        showAddCardsModal = false;
        cardsToAdd.Clear();
    }

    private void ToggleAddCard(Guid cardId)
    {
        if (!cardsToAdd.Add(cardId))
        {
            cardsToAdd.Remove(cardId);
        }
    }

    private void SelectAllAddCards()
    {
        if (availableCardsToAdd != null)
        {
            cardsToAdd = availableCardsToAdd.Select(c => c.Id).ToHashSet();
        }
    }

    private void DeselectAllAddCards()
    {
        cardsToAdd.Clear();
    }

    private async Task HandleAddCards()
    {
        if (!cardsToAdd.Any())
            return;

        isSubmitting = true;
        errorMessage = null;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CollectionService.AddCardsToCollectionAsync(
                CollectionId,
                userId,
                cardsToAdd.ToList(),
                CancellationToken.None
            );

            if (result.IsSuccess)
            {
                successMessage = $"Added {cardsToAdd.Count} card(s) to collection.";
                HideAddCardsModal();
                await LoadCollection();
            }
            else
            {
                errorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to add cards: {ex.Message}";
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

    private class EditCollectionModel
    {
        [Required(ErrorMessage = "Collection name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
    }
}
