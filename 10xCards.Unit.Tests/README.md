# 10xCards Unit Tests

## Overview

This project contains comprehensive unit tests for the 10xCards application services, following the test plan outlined in `.ai/test-plan.md`.

## Test Summary

**Total Tests: 68**

### Test Coverage

#### SrServiceTests (8 tests)
- `RecordReviewAsync_Again_ResetsRepetitionsAndDecreasesEaseFactor` - Verifies SM-2 algorithm reset on "Again" response
- `RecordReviewAsync_Good_IncreasesRepetitionsAndDecreasesEaseFactorSlightly` - Tests first repetition scheduling
- `RecordReviewAsync_Good_SecondRepetition_SchedulesForSixDays` - Validates second repetition interval
- `RecordReviewAsync_Easy_IncreasesRepetitionsAndIncreasesEaseFactor` - Verifies ease factor increase on "Easy"
- `RecordReviewAsync_CardNotFound_ReturnsFailure` - Tests error handling for non-existent cards
- `RecordReviewAsync_WrongUser_ReturnsFailure` - Validates user authorization
- `RecordReviewAsync_UpdatesLastReviewUtcAndNextReviewUtc` - Confirms timestamp updates
- Tests cover: ease factor calculations, repetition counting, interval scheduling, authorization

#### CardServiceTests (18 tests)
- Card creation with validation (front/back length: 50-500 characters)
- Card updates with validation
- Filtering: All, DueToday, New
- Pagination logic
- Status badge calculation: New, Due, Learned
- Card deletion with authorization
- Due cards retrieval
- Tests cover: CRUD operations, filtering, pagination, status calculations

#### ProposalServiceTests (17 tests)
- Text validation (50-20,000 characters)
- Mock proposal generation from various text structures
- Front/back length enforcement (50-500 characters)
- Content truncation for long sentences
- Proposal acceptance with and without edits
- Proposal rejection
- Authorization validation
- Acceptance events creation
- Tests cover: text validation, mock generation, acceptance/rejection logic, authorization

#### CollectionServiceTests (15 tests)
- Collection creation with validation (name: 1-200 chars, description: max 1000 chars)
- Backup creation on update
- Card addition with duplicate detection
- Card removal
- Previous version restoration
- Collection deletion with authorization
- Pagination
- Tests cover: CRUD operations, backup/restore, duplicate handling, authorization

#### AdminServiceTests (10 tests)
- Metrics calculation with no data
- Card counts (total, AI, manual)
- AI acceptance rate calculation (only users with 5+ cards)
- Percentile calculations (P50, P75, P90)
- Active users counting (7 and 30 days)
- Generation errors counting (last 7 days)
- Cache functionality validation
- Tests cover: metrics calculations, percentiles, active users, caching

## Testing Approach

### Isolation
- Tests use **EF Core InMemory** database for complete isolation
- Each test creates its own database instance
- Mock loggers using **Moq** where needed

### Test Structure
- **Arrange**: Set up test data and dependencies
- **Act**: Execute the method under test
- **Assert**: Verify expected outcomes

### Key Testing Patterns
1. **Validation Testing**: Boundary conditions, invalid inputs
2. **Business Logic Testing**: Algorithm correctness (SM-2, percentiles)
3. **Authorization Testing**: User access control
4. **Edge Cases**: Empty data, single users, duplicates

## Running the Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~SrServiceTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Dependencies

- **xUnit** - Testing framework
- **Moq** - Mocking library
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database for testing
- **Microsoft.Extensions.Caching.Memory** - For AdminService cache tests
- **Microsoft.Extensions.Logging.Abstractions** - For logger mocking

## Test Data Helpers

Each test class includes helper methods for seeding test data:
- `CreateInMemoryContext()` - Creates isolated EF Core context
- `CreateMockLogger()` - Creates mock logger instances
- `SeedCards/SeedCardsForUser()` - Creates test cards with various configurations

## Coverage Goals

These unit tests focus on:
- ? Service layer business logic
- ? Validation rules
- ? Authorization checks
- ? Algorithm correctness (SM-2, percentiles)
- ? Edge cases and error handling

Not covered by unit tests (covered by integration/E2E tests):
- Database constraints and relationships
- Entity Framework query performance
- Full authentication/authorization flow
- Blazor UI interactions

## Next Steps

According to the test plan:
1. ? **Unit Tests** (COMPLETED - 68 tests passing)
2. **Integration Tests** - Test services with real SQLite database
3. **E2E Tests** - Test complete user flows with Playwright

## Contributing

When adding new service methods:
1. Add corresponding unit tests
2. Follow existing test naming conventions: `MethodName_Scenario_ExpectedResult`
3. Ensure tests are isolated and repeatable
4. Test both success and failure cases
5. Include authorization tests where applicable
