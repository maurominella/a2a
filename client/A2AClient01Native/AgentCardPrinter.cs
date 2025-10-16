using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentTools; // Namespace for the agent AgentCardPrinter

public static class AgentCardPrinter
{
    /// <summary>
    /// Prints the Agent "Identity Card" to the Console.
    /// </summary>
    public static void PrintAgentIdentityCard(object agentCard)
        => Console.WriteLine(RenderAgentIdentityCard(agentCard));

    /// <summary>
    /// Renders the Agent "Identity Card" as a formatted string.
    /// Pass the AgentCard instance you retrieved from A2A (e.g., cardResolver.GetAgentCardAsync().Result).
    /// </summary>
    public static string RenderAgentIdentityCard(object agentCard)
    {
        var sb = new StringBuilder();
        var line = new string('─', 70);

        if (agentCard is null)
        {
            sb.AppendLine("⚠️ AgentCard is null.");
            return sb.ToString();
        }

        // JSON serializer options for consistent readable fallbacks
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Header
        sb.AppendLine(line);
        sb.AppendLine("🤝 A2A Agent – Identity Card");
        sb.AppendLine(line);

        // Try to access common well-known simple members if they exist
        WriteIfPresent(sb, agentCard, "Id");
        WriteIfPresent(sb, agentCard, "Name");
        WriteIfPresent(sb, agentCard, "Url");
        WriteIfPresent(sb, agentCard, "Description");
        WriteIfPresent(sb, agentCard, "Version");
        WriteIfPresent(sb, agentCard, "Owner");
        WriteIfPresent(sb, agentCard, "Vendor");
        WriteIfPresent(sb, agentCard, "Category");

        // Divider
        sb.AppendLine();

        // Complex/collection members (attempt to format them nicely if present)
        WriteSectionHeader(sb, "Capabilities");
        WriteNestedIfPresent(sb, agentCard, "Capabilities", jsonOptions, preferredFlatMembers: new[]
        {
            "Streaming", "InputModalities", "OutputModalities", "Auth", "RateLimits"
        });

        WriteSectionHeader(sb, "Default Input Modes");
        WriteCollectionIfPresent(sb, agentCard, "DefaultInputModes");

        WriteSectionHeader(sb, "Skills");
        WriteCollectionIfPresent(sb, agentCard, "Skills", itemFormatter: FormatSkill);

        WriteSectionHeader(sb, "Extensions");
        WriteDictionaryIfPresent(sb, agentCard, "Extensions", jsonOptions);

        WriteSectionHeader(sb, "Endpoints");
        WriteCollectionIfPresent(sb, agentCard, "Endpoints"); // some SDKs expose contact points here

        WriteSectionHeader(sb, "Tags");
        WriteCollectionIfPresent(sb, agentCard, "Tags");

        WriteSectionHeader(sb, "Metadata");
        WriteDictionaryIfPresent(sb, agentCard, "Metadata", jsonOptions);

        // Fallback: show any remaining public properties not written above, in a compact JSON
        WriteSectionHeader(sb, "Raw View (JSON)");
        try
        {
            sb.AppendLine(JsonSerializer.Serialize(agentCard, jsonOptions));
        }
        catch
        {
            // Safe reflective fallback if serialization fails
            sb.AppendLine(ReflectiveDump(agentCard));
        }

        sb.AppendLine(line);
        return sb.ToString();
    }

    // ---------- Helpers ----------

    private static void WriteSectionHeader(StringBuilder sb, string title)
    {
        sb.AppendLine();
        sb.AppendLine($"• {title}:");
    }

    private static void WriteIfPresent(StringBuilder sb, object obj, string propName)
    {
        var prop = GetProp(obj, propName);
        if (prop is null) return;

        var value = prop.GetValue(obj, null);
        if (IsNullOrEmpty(value)) return;

        sb.AppendLine($"{propName}: {FormatScalar(value)}");
    }

    private static void WriteNestedIfPresent(
        StringBuilder sb,
        object obj,
        string propName,
        JsonSerializerOptions jsonOptions,
        string[]? preferredFlatMembers = null)
    {
        var prop = GetProp(obj, propName);
        if (prop is null) return;
        var value = prop.GetValue(obj, null);
        if (IsNullOrEmpty(value)) return;

        // If it's a simple scalar, just print it
        if (IsScalar(value))
        {
            sb.AppendLine($"{propName}: {FormatScalar(value)}");
            return;
        }

        // If it has known nested members, print those first flat
        if (preferredFlatMembers != null && preferredFlatMembers.Length > 0)
        {
            bool wroteAny = false;
            foreach (var member in preferredFlatMembers)
            {
                var nestedProp = GetProp(value, member);
                if (nestedProp == null) continue;
                var nestedVal = nestedProp.GetValue(value, null);
                if (IsNullOrEmpty(nestedVal)) continue;

                wroteAny = true;
                if (IsScalar(nestedVal))
                {
                    sb.AppendLine($"  {member}: {FormatScalar(nestedVal)}");
                }
                else if (nestedVal is IEnumerable en && !(nestedVal is string))
                {
                    int i = 0;
                    foreach (var item in en)
                    {
                        sb.AppendLine($"  {member}[{i++}]: {FormatAuto(item, jsonOptions)}");
                    }
                    if (i == 0) sb.AppendLine($"  {member}: (empty)");
                }
                else
                {
                    sb.AppendLine($"  {member}: {FormatAuto(nestedVal, jsonOptions)}");
                }
            }

            if (!wroteAny)
            {
                // Fall back to JSON if nothing matched
                sb.AppendLine(FormatAuto(value, jsonOptions, indent: "  "));
            }
        }
        else
        {
            sb.AppendLine(FormatAuto(value, jsonOptions, indent: "  "));
        }
    }

