# Mock AI Proposal Generation

## Overview

Temporary mock implementation of AI flashcard generation for testing UI without OpenRouter API.

## Implementation

### How It Works

1. **User submits text** via `/generate`
2. **Text is saved** to `SourceTexts` table
3. **Mock proposals are generated:**
   - Text is split into sentences
   - Sentences are paired (2 sentences ? 1 flashcard)
   - Question generated from first sentence
   - Answer is second sentence
   - Max 5 proposals per submission

### Algorithm

```
Input: Source text (50-20,000 characters)

Step 1: Split into sentences (by '.', '!', '?')
Step 2: Filter sentences > 30 characters
Step 3: Take up to 10 sentences
Step 4: Create proposals (max 5)
  - Pair sentences: (0,1), (2,3), (4,5), (6,7), (8,9)
  - Generate question from first sentence
  - Use second sentence as answer
Step 5: Ensure min 50 characters for Front/Back
Step 6: Save to CardProposals table

Output: ProposalDto[] with Ids
```

### Question Generation Templates

| Pattern in Sentence | Question Template |
|---------------------|-------------------|
| Contains "is/are" | "What {subject}?" |
| Contains "can/could" | "Explain: {sentence}" |
| Contains "because/since" | "Why {before-because}?" |
| Default | "What does the text say about '{first 5 words}'?" |

### Example

**Input text:**
```
Blazor is a modern web framework. It allows developers to build 
interactive web applications using C#. There are two main hosting 
models: Blazor Server and Blazor WebAssembly.
```

**Generated proposals:**

1. **Front:** "What Blazor?"
   **Back:** "It allows developers to build interactive web applications using C#"

2. **Front:** "What does the text say about 'There are two main'?"
   **Back:** "Blazor Server and Blazor WebAssembly"

## Limitations

?? **This is a MOCK implementation:**
- ? Not intelligent - simple text splitting
- ? Questions may not make sense
- ? Quality not comparable to real AI
- ? Good enough for UI testing
- ? Allows testing Accept/Reject/Edit flow

## Usage

1. Navigate to `/generate`
2. Paste text (50-20,000 characters)
3. Click "Generate Flashcards"
4. Redirected to `/proposals/{sourceTextId}`
5. Review, edit, accept, or reject proposals
6. Accepted proposals become cards in `/cards`

## Replacing with Real AI

When ready to implement OpenRouter API:

1. Install HTTP client package
2. Add OpenRouter API key to config
3. Replace `GenerateMockProposals` method with:
```csharp
private async Task<List<CardProposal>> GenerateAIProposals(
    Guid userId, 
    Guid sourceTextId, 
    string sourceText,
    CancellationToken cancellationToken)
{
    // Call OpenRouter API
    // Parse JSON response
    // Create CardProposal entities
    // Return list
}
```

## Testing

### Test Case 1: Short text
```
Blazor is a web framework that uses C# instead of JavaScript for 
building interactive web applications. It was released in 2018.
```
Expected: 1 proposal

### Test Case 2: Medium text
```
<paste the Blazor explanation from previous message>
```
Expected: 5 proposals (max)

### Test Case 3: Long text
```
<paste 10+ paragraphs>
```
Expected: 5 proposals (max, due to limit)

## Code Location

**File:** `10xCards.Application/Services/ProposalService.cs`

**Methods:**
- `GenerateProposalsAsync` - Main entry point
- `GenerateMockProposals` - Mock generation logic
- `GenerateQuestion` - Question template logic

## Notes

- Proposals are saved to database immediately
- SourceText is saved even if generation fails
- Mock generation is deterministic (same input ? same output)
- Character limits enforced (50-500 for Front/Back)
- Fallback: If no sentences found, creates 1 generic proposal

## Future Improvements

For production OpenRouter integration:
1. Add retry logic for API failures
2. Add rate limiting
3. Add cost tracking (OpenRouter charges per token)
4. Add model selection (GPT-4, Claude, etc.)
5. Add quality scoring
6. Add caching for identical texts
