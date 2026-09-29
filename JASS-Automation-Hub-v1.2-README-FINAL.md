# JASS Automation Hub v1.2

**JASS Automation Hub** is a local-first Windows workflow automation
application built with **C# / .NET 8 / WPF**.

Version **v1.2** is the frozen functional release baseline for this
project.

It provides a visual workflow designer, reusable automation definitions,
variables, conditions, loops, file/process/HTTP actions, dry-run
execution, live execution monitoring, SQLite persistence, run history,
templates, import/export, and an in-application scheduler.

------------------------------------------------------------------------

## 1. Release Status

**Version:** 1.2\
**Platform:** Windows\
**Framework:** .NET 8 (`net8.0-windows`)\
**UI:** WPF\
**Database:** SQLite\
**Storage model:** Local-first\
**Internet requirement:** None for normal local/file/process workflows

The scheduler operates while **JASS Automation Hub is running**. This
release does **not** install or configure Windows Task Scheduler.

------------------------------------------------------------------------

# 2. Requirements

## Required

-   Windows 10 or Windows 11
-   .NET 8 SDK
-   PowerShell or Windows Terminal
-   Sufficient permissions for the files/folders/processes that a
    workflow will access

## Verify .NET

Open PowerShell and run:

``` powershell
dotnet --version
```

You should have a .NET 8 SDK installed.

You can also check:

``` powershell
dotnet --list-sdks
```

------------------------------------------------------------------------

# 3. Project Structure

After extracting the source package, the project has this general
structure:

``` text
JASS-Automation-Hub-v1.2-CLEAN/
│
├── JASS.AutomationHub.sln
├── README.md
│
└── JASS.AutomationHub/
    ├── App.xaml
    ├── App.xaml.cs
    ├── GlobalUsings.cs
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs
    ├── JASS.AutomationHub.csproj
    ├── app.manifest
    │
    ├── Models/
    │   └── WorkflowModels.cs
    │
    └── Services/
        ├── AutomationStore.cs
        └── WorkflowEngine.cs
```

------------------------------------------------------------------------

# 4. IMPORTANT: Correct PowerShell Location

There are two useful ways to run the project.

## Method A --- From the solution folder

If PowerShell is at:

``` text
C:\Users\singh\Downloads\JASS-Automation-Hub-v1.2-CLEAN
```

run:

``` powershell
dotnet clean
dotnet restore
dotnet build -c Release
dotnet run --project ".\JASS.AutomationHub\JASS.AutomationHub.csproj"
```

## Method B --- From the project folder

If PowerShell is already at:

``` text
C:\Users\singh\Downloads\JASS-Automation-Hub-v1.2-CLEAN\JASS.AutomationHub
```

run:

``` powershell
dotnet clean
dotnet restore
dotnet build -c Release
dotnet run
```

### Important

Do **not** run this:

``` powershell
dotnet run --project ".\JASS.AutomationHub\JASS.AutomationHub.csproj"
```

when you are already inside the `JASS.AutomationHub` directory.

That would incorrectly look for:

``` text
JASS.AutomationHub\JASS.AutomationHub\JASS.AutomationHub.csproj
```

------------------------------------------------------------------------

# 5. Recommended Clean Build

For a completely clean build:

``` powershell
dotnet clean
dotnet restore
dotnet build -c Release
```

Expected result:

