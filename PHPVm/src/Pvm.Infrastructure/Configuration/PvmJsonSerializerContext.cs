using System.Text.Json.Serialization;
using Pvm.Core.Models;

namespace Pvm.Infrastructure.Configuration;

/// <summary>
/// Provides source-generated JSON serialization context for AOT and trimming compatibility.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(PhpVmConfig))]
public partial class PvmJsonSerializerContext : JsonSerializerContext
{
}
