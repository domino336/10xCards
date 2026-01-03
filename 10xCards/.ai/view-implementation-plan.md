# Plan implementacji funkcji 10xCards - Blazor Server

## Przegląd architektury

Aplikacja będzie zbudowana według wzorca:
```
Domain Layer (Entities, Enums)
    ↓
Application Layer (Services, DTOs)
    ↓
Presentation Layer (Blazor Components + Code-behind)
```

**Technologie:**
- Blazor Server (.NET 9)
- EF Core + SQLite
- ASP.NET Core Identity
- Bootstrap 5

---

## 1. Warstwa Domain

### Encje (już zdefiniowane w `10xCards.Domain`):
- `Card`, `CardProposal`, `SourceText`, `CardProgress`, `AcceptanceEvent`, `ActiveGeneration`, `GenerationError`
- Enums: `GenerationMethod`, `AcceptanceAction`, `ReviewResult`

### Walidacja domain:
```csharp
// Walidacja w konstruktorach/metodach fabryk
public static Card Create(string front, string back, ...)
{
    if (front.Length < 50 || front.Length > 500)
        throw new DomainException("Front must be 50-500 characters");
    // ...
}
```

---

## 2. Warstwa Application

### 2.1 Serwisy i interfejsy

#### `IProposalService`
```csharp
public interface IProposalService
{
    Task<Result<GenerateProposalsResponse>> GenerateProposalsAsync(
        Guid userId, 
        string sourceText, 
        CancellationToken ct);
    
    Task<Result<IReadOnlyList<ProposalDetailDto>>> GetProposalsBySourceAsync(
        Guid userId, 
        Guid sourceTextId, 
        CancellationToken ct);
    
    Task<Result<int>> AcceptProposalsAsync(
        Guid userId, 
        AcceptProposalsRequest request, 
        CancellationToken ct);
    
    Task<Result<int>> RejectProposalsAsync(
        Guid userId, 
        Guid[] proposalIds, 
        CancellationToken ct);
}
```

**Implementacja kluczowych metod:**

**GenerateProposalsAsync:**
1. Walidacja długości tekstu (50-20k)
2. Sprawdzenie `ActiveGeneration` (jeśli istnieje → błąd)
3. Utworzenie `SourceText` (hash dla deduplikacji)
4. Wywołanie `IAiCompletionService` (OpenRouter)
5. Batch insert `CardProposal` (max 200)
6. Zapis `ActiveGeneration` → usunięcie po zakończeniu
7. Obsługa timeoutów/błędów → zapis `GenerationError`

**AcceptProposalsAsync:**
1. Walidacja ownership (userId match)
2. Transakcja:
   - Utworzenie `Card` z edytowaną treścią
   - Inicjalizacja `CardProgress` (NextReviewUtc = now)
   - Utworzenie `AcceptanceEvent` (Action=Accepted)
   - Usunięcie `CardProposal`
3. Return liczba zaakceptowanych + lista failed IDs

---

#### `ICardService`
```csharp
public interface ICardService
{
    Task<Result<Guid>> CreateManualCardAsync(
        Guid userId, 
        string front, 
        string back, 
        CancellationToken ct);
    
    Task<Result<PagedCardsResponse>> GetCardsAsync(
        Guid userId, 
        CardFilter filter, 
        int page, 
        int pageSize, 
        CancellationToken ct);
    
    Task<Result> UpdateCardAsync(
        Guid cardId, 
        Guid userId, 
        string front, 
        string back, 
        CancellationToken ct);
    
    Task<Result> DeleteCardAsync(Guid cardId, Guid userId, CancellationToken ct);
    
    Task<Result<IReadOnlyList<ReviewCardDto>>> GetDueCardsAsync(
        Guid userId, 
        CancellationToken ct);
}
```

**GetCardsAsync:**
- Query base: `Cards.Where(c => c.UserId == userId)`
- Filtry:
  - `DueToday`: `.Where(c => c.Progress.NextReviewUtc <= DateTime.UtcNow)`
  - `New`: `.Where(c => c.Progress.ReviewCount == 0)`
- Sortowanie: `OrderBy(c => c.Progress.NextReviewUtc).ThenBy(c => c.Id)`
- Projekcja do DTO (truncate Front do 100 chars)
- Paginacja: `.Skip((page-1)*pageSize).Take(pageSize).AsNoTracking()`