``` text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Then run:

``` powershell
dotnet run
```

if you are inside the project directory.

------------------------------------------------------------------------

# 6. First Launch

The main navigation contains:

-   Dashboard
-   Automations
-   Workflow Designer
-   Runs & History
-   Templates
-   Logs
-   Settings

The Dashboard provides:

-   automation count
-   enabled automation count
-   recorded run count
-   current engine status
-   automation list
-   quick-start templates

------------------------------------------------------------------------

# 7. Automation Library

Open:

**Automations**

The Automation Library allows you to:

-   search automations
-   open the workflow designer
-   run an automation
-   perform a Dry Run
-   validate a workflow
-   duplicate an automation
-   export an automation
-   import an automation
-   delete an automation

Select an automation from the list before using the action buttons.

------------------------------------------------------------------------

# 8. Creating a New Automation

Click:

**+ New Automation**

or use a template from the Dashboard.

A new automation starts with a **Start** node.

Give the automation a meaningful name.

Example:

``` text
Test File Copy
```

Then open the designer.

------------------------------------------------------------------------

# 9. Workflow Designer

The Workflow Designer contains three main areas:

``` text
+----------------+--------------------------+----------------+
| Action Palette | Workflow Canvas         | Inspector      |
|                |                          |                |
| Actions        | Workflow Nodes          | Configuration  |
+----------------+--------------------------+----------------+
```

## Selecting a node

Click the node.

The selected node is highlighted and its configuration appears in the
Inspector.

## Moving a node

Drag the node to a new location.

## Configuring a node

Select the node and edit:

-   Parameter
-   Condition
-   Retry Count
-   Enabled

Then save the workflow.

------------------------------------------------------------------------

# 10. Workflow Connections

The workflow is executed as a graph.

To create a normal connection:

1.  Select the source node.
2.  Click:

``` text
Connect Selected → Shift-click Target
```

3.  Hold **Shift**.
4.  Click the target node.

For an `If Condition` branch use:

``` text
Connect TRUE → Shift-click Target
```

or:

``` text
Connect FALSE → Shift-click Target
```

Then hold **Shift** and click the target node.

To remove connections:

``` text
Remove Connections
```

This removes connections involving the selected node.

------------------------------------------------------------------------

# 11. Validate Before Running

In the Workflow Designer click:

``` text
✓ Validate Workflow
```

Validation checks include:

-   a Start node exists
-   workflow contains nodes
-   file operations use the expected `Source => Destination` syntax
-   connections reference existing nodes

A successful validation reports:

``` text
✓ Workflow is valid and ready for Dry Run.
```

Always validate before testing a new workflow.

------------------------------------------------------------------------

# 12. Dry Run

**Dry Run is the recommended first execution mode.**

Dry Run follows the workflow but does not perform the normal
file/process/HTTP actions.

Instead, the execution monitor reports what would happen.

To run a Dry Run:

1.  Open the automation.
2.  Click **Open Designer** if necessary.
3.  Validate the workflow.
4.  Click:

``` text
◇ Dry Run
```

or use **◇ Dry Run** in the Automation Library.

The application opens:

**Live Execution Monitor**

You can see execution messages and completed steps.

A successful Dry Run is recorded in Run History.

------------------------------------------------------------------------

# 13. Real Execution

Only use a real run after the workflow has been validated and Dry Run
has produced the expected result.

From the Automation Library:

``` text
▶ Run
```

or from the Workflow Designer:

``` text
▶ Run Workflow
```

A confirmation dialog is displayed before a live run.

The execution monitor shows:

-   workflow name
-   DRY RUN or LIVE RUN
-   step messages
-   completion
-   failure information
-   Stop control

------------------------------------------------------------------------

# 14. Stopping a Workflow

During execution use:

``` text
⏹ Stop
```

The engine uses cancellation tokens and records cancellation in the
execution monitor.

------------------------------------------------------------------------

# 15. Supported Actions

JASS Automation Hub v1.2 supports these action types:

1.  Start
2.  Create Folder
3.  Move File
4.  Copy File
5.  Rename File
6.  Delete File
7.  Write Log
8.  Run Process
9.  HTTP GET
10. Delay
11. Set Variable
12. If Condition
13. For Each File
14. Loop End

------------------------------------------------------------------------

# 16. File Operations

## Copy File

Parameter format:

``` text
Source => Destination
```

Example:

``` text
%USERPROFILE%\Desktop\Input\Test1.txt => %USERPROFILE%\Desktop\Output\Test1.txt
```

## Move File

Same syntax:

``` text
Source => Destination
```

## Rename File

Same syntax:

``` text
Source => Destination
```

## Create Folder

Parameter is the folder path:

``` text
%USERPROFILE%\Desktop\MyAutomationFolder
```

## Delete File

Parameter is the complete file path:

``` text
%USERPROFILE%\Desktop\MyAutomationFolder\Test.txt
```

### Safety

For destructive actions such as Move and Delete, test on a dedicated
test folder first.

------------------------------------------------------------------------

# 17. Variables

Variables are managed through:

``` text
Variables
```

Use:

``` text
{VariableName}
```

inside action parameters.

Example variable:

``` text
Name: BackupFolder
Value: %USERPROFILE%\Desktop\Backup
```

Then use:

``` text
{BackupFolder}
```

in another action.

Environment variables are also expanded.

Examples:

``` text
%USERPROFILE%
%TEMP%
%APPDATA%
```

The special placeholder:

``` text
{AppData}
```

expands to the JASS Automation Hub application data directory.

------------------------------------------------------------------------

# 18. Set Variable

Use:

``` text
Variable => Value
```

Example:

``` text
BackupFolder => %USERPROFILE%\Desktop\Backup
```

The value becomes available during the current workflow run.

Variables are case-insensitive during expansion.

------------------------------------------------------------------------

# 19. If Condition

The Condition field supports comparisons such as:

``` text
Value1 == Value2
Value1 != Value2
10 > 5
10 < 20
10 >= 10
10 <= 20
```

Examples:

``` text
Status == Ready
```

``` text
Count >= 10
```

``` text
Mode != Test
```

Connect the node's TRUE and FALSE branches separately:

``` text
If Condition
     |
     +---- TRUE ----> Action A
     |
     +---- FALSE ---> Action B
