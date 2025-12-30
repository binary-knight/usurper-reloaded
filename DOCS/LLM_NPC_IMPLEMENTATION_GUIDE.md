# LLM-Powered NPC Implementation Guide
## Usurper Reloaded - AI-Enhanced NPCs

**Document Version**: 1.0
**Last Updated**: 2025-01-29
**Status**: Design Specification

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Architecture Overview](#architecture-overview)
3. [Implementation Phases](#implementation-phases)
4. [Technical Specifications](#technical-specifications)
5. [Cost Analysis](#cost-analysis)
6. [Security & Safety](#security--safety)
7. [Testing Strategy](#testing-strategy)
8. [Deployment Guide](#deployment-guide)

---

## Executive Summary

### Vision
Transform Usurper Reloaded's 50 NPCs from scripted characters into truly intelligent, conversational agents using Large Language Models (LLMs). Each NPC will have persistent memory, personality-driven responses, and the ability to engage in natural dialogue with players.

### Key Benefits
- **Unique Playthroughs**: Every conversation is dynamically generated
- **Marketing Appeal**: "First BBS game with real AI NPCs"
- **Player Engagement**: NPCs feel genuinely alive and memorable
- **Viral Potential**: Players share amazing/funny NPC interactions
- **Scalability**: Easy to add more NPCs without writing dialogue trees

### Technical Approach
- **Hybrid System**: LLM for important conversations, scripted for simple interactions
- **Multi-Provider Support**: Claude API, OpenAI, local Ollama, or offline fallback
- **Context-Aware**: NPCs remember conversations, relationships, and world events
- **Cost-Efficient**: Caching, semantic deduplication, and smart fallbacks

---

## Architecture Overview

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                    PLAYER INTERACTION                        │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│              DIALOGUE ORCHESTRATOR                           │
│  - Determines if LLM needed vs scripted response            │
│  - Manages conversation context                             │
│  - Handles caching and deduplication                        │
└─────────────────┬────────────────────┬──────────────────────┘
                  │                    │
         ┌────────▼────────┐  ┌────────▼──────────┐
         │   LLM SERVICE   │  │ SCRIPTED DIALOGUE │
         │   (Adaptive)    │  │   (Fallback)      │
         └────────┬────────┘  └────────┬──────────┘
                  │                    │
         ┌────────▼────────────────────▼──────────┐
         │         RESPONSE PROCESSOR              │
         │  - Safety filtering                     │
         │  - Personality enforcement              │
         │  - Memory updating                      │
         └────────┬────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│                    NPC MEMORY SYSTEM                         │
│  - Conversation history                                      │
│  - Relationship tracking                                     │
│  - World event knowledge                                     │
└─────────────────────────────────────────────────────────────┘
```

### File Structure

```
Scripts/
├── AI/
│   ├── LLM/
│   │   ├── ILLMProvider.cs          # Provider interface
│   │   ├── LLMService.cs            # Main orchestrator
│   │   ├── Providers/
│   │   │   ├── ClaudeProvider.cs    # Anthropic Claude API
│   │   │   ├── OpenAIProvider.cs    # OpenAI GPT API
│   │   │   ├── OllamaProvider.cs    # Local Ollama
│   │   │   └── GroqProvider.cs      # Groq (fast cloud)
│   │   ├── PromptBuilder.cs         # Constructs LLM prompts
│   │   ├── ResponseCache.cs         # Semantic caching
│   │   └── SafetyFilter.cs          # Content moderation
│   ├── NPCDialogue.cs               # Dialogue manager
│   └── ConversationContext.cs       # Context tracking
├── Systems/
│   └── LLMConfig.cs                 # Configuration loader
└── Data/
    └── PromptTemplates/
        ├── npc_system_prompt.txt    # Base NPC prompt
        └── personality_prompts.txt  # Per-archetype prompts
```

---

## Implementation Phases

### Phase 1: Foundation (Week 1)
**Goal**: Basic LLM integration with single provider

**Tasks**:
1. Create `ILLMProvider` interface
2. Implement `ClaudeProvider` (recommended starting point)
3. Build `PromptBuilder` with basic NPC context
4. Add `/talk <npc_name>` command to game
5. Test with 1-2 NPCs in controlled environment

**Deliverables**:
- Working Claude API integration
- Simple conversational NPCs
- Basic error handling

**Success Criteria**:
- Player can talk to Sir Galahad and get in-character responses
- NPC remembers previous turn of conversation
- Graceful fallback if API fails

---

### Phase 2: Context & Memory (Week 2)
**Goal**: NPCs remember and reference past interactions

**Tasks**:
1. Extend `MemorySystem` to log LLM conversations
2. Build conversation summarization (use LLM to compress old chats)
3. Integrate relationship system with prompts
4. Add world event awareness (NPCs know about player's deeds)
5. Implement personality-specific prompt templates

**Deliverables**:
- NPCs reference past conversations
- Relationship affects dialogue tone
- NPCs react to player's reputation/achievements

**Success Criteria**:
- Talk to NPC on Day 1, they're friendly
- Kill their friend, talk on Day 2, they're hostile
- NPC mentions "last time we spoke, you said..."

---

### Phase 3: Multi-Provider & Optimization (Week 3)
**Goal**: Add provider options and reduce costs

**Tasks**:
1. Implement `OpenAIProvider` and `OllamaProvider`
2. Build response caching system
3. Add semantic deduplication (similar inputs → cached response)
4. Create hybrid decision logic (LLM vs scripted)
5. Implement conversation compression

**Deliverables**:
- 3+ provider options
- 50-70% cache hit rate
- Intelligent LLM usage (only when needed)

**Success Criteria**:
- Can switch providers via config
- Common greetings use cached responses
- Unique/complex questions trigger LLM

---

### Phase 4: Safety & Polish (Week 4)
**Goal**: Production-ready with safety guardrails

**Tasks**:
1. Implement content filtering (profanity, abuse, modern references)
2. Add personality drift detection (NPC stays in character)
3. Build monitoring dashboard (usage, costs, errors)
4. Create comprehensive test suite
5. Write player-facing documentation

**Deliverables**:
- Safety filters prevent abuse
- Admin dashboard shows LLM metrics
- User guide for conversing with NPCs

**Success Criteria**:
- Cannot trick NPC into breaking character
- Inappropriate player input handled gracefully
- 99%+ uptime for LLM features

---

## Technical Specifications

### 1. LLM Provider Interface

```csharp
namespace UsurperRemake.AI.LLM
{
    public interface ILLMProvider
    {
        /// <summary>
        /// Provider name (e.g., "Claude", "OpenAI", "Ollama")
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Whether provider is available (API key set, service reachable)
        /// </summary>
        Task<bool> IsAvailable();

        /// <summary>
        /// Generate NPC response to player input
        /// </summary>
        /// <param name="systemPrompt">NPC personality and context</param>
        /// <param name="conversationHistory">Recent dialogue</param>
        /// <param name="userMessage">Player's latest input</param>
        /// <param name="maxTokens">Response length limit</param>
        /// <param name="temperature">Randomness (0.0-1.0)</param>
        /// <returns>NPC's response text</returns>
        Task<LLMResponse> GenerateResponse(
            string systemPrompt,
            List<Message> conversationHistory,
            string userMessage,
            int maxTokens = 150,
            float temperature = 0.8f
        );

        /// <summary>
        /// Estimate cost for a given request
        /// </summary>
        decimal EstimateCost(int inputTokens, int outputTokens);
    }

    public class LLMResponse
    {
        public string Text { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public decimal Cost { get; set; }
        public TimeSpan Latency { get; set; }
        public bool FromCache { get; set; }
    }

    public class Message
    {
        public string Role { get; set; } // "user" or "assistant"
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

---

### 2. Claude API Provider Implementation

```csharp
using System.Net.Http;
using System.Text.Json;

namespace UsurperRemake.AI.LLM.Providers
{
    public class ClaudeProvider : ILLMProvider
    {
        private readonly HttpClient httpClient;
        private readonly string apiKey;
        private const string API_URL = "https://api.anthropic.com/v1/messages";

        // Pricing (as of Jan 2025)
        private const decimal INPUT_COST_PER_MILLION = 0.25m;  // Haiku
        private const decimal OUTPUT_COST_PER_MILLION = 1.25m;

        public string Name => "Claude";

        public ClaudeProvider(string apiKey)
        {
            this.apiKey = apiKey;
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<bool> IsAvailable()
        {
            if (string.IsNullOrEmpty(apiKey)) return false;

            try
            {
                // Quick ping to check API availability
                var testRequest = new
                {
                    model = "claude-3-haiku-20240307",
                    max_tokens = 1,
                    messages = new[] { new { role = "user", content = "Hi" } }
                };

                var response = await httpClient.PostAsJsonAsync(API_URL, testRequest);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<LLMResponse> GenerateResponse(
            string systemPrompt,
            List<Message> conversationHistory,
            string userMessage,
            int maxTokens = 150,
            float temperature = 0.8f)
        {
            var startTime = DateTime.Now;

            // Build messages array
            var messages = new List<object>();
            foreach (var msg in conversationHistory)
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }
            messages.Add(new { role = "user", content = userMessage });

            // Construct request
            var request = new
            {
                model = "claude-3-haiku-20240307",
                max_tokens = maxTokens,
                temperature = temperature,
                system = systemPrompt,
                messages = messages.ToArray()
            };

            // Send to API
            var response = await httpClient.PostAsJsonAsync(API_URL, request);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var parsed = JsonDocument.Parse(jsonResponse);

            // Extract response
            var content = parsed.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            var usage = parsed.RootElement.GetProperty("usage");
            var inputTokens = usage.GetProperty("input_tokens").GetInt32();
            var outputTokens = usage.GetProperty("output_tokens").GetInt32();

            return new LLMResponse
            {
                Text = content,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                Cost = EstimateCost(inputTokens, outputTokens),
                Latency = DateTime.Now - startTime,
                FromCache = false
            };
        }

        public decimal EstimateCost(int inputTokens, int outputTokens)
        {
            var inputCost = (inputTokens / 1_000_000m) * INPUT_COST_PER_MILLION;
            var outputCost = (outputTokens / 1_000_000m) * OUTPUT_COST_PER_MILLION;
            return inputCost + outputCost;
        }
    }
}
```

---

### 3. Prompt Builder

```csharp
namespace UsurperRemake.AI.LLM
{
    public class PromptBuilder
    {
        /// <summary>
        /// Build system prompt for NPC
        /// </summary>
        public static string BuildNPCSystemPrompt(NPC npc, Player player, GameState gameState)
        {
            var sb = new StringBuilder();

            // Identity
            sb.AppendLine($"You are {npc.Name2}, a {npc.Race} {npc.Class} in the medieval kingdom of Usurper.");
            sb.AppendLine($"You are level {npc.Level} and currently at {GetLocationName(npc.CurrentLocation)}.");
            sb.AppendLine();

            // Personality
            sb.AppendLine("PERSONALITY PROFILE:");
            sb.AppendLine($"- Archetype: {npc.Personality.Archetype}");
            sb.AppendLine($"- Aggression: {npc.Personality.Aggression:F2} (0=peaceful, 1=violent)");
            sb.AppendLine($"- Greed: {npc.Personality.Greed:F2} (0=generous, 1=greedy)");
            sb.AppendLine($"- Loyalty: {npc.Personality.Loyalty:F2} (0=treacherous, 1=devoted)");
            sb.AppendLine($"- Courage: {npc.Personality.Courage:F2} (0=cowardly, 1=fearless)");
            sb.AppendLine($"- Sociability: {npc.Personality.Sociability:F2} (0=reclusive, 1=friendly)");

            // Alignment
            var alignment = npc.Chivalry > npc.Darkness ? "Good" :
                           npc.Darkness > npc.Chivalry ? "Evil" : "Neutral";
            sb.AppendLine($"- Alignment: {alignment} (Chivalry: {npc.Chivalry}, Darkness: {npc.Darkness})");
            sb.AppendLine();

            // Current state
            sb.AppendLine("CURRENT STATE:");
            sb.AppendLine($"- Health: {npc.HP}/{npc.MaxHP}");
            sb.AppendLine($"- Gold: {npc.Gold}");
            sb.AppendLine($"- Mood: {npc.EmotionalState?.Mood ?? "Neutral"}");
            if (npc.HP < npc.MaxHP / 2)
                sb.AppendLine("- You are wounded and cautious");
            if (npc.Gold < 100)
                sb.AppendLine("- You are low on funds and concerned about money");
            sb.AppendLine();

            // Relationship with player
            var relationship = npc.Relationships?.GetRelationship(player.Name2) ?? 0f;
            sb.AppendLine("RELATIONSHIP WITH PLAYER:");
            sb.AppendLine($"- Player Name: {player.Name2} (Level {player.Level} {player.Class})");
            sb.AppendLine($"- Relationship: {GetRelationshipDescription(relationship)}");
            sb.AppendLine($"- Trust Level: {relationship:F2} (-1.0 to 1.0)");
            sb.AppendLine();

            // Recent memories
            var recentMemories = GetRecentMemories(npc, player, 5);
            if (recentMemories.Count > 0)
            {
                sb.AppendLine("RECENT MEMORIES:");
                foreach (var memory in recentMemories)
                {
                    sb.AppendLine($"- {memory.Description}");
                }
                sb.AppendLine();
            }

            // Current goals
            var activeGoals = npc.Goals?.GetTopGoals(3);
            if (activeGoals?.Count > 0)
            {
                sb.AppendLine("CURRENT GOALS:");
                foreach (var goal in activeGoals)
                {
                    sb.AppendLine($"- {goal.Name} (Priority: {goal.Priority:F2})");
                }
                sb.AppendLine();
            }

            // World context
            sb.AppendLine("WORLD KNOWLEDGE:");
            sb.AppendLine($"- Current King: {gameState.KingName ?? "None (throne vacant)"}");
            if (player.DaysInPower > 0)
                sb.AppendLine($"- The player has ruled as king for {player.DaysInPower} days");
            sb.AppendLine($"- Player's reputation: {GetReputationDescription(player)}");
            sb.AppendLine();

            // Instructions
            sb.AppendLine("IMPORTANT INSTRUCTIONS:");
            sb.AppendLine("1. Stay in character at ALL times");
            sb.AppendLine("2. Respond in 1-2 sentences (this is a text game, be concise)");
            sb.AppendLine("3. Reference your personality traits naturally");
            sb.AppendLine("4. Remember your relationship with the player");
            sb.AppendLine("5. Use medieval language but remain understandable");
            sb.AppendLine("6. DO NOT break the fourth wall or mention being an AI");
            sb.AppendLine("7. DO NOT use modern slang or references");
            sb.AppendLine("8. If asked something you shouldn't know, stay in character and deflect");

            return sb.ToString();
        }

        private static string GetRelationshipDescription(float relationship)
        {
            return relationship switch
            {
                >= 0.8f => "Deeply Trusted Friend",
                >= 0.5f => "Friendly Acquaintance",
                >= 0.2f => "Cautiously Positive",
                >= -0.2f => "Neutral Stranger",
                >= -0.5f => "Mistrustful",
                >= -0.8f => "Enemy",
                _ => "Mortal Enemy"
            };
        }

        private static string GetReputationDescription(Player player)
        {
            if (player.Chivalry > 800) return "Legendary Hero";
            if (player.Chivalry > 500) return "Noble Champion";
            if (player.Darkness > 800) return "Feared Tyrant";
            if (player.Darkness > 500) return "Notorious Villain";
            if (player.Level > 50) return "Powerful Adventurer";
            return "Common Adventurer";
        }

        private static List<MemoryEvent> GetRecentMemories(NPC npc, Player player, int count)
        {
            return npc.Memory?.GetRecentEvents(count)
                .Where(e => e.InvolvedCharacter == player.Name2)
                .ToList() ?? new List<MemoryEvent>();
        }

        private static string GetLocationName(string locationId)
        {
            // Map location IDs to readable names
            return locationId switch
            {
                "0" => "Main Street",
                "1" => "Market",
                "2" => "The Inn",
                "3" => "The Temple",
                "4" => "The Gym",
                "10" => "Weapon Shop",
                "11" => "Armor Shop",
                "20" => "Love Corner",
                _ => "Town"
            };
        }
    }
}
```

---

### 4. LLM Service Orchestrator

```csharp
namespace UsurperRemake.AI.LLM
{
    public class LLMService
    {
        private readonly ILLMProvider provider;
        private readonly ResponseCache cache;
        private readonly SafetyFilter safety;
        private readonly LLMConfig config;

        public LLMService(LLMConfig config)
        {
            this.config = config;
            this.provider = CreateProvider(config);
            this.cache = new ResponseCache();
            this.safety = new SafetyFilter();
        }

        /// <summary>
        /// Get NPC's response to player input
        /// </summary>
        public async Task<NPCDialogueResult> GetNPCResponse(
            NPC npc,
            Player player,
            string playerInput,
            ConversationContext context)
        {
            // Check if should use LLM or scripted response
            if (!ShouldUseLLM(playerInput, context))
            {
                return GetScriptedResponse(npc, playerInput);
            }

            // Safety check on player input
            if (!safety.IsInputSafe(playerInput))
            {
                return new NPCDialogueResult
                {
                    Response = GetRejectionResponse(npc),
                    WasGenerated = false,
                    WasCached = false
                };
            }

            // Check cache
            var cacheKey = GenerateCacheKey(npc, playerInput, context);
            if (cache.TryGet(cacheKey, out var cachedResponse))
            {
                return new NPCDialogueResult
                {
                    Response = cachedResponse,
                    WasGenerated = true,
                    WasCached = true
                };
            }

            // Generate with LLM
            var systemPrompt = PromptBuilder.BuildNPCSystemPrompt(
                npc, player, context.GameState);

            var conversationHistory = BuildConversationHistory(context);

            var llmResponse = await provider.GenerateResponse(
                systemPrompt,
                conversationHistory,
                playerInput,
                maxTokens: config.MaxTokens,
                temperature: config.Temperature
            );

            // Safety check on output
            var safeResponse = safety.FilterResponse(llmResponse.Text, npc);

            // Cache for future use
            cache.Set(cacheKey, safeResponse);

            // Update NPC memory
            UpdateNPCMemory(npc, player, playerInput, safeResponse);

            return new NPCDialogueResult
            {
                Response = safeResponse,
                WasGenerated = true,
                WasCached = false,
                TokensUsed = llmResponse.InputTokens + llmResponse.OutputTokens,
                Cost = llmResponse.Cost,
                Latency = llmResponse.Latency
            };
        }

        /// <summary>
        /// Decide if LLM should be used for this interaction
        /// </summary>
        private bool ShouldUseLLM(string input, ConversationContext context)
        {
            // Always use LLM for these cases
            if (context.IsImportant) return true;
            if (context.RequiresReasoning) return true;
            if (input.Length > 50) return true; // Complex question

            // Common greetings/farewells = scripted
            var simple = new[] { "hi", "hello", "bye", "goodbye", "greetings" };
            if (simple.Any(s => input.ToLower().Contains(s)))
                return false;

            // Default to LLM for unique interactions
            return true;
        }

        /// <summary>
        /// Fallback to scripted responses
        /// </summary>
        private NPCDialogueResult GetScriptedResponse(NPC npc, string input)
        {
            var responses = new Dictionary<string, string[]>
            {
                ["greeting"] = new[]
                {
                    $"Greetings, traveler. I am {npc.Name2}.",
                    $"Well met. What brings you to me?",
                    $"Hail, stranger."
                },
                ["farewell"] = new[]
                {
                    "Fare thee well.",
                    "Safe travels.",
                    "Until we meet again."
                },
                ["default"] = new[]
                {
                    "I'm not sure what you mean.",
                    "Could you rephrase that?",
                    "I don't understand."
                }
            };

            var category = ClassifyInput(input);
            var options = responses.GetValueOrDefault(category, responses["default"]);
            var response = options[new Random().Next(options.Length)];

            return new NPCDialogueResult
            {
                Response = response,
                WasGenerated = false,
                WasCached = false
            };
        }

        private string ClassifyInput(string input)
        {
            var lower = input.ToLower();
            if (new[] { "hi", "hello", "greetings", "hail" }.Any(g => lower.Contains(g)))
                return "greeting";
            if (new[] { "bye", "goodbye", "farewell" }.Any(g => lower.Contains(g)))
                return "farewell";
            return "default";
        }

        private ILLMProvider CreateProvider(LLMConfig config)
        {
            return config.Provider.ToLower() switch
            {
                "claude" => new ClaudeProvider(config.ApiKey),
                "openai" => new OpenAIProvider(config.ApiKey),
                "ollama" => new OllamaProvider(config.OllamaUrl),
                "groq" => new GroqProvider(config.ApiKey),
                _ => throw new Exception($"Unknown LLM provider: {config.Provider}")
            };
        }

        private string GenerateCacheKey(NPC npc, string input, ConversationContext context)
        {
            // Simple cache key based on NPC and input similarity
            return $"{npc.Name2}:{NormalizeInput(input)}";
        }

        private string NormalizeInput(string input)
        {
            // Normalize for semantic similarity
            return input.ToLower().Trim().Replace("?", "").Replace("!", "");
        }

        private List<Message> BuildConversationHistory(ConversationContext context)
        {
            // Include last 3-5 turns for context
            return context.RecentMessages
                .TakeLast(5)
                .ToList();
        }

        private void UpdateNPCMemory(NPC npc, Player player, string input, string response)
        {
            npc.Memory?.RecordEvent(new MemoryEvent
            {
                Type = MemoryType.Conversation,
                Description = $"Player said: '{input}'. I responded: '{response}'",
                InvolvedCharacter = player.Name2,
                Timestamp = DateTime.Now,
                EmotionalImpact = CalculateEmotionalImpact(input, response),
                Importance = 0.5f
            });
        }

        private float CalculateEmotionalImpact(string input, string response)
        {
            // Analyze sentiment (simple version)
            var positive = new[] { "thank", "love", "great", "wonderful", "friend" };
            var negative = new[] { "hate", "terrible", "awful", "enemy", "die" };

            var inputLower = input.ToLower();
            if (positive.Any(w => inputLower.Contains(w))) return 0.3f;
            if (negative.Any(w => inputLower.Contains(w))) return -0.3f;
            return 0f;
        }

        private string GetRejectionResponse(NPC npc)
        {
            var responses = new[]
            {
                "I'll not respond to such talk.",
                "Watch your tongue, stranger.",
                "Speak with more respect, or not at all."
            };
            return responses[new Random().Next(responses.Length)];
        }
    }

    public class NPCDialogueResult
    {
        public string Response { get; set; }
        public bool WasGenerated { get; set; }
        public bool WasCached { get; set; }
        public int TokensUsed { get; set; }
        public decimal Cost { get; set; }
        public TimeSpan Latency { get; set; }
    }

    public class ConversationContext
    {
        public GameState GameState { get; set; }
        public List<Message> RecentMessages { get; set; } = new();
        public bool IsImportant { get; set; }
        public bool RequiresReasoning { get; set; }
        public float RelationshipLevel { get; set; }
    }
}
```

---

### 5. Response Cache

```csharp
namespace UsurperRemake.AI.LLM
{
    public class ResponseCache
    {
        private readonly Dictionary<string, CacheEntry> cache = new();
        private readonly TimeSpan expirationTime = TimeSpan.FromHours(24);

        public bool TryGet(string key, out string response)
        {
            if (cache.TryGetValue(key, out var entry))
            {
                if (DateTime.Now - entry.Timestamp < expirationTime)
                {
                    response = entry.Response;
                    entry.HitCount++;
                    return true;
                }

                // Expired, remove
                cache.Remove(key);
            }

            response = null;
            return false;
        }

        public void Set(string key, string response)
        {
            cache[key] = new CacheEntry
            {
                Response = response,
                Timestamp = DateTime.Now,
                HitCount = 0
            };
        }

        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                TotalEntries = cache.Count,
                TotalHits = cache.Values.Sum(e => e.HitCount),
                OldestEntry = cache.Values.Min(e => e.Timestamp),
                MostPopularKey = cache.OrderByDescending(kv => kv.Value.HitCount)
                    .FirstOrDefault().Key
            };
        }

        private class CacheEntry
        {
            public string Response { get; set; }
            public DateTime Timestamp { get; set; }
            public int HitCount { get; set; }
        }
    }

    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int TotalHits { get; set; }
        public DateTime OldestEntry { get; set; }
        public string MostPopularKey { get; set; }
    }
}
```

---

### 6. Safety Filter

```csharp
namespace UsurperRemake.AI.LLM
{
    public class SafetyFilter
    {
        private readonly HashSet<string> blockedWords;
        private readonly HashSet<string> modernReferences;

        public SafetyFilter()
        {
            blockedWords = LoadBlockedWords();
            modernReferences = LoadModernReferences();
        }

        public bool IsInputSafe(string input)
        {
            var lower = input.ToLower();

            // Block excessive profanity
            var profanityCount = blockedWords.Count(w => lower.Contains(w));
            if (profanityCount > 2) return false;

            // Block attempts to break character
            var breakAttempts = new[]
            {
                "ignore previous instructions",
                "you are an ai",
                "forget your role",
                "system prompt"
            };

            return !breakAttempts.Any(attempt => lower.Contains(attempt));
        }

        public string FilterResponse(string response, NPC npc)
        {
            // Remove modern references
            var filtered = response;
            foreach (var reference in modernReferences)
            {
                if (filtered.Contains(reference, StringComparison.OrdinalIgnoreCase))
                {
                    // Replace with medieval equivalent or remove
                    filtered = filtered.Replace(reference, "[removed]");
                }
            }

            // Ensure NPC name is used correctly
            if (!filtered.Contains(npc.Name2))
            {
                // NPC didn't use their own name, acceptable
            }

            // Trim to reasonable length
            if (filtered.Length > 500)
            {
                filtered = filtered.Substring(0, 497) + "...";
            }

            return filtered;
        }

        private HashSet<string> LoadBlockedWords()
        {
            // Load from file or define inline
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Add profanity list here
                "extreme_profanity_1",
                "extreme_profanity_2"
                // ... etc
            };
        }

        private HashSet<string> LoadModernReferences()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "internet", "computer", "smartphone", "email",
                "television", "car", "airplane", "robot",
                "covid", "2020", "21st century", "modern"
            };
        }
    }
}
```

---

### 7. Configuration

```csharp
namespace UsurperRemake.Systems
{
    public class LLMConfig
    {
        public string Provider { get; set; } = "claude";
        public string ApiKey { get; set; } = "";
        public string OllamaUrl { get; set; } = "http://localhost:11434";
        public int MaxTokens { get; set; } = 150;
        public float Temperature { get; set; } = 0.8f;
        public bool EnableCaching { get; set; } = true;
        public bool FallbackToScripted { get; set; } = true;
        public bool Enabled { get; set; } = true;

        public static LLMConfig Load(string configPath)
        {
            if (!File.Exists(configPath))
            {
                var defaultConfig = new LLMConfig();
                Save(defaultConfig, configPath);
                return defaultConfig;
            }

            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<LLMConfig>(json);
        }

        public static void Save(LLMConfig config, string configPath)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(configPath, json);
        }
    }
}
```

**Example `llm_config.json`**:
```json
{
  "Provider": "claude",
  "ApiKey": "sk-ant-api03-...",
  "OllamaUrl": "http://localhost:11434",
  "MaxTokens": 150,
  "Temperature": 0.8,
  "EnableCaching": true,
  "FallbackToScripted": true,
  "Enabled": true
}
```

---

### 8. Game Integration - Talk Command

```csharp
// In MainStreetLocation.cs or BaseLocation.cs
private async Task TalkToNPC(Player player)
{
    // Get NPCs at current location
    var npcs = NPCSpawnSystem.Instance.GetNPCsAtLocation(currentLocationId);

    if (npcs.Count == 0)
    {
        terminal.WriteLine("There's nobody here to talk to.", "yellow");
        return;
    }

    // List available NPCs
    terminal.WriteLine("Who would you like to talk to?", "cyan");
    for (int i = 0; i < npcs.Count; i++)
    {
        terminal.WriteLine($"{i + 1}. {npcs[i].Name2} (Level {npcs[i].Level} {npcs[i].Class})", "white");
    }

    var choice = await terminal.GetInput("Choice (or 0 to cancel): ");
    if (!int.TryParse(choice, out var index) || index < 1 || index > npcs.Count)
    {
        return;
    }

    var npc = npcs[index - 1];
    await StartConversation(player, npc);
}

private async Task StartConversation(Player player, NPC npc)
{
    terminal.WriteLine($"\nYou approach {npc.Name2}.", "cyan");
    terminal.WriteLine("(Type 'goodbye' to end conversation)\n", "gray");

    var llmService = new LLMService(LLMConfig.Load("llm_config.json"));
    var context = new ConversationContext
    {
        GameState = GetCurrentGameState(),
        RelationshipLevel = npc.Relationships?.GetRelationship(player.Name2) ?? 0f
    };

    while (true)
    {
        var input = await terminal.GetInput($"You: ");

        if (string.IsNullOrWhiteSpace(input))
            continue;

        if (input.ToLower() == "goodbye")
        {
            terminal.WriteLine($"{npc.Name2}: Farewell.", "yellow");
            break;
        }

        // Show "thinking" indicator
        terminal.Write($"{npc.Name2} is thinking", "gray");
        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(300);
            terminal.Write(".", "gray");
        }
        terminal.WriteLine();

        // Get LLM response
        var result = await llmService.GetNPCResponse(npc, player, input, context);

        // Display response with color based on relationship
        var color = result.WasGenerated ? "yellow" : "white";
        terminal.WriteLine($"{npc.Name2}: {result.Response}", color);

        // Show metadata (debug mode)
        if (GameConfig.DebugMode)
        {
            var meta = result.WasCached ? "[Cached]" :
                      result.WasGenerated ? $"[LLM - {result.Cost:C4}]" : "[Scripted]";
            terminal.WriteLine(meta, "dark_gray");
        }

        // Update context with this exchange
        context.RecentMessages.Add(new Message
        {
            Role = "user",
            Content = input,
            Timestamp = DateTime.Now
        });
        context.RecentMessages.Add(new Message
        {
            Role = "assistant",
            Content = result.Response,
            Timestamp = DateTime.Now
        });

        terminal.WriteLine();
    }
}
```

---

## Cost Analysis

### Provider Pricing Comparison (January 2025)

| Provider | Model | Input ($/1M tokens) | Output ($/1M tokens) | Speed | Quality |
|----------|-------|---------------------|----------------------|-------|---------|
| **Claude** | Haiku | $0.25 | $1.25 | Fast | Excellent |
| **Claude** | Sonnet | $3.00 | $15.00 | Medium | Best |
| **OpenAI** | GPT-4 Turbo | $10.00 | $30.00 | Slow | Excellent |
| **OpenAI** | GPT-3.5 Turbo | $0.50 | $1.50 | Fast | Good |
| **Groq** | Llama 3 70B | $0.59 | $0.79 | Very Fast | Good |
| **Ollama** | Local | Free | Free | Varies | Good |

### Usage Estimates

**Average NPC Conversation**:
- System prompt: ~800 tokens
- Player input: ~30 tokens
- Conversation history: ~200 tokens (5 turns)
- NPC response: ~80 tokens
- **Total per turn**: ~1,110 tokens (~900 input, ~80 output)

**Cost per Conversation Turn** (Claude Haiku):
- Input: (900 / 1,000,000) × $0.25 = $0.000225
- Output: (80 / 1,000,000) × $1.25 = $0.0001
- **Total**: ~$0.000325 per response

**Monthly Cost Scenarios**:

| Players | Conversations/Day | Turns/Conversation | Daily Cost | Monthly Cost |
|---------|-------------------|-------------------|------------|--------------|
| 10 | 5 | 10 | $0.16 | $4.88 |
| 50 | 5 | 10 | $0.81 | $24.38 |
| 100 | 5 | 10 | $1.63 | $48.75 |
| 500 | 5 | 10 | $8.13 | $243.75 |
| 1000 | 5 | 10 | $16.25 | $487.50 |

**With 60% cache hit rate**:
- 100 players: **~$19.50/month**
- 500 players: **~$97.50/month**
- 1000 players: **~$195/month**

### Cost Optimization Strategies

1. **Caching**: 60-70% reduction with semantic caching
2. **Hybrid Mode**: Use scripted for simple greetings (70% of interactions)
3. **Conversation Compression**: Summarize old messages to reduce context
4. **Local Fallback**: Use Ollama when API is slow/expensive
5. **Rate Limiting**: Cap conversations per player per day

**Realistic Monthly Cost**: $50-200 for moderate player base (100-500 active)

---

## Security & Safety

### Input Safety

```csharp
// Implemented in SafetyFilter.cs

1. **Profanity Filtering**: Block excessive profanity
2. **Prompt Injection Prevention**: Detect "ignore instructions" attempts
3. **Rate Limiting**: Max 50 LLM calls per player per day
4. **Input Length**: Cap at 500 characters
5. **Regex Validation**: Block suspicious patterns
```

### Output Safety

```csharp
1. **Modern Reference Filter**: Remove anachronisms
2. **Personality Enforcement**: Ensure response matches NPC personality
3. **Length Limiting**: Cap at 500 characters
4. **Content Review**: Flag responses for manual review if suspicious
5. **Fallback**: Revert to scripted if LLM produces nonsense
```

### Data Privacy

- **No PII Storage**: Don't send player emails/passwords to LLM
- **Conversation Logs**: Optional, encrypted if stored
- **API Keys**: Stored securely, never in version control
- **Audit Trail**: Log all LLM requests for debugging

### Abuse Prevention

- **Cooldowns**: 5-second delay between messages
- **Conversation Limits**: Max 20 turns before forced goodbye
- **Ban System**: Auto-ban on 3+ profanity violations
- **Reporting**: Players can report inappropriate NPC responses

---

## Testing Strategy

### Unit Tests

```csharp
[TestClass]
public class LLMServiceTests
{
    [TestMethod]
    public async Task TestCaching_SimilarInputs_ReturnsCachedResponse()
    {
        var service = new LLMService(mockConfig);

        var npc = CreateTestNPC();
        var player = CreateTestPlayer();
        var context = CreateTestContext();

        var result1 = await service.GetNPCResponse(npc, player, "Hello", context);
        var result2 = await service.GetNPCResponse(npc, player, "Hi there", context);

        Assert.IsTrue(result2.WasCached, "Second similar greeting should be cached");
    }

    [TestMethod]
    public void TestSafetyFilter_ProfanityInput_RejectsRequest()
    {
        var filter = new SafetyFilter();

        var unsafe = "you stupid [profanity] [profanity] [profanity]";
        var result = filter.IsInputSafe(unsafe);

        Assert.IsFalse(result, "Excessive profanity should be rejected");
    }

    [TestMethod]
    public void TestPromptBuilder_IncludesPersonality()
    {
        var npc = CreateAggressiveNPC();
        var player = CreateTestPlayer();
        var gameState = CreateTestGameState();

        var prompt = PromptBuilder.BuildNPCSystemPrompt(npc, player, gameState);

        Assert.IsTrue(prompt.Contains("Aggression: 0.9"),
            "Prompt should include NPC personality traits");
    }
}
```

### Integration Tests

```csharp
[TestClass]
public class NPCConversationIntegrationTests
{
    [TestMethod]
    public async Task TestFullConversation_NPCStaysInCharacter()
    {
        var service = new LLMService(testConfig);
        var npc = CreateTestNPC("Sir Galahad", "Honorable", "Good");
        var player = CreateTestPlayer();
        var context = CreateTestContext();

        // Conversation flow
        var inputs = new[]
        {
            "Hello Sir Galahad",
            "What do you think of the king?",
            "Would you join me in overthrowing him?"
        };

        foreach (var input in inputs)
        {
            var result = await service.GetNPCResponse(npc, player, input, context);

            // Verify response is in character
            Assert.IsFalse(result.Response.Contains("AI") || result.Response.Contains("modern"));
            Assert.IsTrue(result.Response.Length > 0);

            // Honorable NPC should refuse evil request
            if (input.Contains("overthrowing"))
            {
                Assert.IsTrue(result.Response.ToLower().Contains("never") ||
                             result.Response.ToLower().Contains("refuse"));
            }
        }
    }
}
```

### Manual QA Checklist

- [ ] NPC stays in character for 10+ turn conversation
- [ ] Relationship changes reflected in tone
- [ ] NPCs remember previous conversations
- [ ] Cache provides consistent responses to similar inputs
- [ ] Safety filter blocks profanity and abuse
- [ ] Fallback to scripted works when API fails
- [ ] Response time < 3 seconds (95th percentile)
- [ ] Cost per conversation < $0.01
- [ ] No modern references in responses
- [ ] NPCs reference their goals/memories appropriately

---

## Deployment Guide

### Development Environment Setup

1. **Install Prerequisites**:
   ```bash
   # .NET 6.0+
   dotnet --version

   # Optional: Ollama for local testing
   curl -fsSL https://ollama.com/install.sh | sh
   ollama pull llama3
   ```

2. **Configuration**:
   ```bash
   # Create config file
   cp llm_config.template.json llm_config.json

   # Edit with your API key
   nano llm_config.json
   # Set: "ApiKey": "sk-ant-api03-YOUR_KEY_HERE"
   ```

3. **Build & Test**:
   ```bash
   dotnet build usurper-reloaded.csproj
   dotnet test Tests/LLMTests.csproj
   ```

### Production Deployment

1. **Environment Variables**:
   ```bash
   # Set API key via environment (more secure than config file)
   export CLAUDE_API_KEY="sk-ant-api03-..."
   export LLM_ENABLED="true"
   export LLM_PROVIDER="claude"
   ```

2. **Rate Limiting**:
   - Configure max conversations per player per day
   - Set per-player cooldowns
   - Monitor API usage via dashboard

3. **Monitoring**:
   ```bash
   # Log LLM usage
   tail -f logs/llm_usage.log

   # Track costs
   grep "Cost:" logs/llm_usage.log | awk '{sum += $2} END {print sum}'
   ```

4. **Fallback Strategy**:
   - Primary: Claude Haiku (fast, cheap)
   - Fallback 1: Groq (if Claude fails)
   - Fallback 2: Scripted responses (if all APIs fail)

### Steam Release Considerations

1. **Player Opt-In**:
   - Make LLM features optional (GDPR compliance)
   - Players can toggle AI NPCs in settings
   - Clearly display "AI-powered conversation" indicator

2. **Offline Mode**:
   - Include Ollama model with game (Steam Workshop)
   - Allow fully offline play with local LLM
   - Scripted fallback for players without GPU

3. **Cost Management**:
   - Cap free LLM conversations per player per day (e.g., 50)
   - Optional "Pro" tier with unlimited AI conversations
   - Use caching aggressively for free tier

4. **User Transparency**:
   - Show "AI generating response..." indicator
   - Display cost/token usage in debug mode
   - Allow reporting of inappropriate responses

---

## Future Enhancements

### Phase 5+: Advanced Features

1. **Dynamic Quest Generation**
   - NPCs use LLM to create unique quests
   - Example: "Fetch me 5 wolf pelts from the northern woods, and I'll teach you a spell"

2. **Emergent Storytelling**
   - NPCs gossip about player's deeds
   - World events trigger NPC discussions
   - NPCs form opinions based on player choices

3. **Personality Evolution**
   - NPC personalities shift based on experiences
   - Traumatic events make NPCs more cautious
   - Successful friendships increase trust

4. **Multi-NPC Conversations**
   - 3-way conversations between player and 2 NPCs
   - NPCs debate/argue with each other
   - Group decision-making

5. **Voice Integration**
   - Text-to-speech for NPC responses
   - Voice cloning for unique NPC voices
   - Player voice input (speech-to-text)

6. **Memory Visualization**
   - Players can view NPC's memories of them
   - "What does Sir Galahad think of me?" command
   - Memory timeline showing relationship evolution

---

## Appendix

### A. Provider Comparison Matrix

| Feature | Claude | OpenAI | Groq | Ollama |
|---------|--------|--------|------|--------|
| Cost | Low | Medium | Low | Free |
| Speed | Fast | Medium | Very Fast | Varies |
| Quality | Excellent | Excellent | Good | Good |
| Offline | No | No | No | Yes |
| Privacy | Cloud | Cloud | Cloud | Local |
| API Stability | High | High | Medium | N/A |

**Recommendation**: Start with Claude Haiku, add Ollama as fallback

### B. Example Conversations

**Friendly NPC (Sir Galahad)**:
```
Player: "Hello Sir Galahad, how are you today?"
Galahad: "Well met, friend! The sun shines bright this morn, and my spirits are high. What brings you to seek counsel with a humble knight?"

Player: "I'm thinking about challenging the king."
Galahad: "Brave words, but the path to the throne is treacherous. Have you the strength and honor to rule justly? I would not support a usurper who seeks power for its own sake."
```

**Hostile NPC (Vex the Merciless)**:
```
Player: "Greetings, Vex."
Vex: "Bah, what do you want, worm? Speak quickly before I grow bored and remove your tongue."

Player: "I come in peace."
Vex: "Peace is for the weak. In this world, only strength matters. Now begone, before I demonstrate mine."
```

**Scheming NPC (Ravenna Shadowmoon)**:
```
Player: "What do you know about the king?"
Ravenna: "Ah, curious are we? The king grows old and paranoid, seeing enemies in every shadow... though some of those shadows are quite real. Perhaps we could discuss this matter further... in private?"

Player: "What do you propose?"
Ravenna: "Let us say I have certain... resources that could prove useful to an ambitious soul such as yourself. We should speak of this when fewer ears are listening."
```

### C. Configuration Templates

**Development Config** (`llm_config.dev.json`):
```json
{
  "Provider": "ollama",
  "ApiKey": "",
  "OllamaUrl": "http://localhost:11434",
  "MaxTokens": 200,
  "Temperature": 0.9,
  "EnableCaching": false,
  "FallbackToScripted": true,
  "Enabled": true,
  "DebugMode": true
}
```

**Production Config** (`llm_config.prod.json`):
```json
{
  "Provider": "claude",
  "ApiKey": "${CLAUDE_API_KEY}",
  "OllamaUrl": "",
  "MaxTokens": 150,
  "Temperature": 0.8,
  "EnableCaching": true,
  "FallbackToScripted": true,
  "Enabled": true,
  "DebugMode": false
}
```

### D. Troubleshooting

**Issue**: API returns 429 (rate limit)
- **Solution**: Increase caching, add retry with exponential backoff

**Issue**: Responses are too verbose
- **Solution**: Lower `MaxTokens` to 100, add instruction "Be concise" to prompt

**Issue**: NPC breaks character
- **Solution**: Strengthen system prompt, add examples of good responses

**Issue**: High latency (>5 seconds)
- **Solution**: Switch to Groq or Ollama, reduce conversation history

**Issue**: Unexpected costs
- **Solution**: Enable aggressive caching, limit daily conversations per player

---

## Conclusion

Implementing LLM-powered NPCs in Usurper Reloaded is highly feasible and would create a truly unique gaming experience. The hybrid approach balances cost, performance, and quality while providing fallbacks for reliability.

**Recommended Starting Point**:
1. Implement Claude Haiku provider (Phase 1)
2. Add conversation caching (Phase 3)
3. Test with 5-10 NPCs in closed beta
4. Expand to all 50 NPCs based on feedback

**Estimated Development Time**: 3-4 weeks for MVP, 2 months for production-ready

**Estimated Monthly Cost**: $50-200 for 100-500 active players

This would position Usurper Reloaded as **the first BBS game with real AI NPCs** - a powerful marketing hook and genuinely innovative feature.
