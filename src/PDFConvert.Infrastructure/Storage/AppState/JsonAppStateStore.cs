using System.Text.Json;

namespace PDFConvert.Infrastructure.Storage.AppState;

public sealed class JsonAppStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _stateFilePath;
    private readonly object _syncRoot = new();

    public JsonAppStateStore(string stateFilePath)
    {
        _stateFilePath = stateFilePath;
    }

    internal AppStateDocument Load()
    {
        lock (_syncRoot)
        {
            if (!File.Exists(_stateFilePath))
            {
                return new AppStateDocument();
            }

            try
            {
                var json = File.ReadAllText(_stateFilePath);
                return JsonSerializer.Deserialize<AppStateDocument>(json, SerializerOptions) ?? new AppStateDocument();
            }
            catch
            {
                return new AppStateDocument();
            }
        }
    }

    internal void Save(AppStateDocument document)
    {
        lock (_syncRoot)
        {
            var directory = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(document, SerializerOptions);
            File.WriteAllText(_stateFilePath, json);
        }
    }
}