```

------------------------------------------------------------------------

# 20. For Each File

Set the parameter to a directory:

``` text
%USERPROFILE%\Desktop\Input
```

During execution the engine enumerates the files in that directory and
updates:

``` text
{CurrentFile}
```

for each file.

The execution monitor reports the current file name.

------------------------------------------------------------------------

# 21. Loop End

`Loop End` is available as a workflow node for loop-oriented workflow
structures.

The engine also contains loop-safety protection.

A runaway workflow is stopped if the execution safety limit is reached.

------------------------------------------------------------------------

# 22. Delay

Delay uses **milliseconds**.

Examples:

``` text
1000
```

means:

``` text
1 second
```

``` text
5000
```

means:

``` text
5 seconds
```

------------------------------------------------------------------------

# 23. Retry Count

Each workflow node can have a Retry Count.

Example:

``` text
Retry Count: 2
```

The engine can attempt the action up to three times:

``` text
Initial attempt
Retry 1
Retry 2
```

A short delay is inserted between failed attempts.

------------------------------------------------------------------------

# 24. Run Process

The `Run Process` action starts a Windows process using the configured
parameter.

Example:

``` text
notepad.exe
```

Use caution with programs that modify files, install software, or
require elevated permissions.

------------------------------------------------------------------------

# 25. HTTP GET

`HTTP GET` performs an HTTP GET request.

Example:

``` text
https://example.com
```

A successful HTTP response is considered successful.

Use Dry Run when designing a workflow, but note that Dry Run does not
perform the real HTTP request.

------------------------------------------------------------------------

# 26. Write Log

The `Write Log` action appends a message to:

``` text
%LOCALAPPDATA%\JASS\AutomationHub\automation.log
```

Example:

``` text
Backup workflow completed
```

Open the application's:

**Logs**

page to inspect the local execution log.

------------------------------------------------------------------------

# 27. Scheduling

Each automation has a Schedule setting.

Available schedules:

``` text
Manual
At Startup
Every 15 minutes
Hourly
Daily
Weekly
```

## Important scheduler behavior

The scheduler runs **inside the JASS Automation Hub application**.

Therefore:

-   JASS Automation Hub must be running.
-   Closing the application stops the in-application scheduler.
-   This version does not install a Windows Task Scheduler job.
-   Future versions can add true background Windows scheduling.

For initial testing, use:

``` text
Manual
```

------------------------------------------------------------------------

# 28. Dry Run Default

Automations have a Dry Run Default setting.

This is used when the internal scheduler triggers an automation.

For safety, workflows should normally be tested with Dry Run enabled
before allowing scheduled live execution.

------------------------------------------------------------------------

# 29. Run History

Open:

**Runs & History**

Run records are stored locally in SQLite.

Recorded information includes:

-   Automation name
-   Start time
-   Finish time
-   Success status
-   Dry Run status
-   Completed steps
-   Message/error information

The Dashboard's Runs counter reflects recorded runs.

------------------------------------------------------------------------

# 30. Live Execution Monitor

During a run the application displays:

**Live Execution Monitor**

It shows:

-   automation name
-   execution mode
-   action messages
-   dry-run descriptions
-   variable changes
-   condition results
-   errors
-   completion state

The Stop button can cancel an active execution.

------------------------------------------------------------------------

# 31. Templates

The Dashboard provides quick-start templates including:

-   File Organizer
-   Backup Workflow
-   Website Check
-   Log & Report

Templates can be used as starting points for new automations.

------------------------------------------------------------------------

# 32. Import / Export

Automations can be exported as:

``` text
.jassautomation
```

or JSON-compatible workflow data.

Use:

**Export**

to save an automation.

Use:

**Import**

to load an exported automation.

This is useful for:

-   backup
-   moving workflows between installations
-   versioning workflow definitions
-   sharing workflow definitions

------------------------------------------------------------------------

# 33. SQLite Data Storage

The application stores its local database at:

``` text
%LOCALAPPDATA%\JASS\AutomationHub\automation-hub.db
```

The application also maintains:

``` text
%LOCALAPPDATA%\JASS\AutomationHub\automation.log
```

The application creates its data directory as required.

The data is local to the Windows user account.

------------------------------------------------------------------------

# 34. Recommended Safe Test

Before creating a real automation, create:

``` text
%USERPROFILE%\Desktop\JASS-Automation-Test
```

Then:

``` text
JASS-Automation-Test/
├── Input/
│   ├── Test1.txt
│   └── Test2.txt
└── Output/
```

Create a Copy File workflow:

``` text
Start
  |
  v
