using System.Text.Json;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Configuration;

/// <summary>
/// Manages persistence of user configuration in %LOCALAPPDATA%\pvm\config\config.json.
/// </summary>
public sealed class JsonConfigStore : IConfigStore
{
    private readonly string _configFilePath;
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonConfigStore()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configDir = Path.Combine(localAppData, "pvm", "config");
        _configFilePath = Path.Combine(configDir, "config.json");
    }

    public JsonConfigStore(string configFilePath)
    {
        _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
    }

    public PhpVmConfig Load()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                var defaultConfig = new PhpVmConfig();
                Save(defaultConfig);
                return defaultConfig;
            }

            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize(json, PvmJsonSerializerContext.Default.PhpVmConfig);
            return config ?? new PhpVmConfig();
        }
        catch
        {
            return new PhpVmConfig();
        }
    }

    public void Save(PhpVmConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        try
        {
            var dir = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(config, PvmJsonSerializerContext.Default.PhpVmConfig);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save configuration to {_configFilePath}: {ex.Message}", ex);
        }
    }

    public void Reset()
    {
        var defaultConfig = new PhpVmConfig();
        Save(defaultConfig);
    }
}