---

#### `ISrService` (Spaced Repetition)
```csharp
public interface ISrService
{
    Task<Result> RecordReviewAsync(
        Guid cardId, 
        Guid userId, 
        ReviewResult result, 
        CancellationToken ct);
}
```

**RecordReviewAsync:**
1. Load `CardProgress` (verify userId ownership)
2. Wywołanie algorytmu SR (FSRS lub SM-2 wrapper):
```csharp
var (newInterval, newEase) = _srAlgorithm.Calculate(
    progress.SrState, 
    result);
```
3. Update `CardProgress`:
   - `ReviewCount++`
   - `LastReviewUtc = UtcNow`
   - `LastReviewResult = result`
   - `NextReviewUtc = UtcNow.Add(newInterval)`
   - `SrState = SerializeState(newEase, ...)`
4. SaveChanges

**Biblioteka SR:** 
- Opcja 1: Własna implementacja SM-2
- Opcja 2: NuGet package (np. `FSRS.Net` jeśli dostępny)

---

#### `IAdminService`
```csharp
public interface IAdminService
{
    Task<Result<AdminMetricsDto>> GetDashboardMetricsAsync(CancellationToken ct);
}
```

**GetDashboardMetricsAsync:**
- Agregacje SQL (surowe zapytania lub LINQ):
```csharp
var totalCards = await _context.Cards.CountAsync(ct);
var aiCards = await _context.Cards.CountAsync(c => c.GenerationMethod == GenerationMethod.Ai, ct);

// Współczynnik akceptacji (wykluczenie users <5 fiszek)
var eligibleUsers = await _context.Cards
    .GroupBy(c => c.UserId)
    .Where(g => g.Count() >= 5)
    .Select(g => g.Key)
    .ToListAsync(ct);

var acceptanceRate = await _context.AcceptanceEvents
    .Where(e => eligibleUsers.Contains(e.UserId) && e.Action == AcceptanceAction.Accepted)
    .CountAsync(ct) / (double)await _context.AcceptanceEvents.CountAsync(ct);

// Percentyle (raw SQL lub LINQ quantile approximation)
//...
```
- Cache wyników (`IMemoryCache`, TTL 5 min)

---

### 2.2 DTOs

**Katalog:** `10xCards.Application/DTOs/`

```csharp
// Proposals
public sealed record GenerateProposalsRequest(
    [StringLength(20000, MinimumLength = 50)] string SourceText);

public sealed record ProposalDto(Guid Id, string Front, string Back, DateTime CreatedUtc);

public sealed record GenerateProposalsResponse(
    Guid SourceTextId, 
    IReadOnlyList<ProposalDto> Proposals);

public sealed record AcceptProposalsRequest(
    Guid[] ProposalIds, 
    Dictionary<Guid, EditedProposal>? Edits);

public sealed record EditedProposal(
    [StringLength(500, MinimumLength = 50)] string Front, 
    [StringLength(500, MinimumLength = 50)] string Back);

// Cards
public sealed record CreateCardRequest(
    [StringLength(500, MinimumLength = 50)] string Front, 
    [StringLength(500, MinimumLength = 50)] string Back);

public sealed record CardListItemDto(
    Guid Id,
    string FrontPreview,
    DateTime? NextReviewUtc,
    int ReviewCount,
    string StatusBadge);

public sealed record PagedCardsResponse(
    IReadOnlyList<CardListItemDto> Cards,
    int TotalCount,
    int PageNumber,
    int PageSize);

// Review
public sealed record ReviewCardDto(Guid Id, string Front, string Back);

public sealed record SessionSummaryDto(
    int TotalReviewed, 
    int AgainCount, 
    int GoodCount, 
    int EasyCount);

// Admin
public sealed record AdminMetricsDto(
    int TotalCards,
    int TotalAiCards,
    int TotalManualCards,
    double AiAcceptanceRate,
    PercentileData AcceptancePercentiles,
    int ActiveUsers7d,
    int ActiveUsers30d,
    int GenerationErrorsLast7d);

public sealed record PercentileData(double P50, double P75, double P90);
```

---