Copy File
```

Copy File parameter:

``` text
%USERPROFILE%\Desktop\JASS-Automation-Test\Input\Test1.txt => %USERPROFILE%\Desktop\JASS-Automation-Test\Output\Test1.txt
```

Then:

1.  Save workflow
2.  Validate
3.  Dry Run
4.  Inspect Live Execution Monitor
5.  Check Runs & History
6.  Only then perform a Live Run

Expected result after Live Run:

``` text
JASS-Automation-Test/
├── Input/
│   ├── Test1.txt
│   └── Test2.txt
└── Output/
    └── Test1.txt
```

------------------------------------------------------------------------

# 35. Recommended v1.2 Test Sequence

Use this sequence when validating a fresh installation:

``` text
1. Launch application
2. Open Automations
3. Open a workflow
4. Click workflow nodes
5. Edit a node parameter
6. Save workflow
7. Validate workflow
8. Perform Dry Run
9. Inspect Live Execution Monitor
10. Check Runs & History
11. Check Logs
12. Perform Live Run on a safe test folder
13. Verify output
14. Test variables
15. Test If Condition
16. Test For Each File
17. Test Retry Count
18. Test Stop
19. Test template creation
20. Test Export
21. Test Import
22. Close application
23. Restart application
24. Verify persistence
```

------------------------------------------------------------------------

# 36. Troubleshooting

## `MSB1003: Specify a project or solution file`

You are probably in the wrong directory.

Find the solution:

``` powershell
Get-ChildItem -Recurse -Filter "*.sln"
```

Find the project:

``` powershell
Get-ChildItem -Recurse -Filter "*.csproj"
```

Then change to the directory containing the solution/project.

------------------------------------------------------------------------

## `MSB1009: Project file does not exist`

Check the current directory:

``` powershell
Get-Location
```

If you are already inside:

``` text
JASS.AutomationHub
```

simply use:

``` powershell
dotnet run
```

------------------------------------------------------------------------

## Build errors after changing source

Run:

``` powershell
dotnet clean
dotnet restore
dotnet build -c Release
```

------------------------------------------------------------------------

## Workflow does not execute

Use this order:

``` text
Validate
   ↓
Dry Run
   ↓
Inspect Monitor
   ↓
