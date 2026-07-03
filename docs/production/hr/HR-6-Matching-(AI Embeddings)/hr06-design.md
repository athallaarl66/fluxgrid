# Design Specifications: Job Matching (HR-6)

## 1. Screen Overview
**Page:** Job Details & Candidate Matching Dashboard

## 2. Wireframe Description
```text
=== Job Details: Senior Software Engineer ===
[Status: Published] [Edit Job]

[Tabs: Job Description | Top AI Matches | All Applicants]

(Tab: Top AI Matches)
Showing candidates with > 50% semantic match.

| Match % | Candidate Name | Key Skills Found | Actions |
| [92%]   | John Doe       | React, Next.js   | [View] [Shortlist] |
| [85%]   | Jane Smith     | Vue, Node.js     | [View] [Shortlist] |
| [78%]   | Alex Johnson   | Angular          | [View] [Shortlist] |

=== Candidate Match Details Modal ===
Title: Match Analysis (John Doe vs Senior Software Engineer)

Match Score: 92%
AI Reasoning:
"Candidate has 3 years of direct experience with React and Next.js, matching the core requirements. While they lack AWS experience (requested), their strong background in GCP shows cloud competency."

[Button: Shortlist] [Button: Reject]
```

## 3. Component Hierarchy
- `JobDetailsPage`
  - `JobHeader`
  - `Tabs`
    - `JobDescriptionPanel`
    - `AiMatchPanel`
      - `MatchRankingTable`
        - `MatchScoreBadge` (Color codes based on percentage)
        - `CandidateMatchDetailsModal`

## 4. UI Components (shadcn/ui)
- `Progress` or a circular `Ring` component to display the Match Percentage visually.
- `Badge` for "Key Skills Found" (Green for exact matches, Yellow for related matches).
- `Table` for the ranked list.

## 5. Visual Guidelines
- **Score Colors**: 
  - > 85%: Green (Excellent Match)
  - 60% - 84%: Yellow/Orange (Good Match)
  - < 60%: Gray/Red (Weak Match)
- **Transparency**: The AI Reasoning text is crucial. Users shouldn't just trust a magical "92%" number; they need a 2-sentence summary explaining *why* the AI scored it that way.

## 6. Responsive Design
- Standard desktop-first data table view. On mobile, condense the table into a list of cards showing the Match Score prominently on the right side.

## 7. States & Interactions
- **Empty State**: If no candidates match above the threshold, show a helpful message: "No strong matches found in the current pool. Try lowering the threshold or uploading more CVs."

## 8. Accessibility
- Ensure the circular progress/match score has an `aria-label` stating the exact percentage for screen readers.