### 2.3 Result pattern
```csharp
public sealed record Result<T>(bool IsSuccess, T? Value, string? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

public sealed record Result(bool IsSuccess, string? Error)
{
    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}
```

---

## 3. Warstwa Presentation (Blazor)

### 3.1 Struktura komponentów

```
10xCards/
├── Components/
│   ├── Pages/
│   │   ├── GenerateCards.razor + .razor.cs
│   │   ├── ReviewProposals.razor + .razor.cs
│   │   ├── Cards.razor + .razor.cs
│   │   ├── CreateCard.razor + .razor.cs
│   │   ├── ReviewSession.razor + .razor.cs
│   │   ├── EditCard.razor + .razor.cs
│   │   └── Admin/
│   │       └── Dashboard.razor + .razor.cs
│   ├── Shared/
│   │   ├── ProposalCard.razor (komponent pojedynczej propozycji)
│   │   ├── CardListItem.razor (komponent elementu listy)
│   │   ├── ConfirmModal.razor (reusable modal)
│   │   └── PaginationControl.razor
│   └── Layout/
│       ├── MainLayout.razor (navbar z linkami)
│       └── NavMenu.razor
```

---

### 3.2 Implementacja kluczowych komponentów

#### `GenerateCards.razor`
```razor
@page "/generate"
@attribute [Authorize]
@inject IProposalService ProposalService
@inject NavigationManager Navigation

<h2>Generate Flashcards from Text</h2>

<EditForm Model="@model" OnValidSubmit="HandleGenerate">
    <DataAnnotationsValidator />
    <ValidationSummary class="alert alert-danger" />
    
    <div class="mb-3">
        <label for="sourceText" class="form-label">Paste your notes (50-20,000 chars)</label>
        <textarea @bind="model.SourceText" 
                  class="form-control" 
                  rows="10" 
                  id="sourceText"
                  disabled="@isGenerating"></textarea>
    </div>
    
    <button type="submit" class="btn btn-primary" disabled="@isGenerating">
        @if (isGenerating)
        {
            <span class="spinner-border spinner-border-sm me-2"></span>
            <span>Generating...</span>
        }
        else
        {
            <span>Generate Flashcards</span>
        }
    </button>
</EditForm>

@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="alert alert-danger mt-3" role="alert">
        @errorMessage
        <button class="btn btn-sm btn-outline-danger ms-2" @onclick="HandleGenerate">Retry</button>
    </div>
}
```

**Code-behind (`GenerateCards.razor.cs`):**
```csharp
public partial class GenerateCards
{
    private GenerateProposalsRequest model = new("");
    private bool isGenerating;
    private string? errorMessage;
    
    private async Task HandleGenerate()
    {
        isGenerating = true;
        errorMessage = null;
        
        var userId = GetCurrentUserId(); // z AuthenticationStateProvider
        var result = await ProposalService.GenerateProposalsAsync(
            userId, 
            model.SourceText, 
            CancellationToken.None);
        
        isGenerating = false;
        
        if (result.IsSuccess)
        {
            Navigation.NavigateTo($"/proposals/{result.Value.SourceTextId}");
        }
        else
        {
            errorMessage = result.Error;
        }
    }
    
    private Guid GetCurrentUserId()
    {
        // Claims-based retrieval
        var claim = AuthenticationState.User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim!.Value);
    }
}
```

---

#### `ReviewProposals.razor`
```razor
@page "/proposals/{SourceTextId:guid}"
@attribute [Authorize]
@inject IProposalService ProposalService

<h2>Review Generated Proposals</h2>

@if (proposals == null)
{
    <p>Loading...</p>
}
else
{
    <div class="mb-3">
        <button class="btn btn-success me-2" 
                @onclick="AcceptSelected" 
                disabled="@(!selectedIds.Any())">
            Accept Selected (@selectedIds.Count)
        </button>
        <button class="btn btn-outline-danger" 
                @onclick="RejectSelected" 
                disabled="@(!selectedIds.Any())">
            Reject Selected
        </button>
    </div>
    
    @foreach (var proposal in proposals)
    {
        <ProposalCard Proposal="@proposal" 
                      IsSelected="@selectedIds.Contains(proposal.Id)"
                      OnToggleSelect="@(() => ToggleSelect(proposal.Id))"
                      OnEdit="@((front, back) => HandleEdit(proposal.Id, front, back))"
                      OnAccept="@(() => AcceptSingle(proposal.Id))"
                      OnReject="@(() => RejectSingle(proposal.Id))" />
    }
}
```

