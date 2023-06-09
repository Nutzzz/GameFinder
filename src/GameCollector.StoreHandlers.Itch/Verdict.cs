using System.Collections.Generic;

namespace GameCollector.StoreHandlers.Itch;

internal record Verdict(
    string? BasePath,
    List<Candidate>? Candidates
);

internal record Candidate(
    string? Path
);
