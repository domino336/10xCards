using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using _10xCards.Application.DTOs.Collections;
using _10xCards.Application.Services;
using _10xCards.Components.Shared;

namespace _10xCards.Components.Pages;

public partial class CollectionDetail
{
    [Parameter] public Guid CollectionId { get; set; }

    [Inject] private ICollectionService CollectionService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private CollectionDetailDto? collection;
    private bool isLoading = true;
    private string? errorMessage;
    private string? successMessage;
    private ConfirmModal deleteModal = default!;
    private ConfirmModal restoreModal = default!;

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

    private void ConfirmDelete()
    {
        deleteModal.Show();
    }

    private async Task HandleDelete()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CollectionService.DeleteCollectionAsync(CollectionId, userId, CancellationToken.None);

            if (result.IsSuccess)
            {
                Navigation.NavigateTo("/collections");
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
    }

    private void ConfirmRestore()
    {
        restoreModal.Show();
    }

    private async Task HandleRestore()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await CollectionService.RestorePreviousVersionAsync(CollectionId, userId, CancellationToken.None);

            if (result.IsSuccess)
            {
                successMessage = "Collection restored to previous version successfully.";
                await LoadCollection();
            }
            else
            {
                errorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to restore collection: {ex.Message}";
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
