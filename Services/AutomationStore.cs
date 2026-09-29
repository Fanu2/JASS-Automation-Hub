using System.Text.Json;
using Microsoft.Data.Sqlite;
using JASS.AutomationHub.Models;

namespace JASS.AutomationHub.Services;

public sealed class AutomationStore
{
    private readonly string _db = Path.Combine(App.DataFolder, "automation-hub.db");
    private readonly string _legacy = Path.Combine(App.DataFolder, "automations.json");
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public AutomationStore()
    {
        Directory.CreateDirectory(App.DataFolder);
        using var c = Open();
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Automations(
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Description TEXT,
                Enabled INTEGER NOT NULL,
                Created TEXT NOT NULL,
                LastRun TEXT,
                RunCount INTEGER NOT NULL,
                Schedule TEXT,
                Definition TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Runs(
                Id TEXT PRIMARY KEY,
                AutomationId TEXT NOT NULL,
                AutomationName TEXT NOT NULL,
                Started TEXT NOT NULL,
                Finished TEXT NOT NULL,
                Success INTEGER NOT NULL,
                DryRun INTEGER NOT NULL,
                CompletedSteps INTEGER NOT NULL,
                Message TEXT
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public List<Automation> Load()
    {
        var result = new List<Automation>();
        using var c = Open();
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Definition FROM Automations ORDER BY Name";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var a = JsonSerializer.Deserialize<Automation>(r.GetString(0), _json);
            if (a != null) result.Add(a);
        }

        if (result.Count == 0 && File.Exists(_legacy))
        {
            try
            {
                var old = JsonSerializer.Deserialize<List<Automation>>(File.ReadAllText(_legacy), _json) ?? [];
                Save(old);
                return old;
            }
            catch { }
        }
        return result;
    }

    public void Save(IEnumerable<Automation> automations)
    {
        using var c = Open();
        c.Open();
        using var tx = c.BeginTransaction();
        foreach (var a in automations)
        {
            using var cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO Automations(Id,Name,Description,Enabled,Created,LastRun,RunCount,Schedule,Definition)
                VALUES($id,$name,$desc,$enabled,$created,$last,$runs,$schedule,$def)
                ON CONFLICT(Id) DO UPDATE SET
                    Name=$name, Description=$desc, Enabled=$enabled, LastRun=$last,
                    RunCount=$runs, Schedule=$schedule, Definition=$def;
                """;
            cmd.Parameters.AddWithValue("$id", a.Id.ToString());
            cmd.Parameters.AddWithValue("$name", a.Name);
            cmd.Parameters.AddWithValue("$desc", a.Description);
            cmd.Parameters.AddWithValue("$enabled", a.Enabled ? 1 : 0);
            cmd.Parameters.AddWithValue("$created", a.Created.ToString("O"));
            cmd.Parameters.AddWithValue("$last", a.LastRun?.ToString("O") ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$runs", a.RunCount);
            cmd.Parameters.AddWithValue("$schedule", a.Schedule);
            cmd.Parameters.AddWithValue("$def", JsonSerializer.Serialize(a, _json));
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public void SaveRun(AutomationRun run)
    {
        using var c = Open();
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Runs(Id,AutomationId,AutomationName,Started,Finished,Success,DryRun,CompletedSteps,Message)
            VALUES($id,$aid,$name,$started,$finished,$success,$dry,$steps,$message)
            """;
        cmd.Parameters.AddWithValue("$id", run.Id.ToString());
        cmd.Parameters.AddWithValue("$aid", run.AutomationId.ToString());
        cmd.Parameters.AddWithValue("$name", run.AutomationName);
        cmd.Parameters.AddWithValue("$started", run.Started.ToString("O"));
        cmd.Parameters.AddWithValue("$finished", run.Finished.ToString("O"));
        cmd.Parameters.AddWithValue("$success", run.Success ? 1 : 0);
        cmd.Parameters.AddWithValue("$dry", run.DryRun ? 1 : 0);
        cmd.Parameters.AddWithValue("$steps", run.CompletedSteps);
        cmd.Parameters.AddWithValue("$message", run.Message);
        cmd.ExecuteNonQuery();
    }

    public List<AutomationRun> LoadRuns()
    {
        var list = new List<AutomationRun>();
        using var c = Open();
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,AutomationId,AutomationName,Started,Finished,Success,DryRun,CompletedSteps,Message FROM Runs ORDER BY Started DESC LIMIT 500";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new AutomationRun
            {
                Id = Guid.Parse(r.GetString(0)),
                AutomationId = Guid.Parse(r.GetString(1)),
                AutomationName = r.GetString(2),
                Started = DateTime.Parse(r.GetString(3)),
                Finished = DateTime.Parse(r.GetString(4)),
                Success = r.GetInt32(5) == 1,
                DryRun = r.GetInt32(6) == 1,
                CompletedSteps = r.GetInt32(7),
                Message = r.IsDBNull(8) ? "" : r.GetString(8)
            });
        }
        return list;
    }

    private SqliteConnection Open() => new($"Data Source={_db}");
}
