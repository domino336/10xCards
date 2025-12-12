using _10xCards.Application.DTOs.Proposals;
using _10xCards.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace _10xCards.Components.Pages;

[Authorize]
public partial class ReviewProposals
{
    [Parameter]
    public Guid SourceTextId { get; set; }

    private List<ProposalDetailDto>? proposals;
    private HashSet<Guid> selectedIds = new();
    private Dictionary<Guid, EditedProposal> edits = new();
    private string? successMessage;
    private string? errorMessage;
    private bool isProcessing;
    private string? lastAction;

    protected override async Task OnInitializedAsync()
    {
        await LoadProposals();
    }

    private async Task LoadProposals()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await ProposalService.GetProposalsBySourceAsync(userId, SourceTextId, default);

            if (result.IsSuccess && result.Value != null)
            {
                proposals = result.Value.ToList();
            }
            else
            {
                errorMessage = result.Error ?? "Failed to load proposals.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"An unexpected error occurred: {ex.Message}";
        }
    }

    private void ToggleSelect(Guid id)
    {
        if (!selectedIds.Add(id))
        {
            selectedIds.Remove(id);
        }
    }

    private void HandleEdit(Guid id, string front, string back)
    {
        edits[id] = new EditedProposal(front, back);
    }

    private async Task AcceptSelected()
    {
        if (!selectedIds.Any() || isProcessing)
        {
            return;
        }

        isProcessing = true;
        lastAction = "accept";
        successMessage = null;
        errorMessage = null;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var request = new AcceptProposalsRequest(selectedIds.ToArray(), edits);

            var result = await ProposalService.AcceptProposalsAsync(userId, request, default);

            if (result.IsSuccess)
            {
                var count = result.Value;
                successMessage = count == 1 
                    ? "? 1 flashcard accepted and added to your library!" 
                    : $"? {count} flashcards accepted and added to your library!";
                
                // Remove accepted proposals from the list
                proposals = proposals?.Where(p => !selectedIds.Contains(p.Id)).ToList();
                
                selectedIds.Clear();
                edits.Clear();

                // If no more proposals, redirect to cards after 2 seconds
                if (proposals == null || !proposals.Any())
                {
                    await Task.Delay(2000);
                    Navigation.NavigateTo("/cards");
                }
            }
            else
            {
                errorMessage = result.Error ?? "Failed to accept proposals.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            lastAction = null;
        }
    }

    private async Task RejectSelected()
    {
        if (!selectedIds.Any() || isProcessing)
        {
            return;
        }

        isProcessing = true;
        lastAction = "reject";
        successMessage = null;
        errorMessage = null;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await ProposalService.RejectProposalsAsync(userId, selectedIds.ToArray(), default);

            if (result.IsSuccess)
            {
                var count = result.Value;
                successMessage = count == 1 
                    ? "??? 1 proposal rejected and removed." 
                    : $"??? {count} proposals rejected and removed.";
                
                // Remove rejected proposals from the list
                proposals = proposals?.Where(p => !selectedIds.Contains(p.Id)).ToList();
                
                selectedIds.Clear();
                edits.Clear();

                // If no more proposals, redirect to generate after 2 seconds
                if (proposals == null || !proposals.Any())
                {
                    await Task.Delay(2000);
                    Navigation.NavigateTo("/generate");
                }
            }
            else
            {
                errorMessage = result.Error ?? "Failed to reject proposals.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            lastAction = null;
        }
    }

    private async Task AcceptSingle(Guid id)
    {
        if (isProcessing)
            return;

        selectedIds.Clear();
        selectedIds.Add(id);
        await AcceptSelected();
    }

    private async Task RejectSingle(Guid id)
    {
        if (isProcessing)
            return;

        selectedIds.Clear();
        selectedIds.Add(id);
        await RejectSelected();
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
}