Live Run
```

Check:

-   node Enabled state
-   node parameters
-   connections
-   conditions
-   file paths
-   permissions

------------------------------------------------------------------------

## File action fails

Check the path and parameter syntax.

Copy/Move/Rename require:

``` text
Source => Destination
```

Do not use:

``` text
Source -> Destination
```

or:

``` text
Source = Destination
```

------------------------------------------------------------------------

## Scheduled automation does not run

Remember:

> The in-application scheduler only operates while JASS Automation Hub
> is running.

Check:

-   automation is Enabled
-   Schedule is not Manual
-   application is open
-   automation has the expected Dry Run Default

------------------------------------------------------------------------

# 37. Architecture

JASS Automation Hub uses a simple local-first architecture:

``` text
WPF UI
  |
  +-- Automation Library
  |
  +-- Visual Workflow Designer
  |
  +-- Execution Monitor
  |
  +-- Runs & History
  |
  +-- Templates
  |
  +-- Logs
  |
  v
Workflow Engine
  |
  +-- Actions
  +-- Variables
  +-- Conditions
  +-- Loops
  +-- Retry
  +-- Cancellation
  |
  v
SQLite Persistence
```

Main services:

``` text
Services/AutomationStore.cs
Services/WorkflowEngine.cs
```

Main models:

``` text
Models/WorkflowModels.cs
```

------------------------------------------------------------------------

# 38. Privacy / Local-First Design

JASS Automation Hub is designed around local operation.

Workflow definitions, variables, run history, and logs are stored
locally.

The application does not require a cloud account for its core automation
features.

HTTP GET actions obviously access the requested network resource when a
workflow containing that action is executed.

------------------------------------------------------------------------

# 39. Safety

Automation can modify files and launch processes.

Always:

-   test with a dedicated folder
-   use Dry Run first
-   validate workflows
-   verify paths
-   avoid destructive actions until tested
-   keep backups of important data
-   review imported workflows before executing them

Do not test destructive workflows against system directories or
irreplaceable personal data.

------------------------------------------------------------------------

# 40. GitHub Release Workflow

After final verification, initialize or update the repository:

``` powershell
git status
git add .
git commit -m "Release JASS Automation Hub v1.2"
git tag -a v1.2.0 -m "JASS Automation Hub v1.2"
git push origin main
git push origin v1.2.0
```

Before committing, verify:

``` powershell
git status
```

The working tree should contain only the intended project files.

Recommended repository name:

``` text
JASS-Automation-Hub
```

Recommended release tag:

``` text
v1.2.0
```

------------------------------------------------------------------------

# 41. Suggested GitHub Repository Description

``` text
JASS Automation Hub — Local-first Windows workflow automation platform built with C#/.NET 8 WPF, visual workflows, variables, conditions, loops, scheduling, dry runs, execution monitoring and SQLite persistence.
```

------------------------------------------------------------------------

# 42. Version History

## v1.2.0

Major workflow-engine milestone:

-   visual workflow designer
-   selectable/draggable workflow nodes
-   explicit graph connections
-   TRUE/FALSE branching
-   node inspector
-   variables
-   variable expansion
-   Set Variable
-   If Condition
-   For Each File
-   Loop End
-   retry handling
-   cancellation/Stop
-   Dry Run
-   Live Execution Monitor
-   SQLite run history
-   workflow validation
-   scheduling options
-   templates
-   import/export
-   improved dark JASS UI
-   execution/history UI readability fixes

## v1.1.0

Foundation release containing:

-   WPF dashboard
-   automation library
-   SQLite persistence
-   workflow designer foundation
-   action palette
-   node inspector
-   local logging
-   Dry Run foundation
-   templates
-   run history foundation

------------------------------------------------------------------------

# 43. Final v1.2 Baseline

This release should be treated as the **frozen v1.2 baseline**.

Future development should preserve the working v1.2 behavior and add new
capabilities incrementally.

Potential future work may include:

-   Windows Task Scheduler integration
-   richer condition expressions
-   more file actions
-   folder iteration improvements
-   CSV/JSON/SQLite data actions
-   document/PDF actions
-   visual connection lines
-   richer branching
-   reusable sub-workflows
-   credentials/secrets management
-   plugin architecture
-   execution reports
-   workflow versioning
-   packaged installer

These are future enhancements and are **not required for the v1.2
baseline**.

------------------------------------------------------------------------

## License

Add the project's chosen license here before publishing if a specific
open-source license is intended.

------------------------------------------------------------------------

**JASS Automation Hub v1.2 --- Final Local-First Automation Baseline**