**Code-behind:**

```csharp
public partial class ReviewProposals
{
    [Parameter] public Guid SourceTextId { get; set; }
    
    private List<ProposalDetailDto>? proposals;
    private HashSet<Guid> selectedIds = new();
    private Dictionary<Guid, EditedProposal> edits = new();
    
    protected override async Task OnInitializedAsync()
    {
        var userId = GetCurrentUserId();
        var result = await ProposalService.GetProposalsBySourceAsync(userId, SourceTextId, default);
        
        if (result.IsSuccess)
            proposals = result.Value.ToList();
    }
    
    private void ToggleSelect(Guid id)
    {
        if (!selectedIds.Add(id))
            selectedIds.Remove(id);
    }
    
    private void HandleEdit(Guid id, string front, string back)
    {
        edits[id] = new EditedProposal(front, back);
    }
    
    private async Task AcceptSelected()
    {
        var userId = GetCurrentUserId();
        var request = new AcceptProposalsRequest(selectedIds.ToArray(), edits);
        
        var result = await ProposalService.AcceptProposalsAsync(userId, request, default);
        
        if (result.IsSuccess)
        {
            // Refresh list, show success toast
            await OnInitializedAsync();
            selectedIds.Clear();
        }
    }
    
    // Similar: RejectSelected, AcceptSingle, RejectSingle
}
```

---

#### `ProposalCard.razor` (Shared component)
```razor
<div class="card mb-3 @(IsSelected ? "border-primary" : "")">
    <div class="card-body">
        <div class="form-check mb-2">
            <input class="form-check-input" 
                   type="checkbox" 
                   checked="@IsSelected" 
                   @onchange="@(() => OnToggleSelect.InvokeAsync())">
        </div>
        
        <EditForm Model="@editModel">
            <div class="mb-2">
                <label class="form-label fw-bold">Front</label>
                <InputTextArea @bind-Value="editModel.Front" 
                               class="form-control" 
                               rows="2" 
                               @oninput="@(() => hasChanges = true)" />
                <ValidationMessage For="@(() => editModel.Front)" />
            </div>
            
            <div class="mb-2">
                <label class="form-label fw-bold">Back</label>
                <InputTextArea @bind-Value="editModel.Back" 
                               class="form-control" 
                               rows="3" 
                               @oninput="@(() => hasChanges = true)" />
                <ValidationMessage For="@(() => editModel.Back)" />
            </div>
        </EditForm>
        
        <div class="d-flex gap-2">
            @if (hasChanges)
            {
                <button class="btn btn-sm btn-outline-primary" 
                        @onclick="SaveEdit">
                    Save Edit
                </button>
            }
            <button class="btn btn-sm btn-success" @onclick="@(() => OnAccept.InvokeAsync())">
                Accept
            </button>
            <button class="btn btn-sm btn-outline-danger" @onclick="@(() => OnReject.InvokeAsync())">
                Reject
            </button>
        </div>
    </div>
</div>

@code {
    [Parameter] public ProposalDetailDto Proposal { get; set; } = default!;
    [Parameter] public bool IsSelected { get; set; }
    [Parameter] public EventCallback OnToggleSelect { get; set; }
    [Parameter] public EventCallback<(string Front, string Back)> OnEdit { get; set; }
    [Parameter] public EventCallback OnAccept { get; set; }
    [Parameter] public EventCallback OnReject { get; set; }
    
    private EditedProposal editModel = default!;
    private bool hasChanges;
    
    protected override void OnParametersSet()
    {
        editModel = new(Proposal.Front, Proposal.Back);
    }
    
    private void SaveEdit()
    {
        OnEdit.InvokeAsync((editModel.Front, editModel.Back));
        hasChanges = false;
    }
}
```

---

