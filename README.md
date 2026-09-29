# JASS Automation Hub v1.2

**JASS Automation Hub** is a local-first Windows workflow automation platform built with C#/.NET 8 WPF.

## v1.2 engine milestone

### Workflow designer
- Visual workflow canvas
- Draggable nodes
- Sequential/explicit connections
- TRUE / FALSE branch connections
- Node inspector
- Node retry count
- Enable/disable nodes
- Delete nodes
- Workflow validation

### Runtime engine
- Graph-based execution
- Variables and `{VariableName}` expansion
- Runtime Set Variable
- If Condition branching
- Loop safety guard
- Retry handling
- Cancellation / Stop
- Dry Run
- Live execution monitor
- Execution history

### Automation management
- Search
- Duplicate
- Delete
- Import `.jassautomation` / JSON
- Export `.jassautomation` / JSON
- SQLite persistence
- Run history
- Templates

### Scheduling
- Manual
- At Startup
- Every 15 minutes
- Hourly
- Daily
- Weekly

The scheduler operates while the JASS Automation Hub application is running. A future release can add a Windows Task Scheduler integration for true background execution.

## Actions

- Start
- Create Folder
- Move File
- Copy File
- Rename File
- Delete File
- Write Log
- Run Process
- HTTP GET
- Delay
- Set Variable
- If Condition
- For Each File
- Loop End

## Build

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project .\JASS.AutomationHub\JASS.AutomationHub.csproj
```

## Data

`%LOCALAPPDATA%\JASS\AutomationHub\automation-hub.db`

## Safety

Use **Validate** and **Dry Run** before workflows that modify files or launch programs.
