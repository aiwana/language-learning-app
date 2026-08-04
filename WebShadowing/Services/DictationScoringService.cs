using System.Globalization;
using System.Text;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class DictationScoringService
{
    public DictationScoringResult Evaluate(string expectedText, string actualText)
    {
        var expectedTokens = NormalizeTokens(expectedText);
        var actualTokens = NormalizeTokens(actualText);
        var diffTokens = BuildDiff(expectedTokens, actualTokens);
        var penaltyCount = diffTokens.Count(item => item.Status != "correct");
        var score = expectedTokens.Count == 0
            ? 0
            : Math.Max(0, (int)Math.Round(
                ((expectedTokens.Count - penaltyCount) * 100m) / expectedTokens.Count,
                MidpointRounding.AwayFromZero));

        return new DictationScoringResult
        {
            Score = score,
            NormalizedExpected = string.Join(' ', expectedTokens),
            NormalizedAnswer = string.Join(' ', actualTokens),
            Tokens = diffTokens
        };
    }

    private static IReadOnlyList<string> NormalizeTokens(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var normalized = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (var character in normalized)
        {
            if (character is '\'' or '\u2019')
            {
                // Ignore apostrophes without splitting tokens so "don't" and "dont" align.
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                current.Append(character);
                continue;
            }

            FlushCurrentToken(tokens, current);
        }

        FlushCurrentToken(tokens, current);
        return NormalizeNumberSequences(tokens);
    }

    private static void FlushCurrentToken(List<string> tokens, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        tokens.Add(current.ToString());
        current.Clear();
    }

    private static IReadOnlyList<string> NormalizeNumberSequences(IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return tokens;
        }

        var normalizedTokens = new List<string>(tokens.Count);
        for (var index = 0; index < tokens.Count;)
        {
            if (TryParseNumberSequence(tokens, index, out var normalized, out var consumed))
            {
                normalizedTokens.Add(normalized);
                index += consumed;
                continue;
            }

            normalizedTokens.Add(tokens[index]);
            index++;
        }

        return normalizedTokens;
    }

    private static bool TryParseNumberSequence(
        IReadOnlyList<string> tokens,
        int startIndex,
        out string normalized,
        out int consumed)
    {
        normalized = string.Empty;
        consumed = 0;

        if (startIndex >= tokens.Count)
        {
            return false;
        }

        var token = tokens[startIndex];
        if (TryNormalizeNumericToken(token, out normalized))
        {
            consumed = 1;
            return true;
        }

        if (!IsNumberWord(token))
        {
            return false;
        }

        long total = 0;
        long current = 0;
        var index = startIndex;
        var seenNumberWord = false;

        while (index < tokens.Count)
        {
            var currentToken = tokens[index];
            if (currentToken == "and")
            {
                index++;
                continue;
            }

            if (TryNormalizeNumericToken(currentToken, out var numericToken))
            {
                if (seenNumberWord)
                {
                    break;
                }

                normalized = numericToken;
                consumed = 1;
                return true;
            }

            if (TryParseNumberWord(currentToken, out var value))
            {
                current += value;
                seenNumberWord = true;
                index++;
                continue;
            }

            if (currentToken == "hundred")
            {
                current = Math.Max(1, current) * 100;
                seenNumberWord = true;
                index++;
                continue;
            }

            if (currentToken == "thousand")
            {
                current = Math.Max(1, current);
                total += current * 1000;
                current = 0;
                seenNumberWord = true;
                index++;
                continue;
            }

            if (currentToken == "million")
            {
                current = Math.Max(1, current);
                total += current * 1_000_000;
                current = 0;
                seenNumberWord = true;
                index++;
                continue;
            }

            break;
        }

        if (!seenNumberWord)
        {
            return false;
        }

        consumed = index - startIndex;
        normalized = (total + current).ToString(CultureInfo.InvariantCulture);
        return consumed > 0;
    }

    private static bool TryParseNumberWord(string token, out long value)
    {
        value = token switch
        {
            "zero" => 0,
            "one" => 1,
            "two" => 2,
            "three" => 3,
            "four" => 4,
            "five" => 5,
            "six" => 6,
            "seven" => 7,
            "eight" => 8,
            "nine" => 9,
            "ten" => 10,
            "eleven" => 11,
            "twelve" => 12,
            "thirteen" => 13,
            "fourteen" => 14,
            "fifteen" => 15,
            "sixteen" => 16,
            "seventeen" => 17,
            "eighteen" => 18,
            "nineteen" => 19,
            "twenty" => 20,
            "thirty" => 30,
            "forty" => 40,
            "fifty" => 50,
            "sixty" => 60,
            "seventy" => 70,
            "eighty" => 80,
            "ninety" => 90,
            _ => -1
        };

        return value >= 0;
    }

    private static bool TryNormalizeNumericToken(string token, out string normalized)
    {
        if (long.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            normalized = value.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        normalized = string.Empty;
        return false;
    }

    private static bool IsNumberWord(string token) =>
        token is "zero" or "one" or "two" or "three" or "four" or "five" or "six" or "seven"
            or "eight" or "nine" or "ten" or "eleven" or "twelve" or "thirteen" or "fourteen"
            or "fifteen" or "sixteen" or "seventeen" or "eighteen" or "nineteen" or "twenty"
            or "thirty" or "forty" or "fifty" or "sixty" or "seventy" or "eighty" or "ninety"
            or "hundred" or "thousand" or "million" or "and";

    private static IReadOnlyList<DictationTokenFeedbackDto> BuildDiff(
        IReadOnlyList<string> expectedTokens,
        IReadOnlyList<string> actualTokens)
    {
        var expectedCount = expectedTokens.Count;
        var actualCount = actualTokens.Count;
        var costs = new int[expectedCount + 1, actualCount + 1];

        for (var i = 0; i <= expectedCount; i++)
        {
            costs[i, 0] = i;
        }

        for (var j = 0; j <= actualCount; j++)
        {
            costs[0, j] = j;
        }

        for (var i = 1; i <= expectedCount; i++)
        {
            for (var j = 1; j <= actualCount; j++)
            {
                var match = string.Equals(expectedTokens[i - 1], actualTokens[j - 1], StringComparison.Ordinal)
                    ? costs[i - 1, j - 1]
                    : int.MaxValue;
                var substitute = costs[i - 1, j - 1] + 1;
                var delete = costs[i - 1, j] + 1;
                var insert = costs[i, j - 1] + 1;
                costs[i, j] = Math.Min(Math.Min(match, substitute), Math.Min(delete, insert));
            }
        }

        var operations = new List<DictationTokenFeedbackDto>();
        var expectedIndex = expectedCount;
        var actualIndex = actualCount;

        while (expectedIndex > 0 || actualIndex > 0)
        {
            if (expectedIndex > 0 && actualIndex > 0
                && string.Equals(expectedTokens[expectedIndex - 1], actualTokens[actualIndex - 1], StringComparison.Ordinal)
                && costs[expectedIndex, actualIndex] == costs[expectedIndex - 1, actualIndex - 1])
            {
                operations.Add(new DictationTokenFeedbackDto
                {
                    Status = "correct",
                    Actual = actualTokens[actualIndex - 1],
                    Expected = expectedTokens[expectedIndex - 1]
                });
                expectedIndex--;
                actualIndex--;
                continue;
            }

            if (expectedIndex > 0 && actualIndex > 0
                && costs[expectedIndex, actualIndex] == costs[expectedIndex - 1, actualIndex - 1] + 1)
            {
                operations.Add(new DictationTokenFeedbackDto
                {
                    Status = "substitution",
                    Actual = actualTokens[actualIndex - 1],
                    Expected = expectedTokens[expectedIndex - 1]
                });
                expectedIndex--;
                actualIndex--;
                continue;
            }

            if (expectedIndex > 0 && costs[expectedIndex, actualIndex] == costs[expectedIndex - 1, actualIndex] + 1)
            {
                operations.Add(new DictationTokenFeedbackDto
                {
                    Status = "deletion",
                    Expected = expectedTokens[expectedIndex - 1]
                });
                expectedIndex--;
                continue;
            }

            operations.Add(new DictationTokenFeedbackDto
            {
                Status = "insertion",
                Actual = actualTokens[actualIndex - 1]
            });
            actualIndex--;
        }

        operations.Reverse();
        for (var index = 0; index < operations.Count; index++)
        {
            operations[index] = operations[index] with { Index = index };
        }

        return operations;
    }
}

public sealed class DictationScoringResult
{
    public int Score { get; init; }
    public string NormalizedExpected { get; init; } = string.Empty;
    public string NormalizedAnswer { get; init; } = string.Empty;
    public IReadOnlyList<DictationTokenFeedbackDto> Tokens { get; init; } = [];
}