#### `Cards.razor`
```razor
@page "/cards"
@attribute [Authorize]
@inject ICardService CardService

<h2>My Flashcards</h2>

<div class="mb-3">
    <div class="btn-group" role="group">
        <button class="btn @(filter == CardFilter.All ? "btn-primary" : "btn-outline-primary")" 
                @onclick="@(() => SetFilter(CardFilter.All))">
            All
        </button>
        <button class="btn @(filter == CardFilter.DueToday ? "btn-primary" : "btn-outline-primary")" 
                @onclick="@(() => SetFilter(CardFilter.DueToday))">
            Due Today
        </button>
        <button class="btn @(filter == CardFilter.New ? "btn-primary" : "btn-outline-primary")" 
                @onclick="@(() => SetFilter(CardFilter.New))">
            New
        </button>
    </div>
</div>

@if (response == null)
{
    <p>Loading...</p>
}
else
{
    @foreach (var card in response.Cards)
    {
        <CardListItem Card="@card" OnDelete="@(() => ConfirmDelete(card.Id))" />
    }
    
    <PaginationControl TotalPages="@totalPages" 
                       CurrentPage="@currentPage" 
                       OnPageChange="@LoadPage" />
}

<ConfirmModal @ref="deleteModal" 
              Title="Delete Card" 
              Message="Are you sure? This action cannot be undone." 
              OnConfirm="@HandleDelete" />
```

**Code-behind:**

```csharp
public partial class Cards
{
    private PagedCardsResponse? response;
    private CardFilter filter = CardFilter.All;
    private int currentPage = 1;
    private int totalPages;
    private Guid? cardToDelete;
    private ConfirmModal deleteModal = default!;
    
    protected override async Task OnInitializedAsync() => await LoadPage(1);
    
    private async Task LoadPage(int page)
    {
        currentPage = page;
        var userId = GetCurrentUserId();
        var result = await CardService.GetCardsAsync(userId, filter, page, 50, default);
        
        if (result.IsSuccess)
        {
            response = result.Value;
            totalPages = (int)Math.Ceiling(response.TotalCount / 50.0);
        }
    }
    
    private async Task SetFilter(CardFilter newFilter)
    {
        filter = newFilter;
        await LoadPage(1);
    }
    
    private void ConfirmDelete(Guid id)
    {
        cardToDelete = id;
        deleteModal.Show();
    }
    
    private async Task HandleDelete()
    {
        if (cardToDelete.HasValue)
        {
            var userId = GetCurrentUserId();
            await CardService.DeleteCardAsync(cardToDelete.Value, userId, default);
            await LoadPage(currentPage);
        }
    }
}
```

---

#### `ReviewSession.razor`
```razor
@page "/review"
@attribute [Authorize]
@inject ICardService CardService
@inject ISrService SrService

@if (sessionState == SessionState.Loading)
{
    <p>Loading cards...</p>
}
else if (sessionState == SessionState.NoDueCards)
{
    <div class="alert alert-info">
        <h4>No cards due for review!</h4>
        <p>Come back later or <a href="/cards">browse your cards</a>.</p>
    </div>
}
else if (sessionState == SessionState.ShowFront)
{
    <div class="card text-center">
        <div class="card-body">
            <h3 class="card-title">Question</h3>
            <p class="display-6">@currentCard!.Front</p>
            <button class="btn btn-primary btn-lg mt-4" @onclick="ShowAnswer">
                Show Answer
            </button>
        </div>
    </div>
}
else if (sessionState == SessionState.ShowBack)
{
    <div class="card text-center">
        <div class="card-body">
            <h5 class="text-muted">Question</h5>
            <p>@currentCard!.Front</p>
            <hr>
            <h3 class="card-title">Answer</h3>
            <p class="display-6">@currentCard.Back</p>
            
            <div class="d-flex justify-content-center gap-3 mt-4">
                <button class="btn btn-danger btn-lg" @onclick="@(() => Rate(ReviewResult.Again))">
                    Again
                </button>
                <button class="btn btn-warning btn-lg" @onclick="@(() => Rate(ReviewResult.Good))">
                    Good
                </button>
                <button class="btn btn-success btn-lg" @onclick="@(() => Rate(ReviewResult.Easy))">
                    Easy
                </button>
            </div>
        </div>
    </div>
}
else if (sessionState == SessionState.Finished)
{
    <div class="alert alert-success">
        <h4>Session Complete!</h4>
        <p>Reviewed: @summary!.TotalReviewed cards</p>
        <ul>
            <li>Again: @summary.AgainCount</li>
            <li>Good: @summary.GoodCount</li>
            <li>Easy: @summary.EasyCount</li>
        </ul>
        <a href="/cards" class="btn btn-primary">Back to Cards</a>
    </div>
}
```

