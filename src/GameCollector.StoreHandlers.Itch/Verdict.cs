using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.Itch
{
    public class Verdict
    {
        [JsonPropertyName("basePath")]
        public string? BasePath { get; init; }

        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; init; }
    }

    public class Candidate
    {
        [JsonPropertyName("path")]
        public string? Path { get; init; }
    }
}
