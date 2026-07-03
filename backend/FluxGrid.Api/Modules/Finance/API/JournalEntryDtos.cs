using System;
using System.Collections.Generic;

namespace FluxGrid.Api.Modules.Finance.API;

public record JournalEntryLineDto(Guid AccountId, decimal Debit, decimal Credit, string? Description);

public record CreateJournalEntryRequest(
    DateTime TransactionDate,
    string Description,
    List<JournalEntryLineDto> Lines,
    string Status = "DRAFT");

public record UpdateJournalEntryRequest(
    DateTime TransactionDate,
    string Description,
    List<JournalEntryLineDto> Lines,
    string Status = "DRAFT");
