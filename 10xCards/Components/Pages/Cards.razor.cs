using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using _10xCards.Application.DTOs.Cards;
using _10xCards.Application.Services;
using _10xCards.Components.Shared;

namespace _10xCards.Components.Pages;

public partial class Cards
{
    [Inject] private ICardService CardService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private PagedCardsResponse? response;
    private CardFilter filter = CardFilter.All;
    private int currentPage = 1;
    private int totalPages;
    private bool isLoading;
    private string? errorMessage;
    private string? successMessage;
    private Guid? cardToDelete;
    private ConfirmModal deleteModal = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadPage(1);
    }

    private async Task LoadPage(int page)
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CardService.GetCardsAsync(userId, filter, page, 50, CancellationToken.None);

            if (result.IsSuccess)
            {
                response = result.Value;
                currentPage = page;
                totalPages = (int)Math.Ceiling(response.TotalCount / 50.0);
            }
            else
            {
                errorMessage = result.Error;
                response = null;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"An error occurred while loading cards: {ex.Message}";
            response = null;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task SetFilter(CardFilter newFilter)
    {
        if (filter == newFilter)
            return;

        filter = newFilter;
        await LoadPage(1);
    }

    private void ConfirmDelete(Guid cardId)
    {
        cardToDelete = cardId;
        deleteModal.Show();
    }

    private async Task HandleDelete()
    {
        if (!cardToDelete.HasValue)
            return;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CardService.DeleteCardAsync(cardToDelete.Value, userId, CancellationToken.None);

            if (result.IsSuccess)
            {
                successMessage = "Flashcard deleted successfully.";
                
                // Refresh current page, or go to previous page if current page is now empty
                if (response!.Cards.Count == 1 && currentPage > 1)
                {
                    await LoadPage(currentPage - 1);
                }
                else
                {
                    await LoadPage(currentPage);
                }
            }
            else
            {
                errorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to delete card: {ex.Message}";
        }
        finally
        {
            cardToDelete = null;
        }
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
}
