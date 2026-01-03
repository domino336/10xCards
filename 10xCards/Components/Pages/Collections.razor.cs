using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using _10xCards.Application.DTOs.Collections;
using _10xCards.Application.Services;
using _10xCards.Components.Shared;

namespace _10xCards.Components.Pages;

public partial class Collections
{
    [Inject] private ICollectionService CollectionService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private PagedCollectionsResponse? response;
    private int currentPage = 1;
    private int totalPages;
    private bool isLoading;
    private string? errorMessage;
    private string? successMessage;
    private Guid? collectionToDelete;
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
            var result = await CollectionService.GetCollectionsAsync(userId, page, 50, CancellationToken.None);

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
            errorMessage = $"An error occurred while loading collections: {ex.Message}";
            response = null;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void ConfirmDelete(Guid collectionId)
    {
        collectionToDelete = collectionId;
        deleteModal.Show();
    }

    private async Task HandleDelete()
    {
        if (!collectionToDelete.HasValue)
            return;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CollectionService.DeleteCollectionAsync(collectionToDelete.Value, userId, CancellationToken.None);

            if (result.IsSuccess)
            {
                successMessage = "Collection deleted successfully.";
                
                // Refresh current page, or go to previous page if current page is now empty
                if (response!.Collections.Count == 1 && currentPage > 1)
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
            errorMessage = $"Failed to delete collection: {ex.Message}";
        }
        finally
        {
            collectionToDelete = null;
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
