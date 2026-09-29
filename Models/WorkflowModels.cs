namespace JASS.AutomationHub.Models;

public enum ActionKind
{
    Start,
    MoveFile,
    CopyFile,
    RenameFile,
    CreateFolder,
    DeleteFile,
    WriteLog,
    RunProcess,
    HttpGet,
    Delay,
    SetVariable,
    IfCondition,
    ForEachFile,
    LoopEnd
}

public sealed class Automation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Automation";
    public string Description { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool DryRunDefault { get; set; } = true;
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime? LastRun { get; set; }
    public int RunCount { get; set; }
    public string Schedule { get; set; } = "Manual";
    public List<WorkflowNode> Nodes { get; set; } = [];
    public List<WorkflowConnection> Connections { get; set; } = [];
    public List<WorkflowVariable> Variables { get; set; } = [];
}

public sealed class WorkflowNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ActionKind Kind { get; set; }
    public string Title { get; set; } = "";
    public string Parameter { get; set; } = "";
    public string Condition { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public bool Enabled { get; set; } = true;
    public int RetryCount { get; set; }
}

public sealed class WorkflowConnection
{
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public string Label { get; set; } = "";
}

public sealed class WorkflowVariable
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

public sealed class AutomationRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AutomationId { get; set; }
    public string AutomationName { get; set; } = "";
    public DateTime Started { get; set; }
    public DateTime Finished { get; set; }
    public bool Success { get; set; }
    public bool DryRun { get; set; }
    public int CompletedSteps { get; set; }
    public string Message { get; set; } = "";
}