**Code-behind:**

```csharp
public partial class ReviewSession
{
    private Queue<ReviewCardDto> cards = new();
    private ReviewCardDto? currentCard;
    private SessionState sessionState = SessionState.Loading;
    private SessionSummaryDto? summary;
    private int againCount, goodCount, easyCount;
    
    protected override async Task OnInitializedAsync()
    {
        var userId = GetCurrentUserId();
        var result = await CardService.GetDueCardsAsync(userId, default);
        
        if (!result.IsSuccess || !result.Value.Any())
        {
            sessionState = SessionState.NoDueCards;
            return;
        }
        
        cards = new Queue<ReviewCardDto>(result.Value);
        LoadNextCard();
    }
    
    private void LoadNextCard()
    {
        if (!cards.Any())
        {
            FinishSession();
            return;
        }
        
        currentCard = cards.Dequeue();
        sessionState = SessionState.ShowFront;
    }
    
    private void ShowAnswer()
    {
        sessionState = SessionState.ShowBack;
    }
    
    private async Task Rate(ReviewResult result)
    {
        var userId = GetCurrentUserId();
        await SrService.RecordReviewAsync(currentCard!.Id, userId, result, default);
        
        switch (result)
        {
            case ReviewResult.Again: againCount++; break;
            case ReviewResult.Good: goodCount++; break;
            case ReviewResult.Easy: easyCount++; break;
        }
        
        LoadNextCard();
    }
    
    private void FinishSession()
    {
        summary = new(againCount + goodCount + easyCount, againCount, goodCount, easyCount);
        sessionState = SessionState.Finished;
    }
    
    private enum SessionState
    {
        Loading, NoDueCards, ShowFront, ShowBack, Finished
    }
}
```

---

#### `Admin/Dashboard.razor`
```razor
@page "/admin/dashboard"
@attribute [Authorize(Roles = "Admin")]
@inject IAdminService AdminService

<h2>Admin Dashboard</h2>

<button class="btn btn-secondary mb-3" @onclick="Refresh">
    <i class="bi bi-arrow-clockwise"></i> Refresh
</button>

@if (metrics == null)
{
    <p>Loading...</p>
}
else
{
    <div class="row">
        <div class="col-md-3">
            <div class="card">
                <div class="card-body">
                    <h5>Total Cards</h5>
                    <p class="display-4">@metrics.TotalCards</p>
                </div>
            </div>
        </div>
        
        <div class="col-md-3">
            <div class="card">
                <div class="card-body">
                    <h5>AI Acceptance Rate</h5>
                    <p class="display-4">@metrics.AiAcceptanceRate.ToString("P0")</p>
                </div>
            </div>
        </div>
        
        <div class="col-md-3">
            <div class="card">
                <div class="card-body">
                    <h5>Active Users (7d)</h5>
                    <p class="display-4">@metrics.ActiveUsers7d</p>
                </div>
            </div>
        </div>
        
        <div class="col-md-3">
            <div class="card">
                <div class="card-body">
                    <h5>Generation Errors</h5>
                    <p class="display-4 text-danger">@metrics.GenerationErrorsLast7d</p>
                </div>
            </div>
        </div>
    </div>
    
    <div class="mt-4">
        <h4>Acceptance Percentiles</h4>
        <ul>
            <li>P50: @metrics.AcceptancePercentiles.P50.ToString("P0")</li>
            <li>P75: @metrics.AcceptancePercentiles.P75.ToString("P0")</li>
            <li>P90: @metrics.AcceptancePercentiles.P90.ToString("P0")</li>
        </ul>
    </div>
}
```

**Code-behind:**

```csharp
public partial class Dashboard
{
    private AdminMetricsDto? metrics;
    
    protected override async Task OnInitializedAsync() => await Refresh();
    
    private async Task Refresh()
    {
        var result = await AdminService.GetDashboardMetricsAsync(default);
        if (result.IsSuccess)
            metrics = result.Value;
    }
}
```

---

## 4. Konfiguracja i DI

