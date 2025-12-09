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
        if (!selectedIds.Any())
        {
            return;
        }

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var request = new AcceptProposalsRequest(selectedIds.ToArray(), edits);

            var result = await ProposalService.AcceptProposalsAsync(userId, request, default);

            if (result.IsSuccess)
            {
                successMessage = $"Successfully accepted {result.Value} proposal(s).";
                await LoadProposals();
                selectedIds.Clear();
                edits.Clear();
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
    }

    private async Task RejectSelected()
    {
        if (!selectedIds.Any())
        {
            return;
        }

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await ProposalService.RejectProposalsAsync(userId, selectedIds.ToArray(), default);

            if (result.IsSuccess)
            {
                successMessage = $"Successfully rejected {result.Value} proposal(s).";
                await LoadProposals();
                selectedIds.Clear();
                edits.Clear();
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
    }

    private async Task AcceptSingle(Guid id)
    {
        selectedIds.Clear();
        selectedIds.Add(id);
        await AcceptSelected();
    }

    private async Task RejectSingle(Guid id)
    {
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