    private static void WriteCollectionIfPresent(
        StringBuilder sb,
        object obj,
        string propName,
        Func<object, string>? itemFormatter = null)
    {
        var prop = GetProp(obj, propName);
        if (prop is null) return;

        var value = prop.GetValue(obj, null);
        if (value is null) return;

        if (value is IEnumerable en && value is not string)
        {
            int i = 0;
            foreach (var item in en)
            {
                var line = itemFormatter != null ? itemFormatter(item) : FormatScalarOrJson(item);
                sb.AppendLine($"  [{i++}] {line}");
            }
            if (i == 0) sb.AppendLine("  (empty)");
        }
        else
        {
            sb.AppendLine($"  {propName}: {FormatScalarOrJson(value)}");
        }
    }

    private static void WriteDictionaryIfPresent(
        StringBuilder sb,
        object obj,
        string propName,
        JsonSerializerOptions jsonOptions)
    {
        var prop = GetProp(obj, propName);
        if (prop is null) return;

        var value = prop.GetValue(obj, null);
        if (value is null) return;

        if (value is IDictionary dict)
        {
            if (dict.Count == 0)
            {
                sb.AppendLine("  (empty)");
                return;
            }
            foreach (DictionaryEntry entry in dict)
            {
                var k = entry.Key?.ToString() ?? "(null)";
                var v = entry.Value;
                sb.AppendLine($"  {k}: {FormatAuto(v, jsonOptions)}");
            }
        }
        else
        {
            // Fallback to JSON
            sb.AppendLine(FormatAuto(value, jsonOptions, indent: "  "));
        }
    }

    private static string FormatSkill(object skill)
    {
        if (skill is null) return "(null)";
        // Try common skill-like members (Name, Description, Operations)
        var name = GetProp(skill, "Name")?.GetValue(skill, null)?.ToString();
        var desc = GetProp(skill, "Description")?.GetValue(skill, null)?.ToString();
        var opsProp = GetProp(skill, "Operations") ?? GetProp(skill, "Functions") ?? GetProp(skill, "Actions");

        string opsSnippet = string.Empty;
        if (opsProp != null)
        {
            var opsVal = opsProp.GetValue(skill, null);
            if (opsVal is IEnumerable en && opsVal is not string)
            {
                var ops = en.Cast<object?>()
                            .Select(o => GetProp(o, "Name")?.GetValue(o, null)?.ToString()
                                      ?? o?.ToString()
                                      ?? "(op)")
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();
                if (ops.Count > 0) opsSnippet = $" | Ops: {string.Join(", ", ops)}";
            }
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(name)) parts.Add(name);
        if (!string.IsNullOrWhiteSpace(desc)) parts.Add(desc);
        var head = parts.Count > 0 ? string.Join(" — ", parts) : skill.ToString();

        return (head ?? "(skill)") + opsSnippet;
    }

    private static string FormatScalarOrJson(object value)
        => IsScalar(value) ? FormatScalar(value) : SerializeSafe(value);

    private static string FormatScalar(object value)
    {
        return value switch
        {
            null => "(null)",
            string s when string.IsNullOrWhiteSpace(s) => "(empty)",
            string s => s,
            bool b => b ? "true" : "false",
            DateTime dt => dt.ToString("o"),
            Uri uri => uri.ToString(),
            _ => value.ToString() ?? "(null)"
        };
    }

    private static string SerializeSafe(object value)
    {
        try
        {
            return JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch
        {
            return ReflectiveDump(value);
        }
    }

    private static string FormatAuto(object? value, JsonSerializerOptions jsonOptions, string? indent = null)
    {
        if (value is null) return "(null)";
        if (IsScalar(value)) return FormatScalar(value);

        try
        {
            var json = JsonSerializer.Serialize(value, jsonOptions);
            if (!string.IsNullOrEmpty(indent))
            {
                // add indent to each line
                var prefixed = string.Join(Environment.NewLine, json.Split('\n').Select(l => indent + l));
                return Environment.NewLine + prefixed;
            }
            return json;
        }
        catch
        {
            var dump = ReflectiveDump(value);
            if (!string.IsNullOrEmpty(indent))
            {
                var prefixed = string.Join(Environment.NewLine, dump.Split('\n').Select(l => indent + l));
                return Environment.NewLine + prefixed;
            }
            return dump;
        }
    }

    private static string ReflectiveDump(object obj)
    {
        var sb = new StringBuilder();
        var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.Name);

        sb.Append("{ ");
        sb.Append(string.Join(", ",
            props.Select(p =>
            {
                try
                {
                    var val = p.GetValue(obj, null);
                    return $"{p.Name}: {(IsScalar(val) ? FormatScalar(val!) : "[object]")}";
                }
                catch
                {
                    return $"{p.Name}: <unavailable>";
                }
            })));
        sb.Append(" }");
        return sb.ToString();
    }

    private static PropertyInfo? GetProp(object obj, string name)
        => obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

    private static bool IsScalar(object? value)
        => value is null
           || value is string
           || value is bool
           || value is char
           || value is byte or sbyte
           || value is short or ushort
           || value is int or uint
           || value is long or ulong
           || value is float or double or decimal
           || value is DateTime or DateTimeOffset
           || value is Guid
           || value is Uri;

    private static bool IsNullOrEmpty(object? value)
    {
        if (value is null) return true;
        if (value is string s) return string.IsNullOrWhiteSpace(s);
        if (value is IEnumerable en && value is not string)
        {
            foreach (var _ in en) return false;
            return true;
        }
        return false;
    }
}