### `Program.cs`
```csharp
var builder = WebApplication.CreateBuilder(args);

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// EF Core + SQLite
builder.Services.AddDbContext<CardsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<CardsDbContext>()
    .AddDefaultTokenProviders();

// Application Services
builder.Services.AddScoped<IProposalService, ProposalService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<ISrService, SrService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddSingleton<IAiCompletionService, OpenRouterService>();

// Caching
builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

---

## 5. Migracje i inicjalizacja bazy

### Skrypt migracji:
```sh
dotnet ef migrations add InitialCreate --project 10xCards.Persistance --startup-project 10xCards
dotnet ef database update --project 10xCards.Persistance --startup-project 10xCards
```

### Seed admina:
```csharp
// W Program.cs (po app.Build())
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    
    var admin = await userManager.FindByEmailAsync("admin@10xcards.local");
    if (admin == null)
    {
        admin = new IdentityUser { UserName = "admin@10xcards.local", Email = "admin@10xcards.local" };
        await userManager.CreateAsync(admin, "Admin123!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}
```

---

## 6. Testowanie

### Unit Tests (`10xCards.Unit.Tests`)
- Domain validation logic
- SR algorithm calculations
- Service business logic (mocked DbContext)

### Integration Tests (`10xCards.Integration.Tests`)
- WebApplicationFactory<Program>
- In-memory SQLite database
- End-to-end flows (generate → accept → review)

---

## 7. Checklist implementacji

**Faza 1: Fundament (Week 1-2)**
- [ ] Domain entities + EF configurations
- [ ] Migracje bazy danych
- [ ] Identity setup (rejestracja/logowanie)
- [ ] Bazowe komponenty (layout, navbar)

**Faza 2: Generacja i selekcja (Week 3-4)**
- [ ] `IProposalService` + `ProposalService`
- [ ] `IAiCompletionService` stub/mock
- [ ] `GenerateCards.razor` + `ReviewProposals.razor`
- [ ] `ProposalCard` shared component
- [ ] Accept/Reject bulk actions

**Faza 3: Biblioteka fiszek (Week 5-6)**
- [ ] `ICardService` + `CardService`
- [ ] `Cards.razor` (lista + filtry + paginacja)
- [ ] `CreateCard.razor` (manualne tworzenie)
- [ ] `EditCard.razor` + delete flow

**Faza 4: Spaced Repetition (Week 7-8)**
- [ ] `ISrService` + SR algorithm wrapper
- [ ] `ReviewSession.razor` (state machine)
- [ ] Session persistence (localStorage backup)
- [ ] Progress tracking

**Faza 5: Kolekcje fiszek (US-051) ✅ COMPLETED**
- [x] Domain entities (`CardCollection`, `CardCollectionCard`, `CardCollectionBackup`)
- [x] EF Core configurations
- [x] `ICollectionService` + `CollectionService`
- [x] DTOs (CollectionDto, CollectionDetailDto, PagedCollectionsResponse)
- [ ] Migracja bazy danych: `dotnet ef migrations add AddCardCollections --project 10xCards.Persistance --startup-project 10xCards`
- [x] `Collections.razor` (lista kolekcji)
- [x] `CreateCollection.razor` (tworzenie z wyborem fiszek)
- [x] `CollectionDetail.razor` (szczegóły + zarządzanie fiskami)
- [x] `EditCollection.razor` (edycja + restore backup + add/remove cards)
- [x] Nawigacja: dodano link "Collections" w menu
- [ ] Testing: utworzenie, edycja, restore, delete, autoryzacja

**Faza 6: Admin i finalizacja (Week 11-12)**
- [ ] `IAdminService` + agregacje SQL
- [ ] `Admin/Dashboard.razor`
- [ ] Caching metryk
- [ ] XSS sanitization
- [ ] Performance optimization (indeksy, AsNoTracking)
- [ ] Integration tests
- [ ] Deployment prep

---

## 8. Uwagi końcowe

- **Responsywność:** Bootstrap grid + utilities zapewniają podstawową responsywność (testować na 360px)
- **Accessibility:** Dodać `aria-label` do kluczowych przycisków, focus management w sesji
- **Error handling:** Globally unhandled exceptions → error boundary component
- **Logging:** `ILogger<T>` w serwisach (persist do pliku via Serilog opcjonalnie)
