using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using JASS.AutomationHub.Models;
using JASS.AutomationHub.Services;

namespace JASS.AutomationHub;

public partial class MainWindow : Window
{
    private readonly AutomationStore _store = new();
    private readonly WorkflowEngine _engine = new();
    private readonly ObservableCollection<Automation> _automations = [];
    private CancellationTokenSource? _runCts;
    private Automation? _selected;
    private WorkflowNode? _selectedNode;
    private Canvas? _designerCanvas;
    private WorkflowNode? _dragNode;
    private Point _dragOffset;
    private ContentControl? _designerInspectorHost;
    private string _pendingConnectionLabel = "";
    private readonly System.Windows.Threading.DispatcherTimer _schedulerTimer;
    private readonly Dictionary<Guid, DateTime> _lastScheduled = [];

    public MainWindow()
    {
        InitializeComponent();
        LoadData();
        if (_automations.Count == 0) SeedTemplates();

        _schedulerTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _schedulerTimer.Tick += SchedulerTimer_Tick;
        _schedulerTimer.Start();

        ShowDashboard();
    }

    private void LoadData()
    {
        _automations.Clear();
        foreach (var a in _store.Load()) _automations.Add(a);
    }

    private void SeedTemplates()
    {
        _automations.Add(CreateTemplate("Downloads Organizer", "Starter file organization workflow."));
        _automations.Add(CreateTemplate("Daily Backup", "Starter backup workflow — configure paths before running."));
        _automations.Add(CreateTemplate("Website Check", "HTTP check followed by local logging."));
        _automations.Add(CreateTemplate("PDF Processing", "Starter document workflow."));
        Save();
    }

    private Automation CreateTemplate(string name, string description)
    {
        var a = new Automation { Name = name, Description = description };
        var start = new WorkflowNode { Kind = ActionKind.Start, Title = "Start", X = 80, Y = 70 };
        var log = new WorkflowNode { Kind = ActionKind.WriteLog, Title = "Write Log", Parameter = $"{name} started", X = 80, Y = 220 };
        a.Nodes.Add(start); a.Nodes.Add(log);
        a.Connections.Add(new WorkflowConnection { FromNodeId = start.Id, ToNodeId = log.Id });
        a.Variables.Add(new WorkflowVariable { Name = "Today", Value = DateTime.Today.ToString("yyyy-MM-dd") });
        return a;
    }

    private void Save() => _store.Save(_automations);
    private Brush Muted() => (Brush)FindResource("MutedBrush");

    private Border Panel(double marginRight = 0) => new()
    {
        Background = (Brush)FindResource("PanelBrush"),
        CornerRadius = new CornerRadius(14),
        Padding = new Thickness(20),
        Margin = new Thickness(0, 0, marginRight, 0)
    };

    private Border Card(string title, string value, string subtitle) => new()
    {
        Background = (Brush)FindResource("PanelBrush"),
        CornerRadius = new CornerRadius(14),
        Margin = new Thickness(0, 0, 10, 0),
        Padding = new Thickness(18),
        Child = new StackPanel
        {
            Children =
            {
                new TextBlock { Text=title, Foreground=Muted(), FontSize=11 },
                new TextBlock { Text=value, FontSize=28, FontWeight=FontWeights.Bold, Margin=new Thickness(0,7,0,2) },
                new TextBlock { Text=subtitle, Foreground=Muted() }
            }
        }
    };

    private void ShowDashboard()
    {
        PageTitle.Text = "Automation Dashboard";
        PageSubtitle.Text = "Build, connect, validate and run your Windows automations.";
        var panel = new StackPanel();
        var cards = new UniformGrid { Columns = 4, Margin = new Thickness(0,0,0,22) };
        cards.Children.Add(Card("AUTOMATIONS", _automations.Count.ToString(), "Saved workflows"));
        cards.Children.Add(Card("ENABLED", _automations.Count(a => a.Enabled).ToString(), "Ready to run"));
        cards.Children.Add(Card("RUNS", _automations.Sum(a => a.RunCount).ToString(), "Recorded runs"));
        cards.Children.Add(Card("ENGINE", _runCts == null ? "Idle" : "Running", "Live execution"));
        panel.Children.Add(cards);

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var recent = Panel(10);
        var rp = new StackPanel();
        rp.Children.Add(new TextBlock { Text="Your Automations", FontSize=18, FontWeight=FontWeights.SemiBold });
        rp.Children.Add(new TextBlock { Text="Open a workflow in the visual designer.", Foreground=Muted(), Margin=new Thickness(0,4,0,12) });
        foreach (var a in _automations.Take(8))
        {
            var b = new Button { Content=$"{(a.Enabled ? "●" : "○")}   {a.Name}", HorizontalContentAlignment=HorizontalAlignment.Left, Background=Brushes.Transparent, BorderBrush=Brushes.Transparent };
            b.Click += (_,_)=>OpenDesigner(a);
            rp.Children.Add(b);
        }
        recent.Child=rp; Grid.SetColumn(recent,0); row.Children.Add(recent);

        var quick = Panel();
        var qp = new StackPanel();
        qp.Children.Add(new TextBlock { Text="Quick Start", FontSize=18, FontWeight=FontWeights.SemiBold });
        qp.Children.Add(new TextBlock { Text="Start with a reusable template.", Foreground=Muted(), Margin=new Thickness(0,4,0,14) });
        foreach(var text in new[]{"📁  File Organizer","💾  Backup Workflow","🌐  Website Check","📝  Log & Report"})
        {
            var b=new Button{Content=text,HorizontalContentAlignment=HorizontalAlignment.Left};
            b.Click+=(_,_)=>NewAutomation(text.Substring(3));
            qp.Children.Add(b);
        }
        quick.Child=qp;Grid.SetColumn(quick,1);row.Children.Add(quick);
        panel.Children.Add(row);
        MainContent.Content=panel;
    }

    private void ShowAutomations()
    {
        PageTitle.Text="Automation Library";
        PageSubtitle.Text="Search, duplicate, validate, export and run workflows.";
        var root=new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(300)});
        root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});

        var left=new DockPanel();
        var search=new TextBox{Margin=new Thickness(0,0,0,10),ToolTip="Search automations"};
        DockPanel.SetDock(search,Dock.Top);left.Children.Add(search);
        var list=new ListBox{ItemsSource=_automations,DisplayMemberPath="Name"};
        list.SelectionChanged+=(_,_)=>{if(list.SelectedItem is Automation a)_selected=a;};
        left.Children.Add(list);
        search.TextChanged+=(_,_)=>{var q=search.Text.Trim();list.ItemsSource=string.IsNullOrWhiteSpace(q)?_automations:_automations.Where(a=>a.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||a.Description.Contains(q,StringComparison.OrdinalIgnoreCase)).ToList();};
        Grid.SetColumn(left,0);root.Children.Add(new Border{Background=(Brush)FindResource("PanelBrush"),CornerRadius=new CornerRadius(14),Padding=new Thickness(12),Child=left});

        var details=new StackPanel{Margin=new Thickness(20,0,0,0)};
        details.Children.Add(new TextBlock{Text="Automation Library",FontSize=22,FontWeight=FontWeights.SemiBold});
        details.Children.Add(new TextBlock{Text="Manage your automation definitions.",Foreground=Muted(),Margin=new Thickness(0,5,0,15)});
        var buttons=new WrapPanel();
        AddButton(buttons,"Open Designer",()=>{if(_selected!=null)OpenDesigner(_selected);},true);
        AddButton(buttons,"▶ Run",async()=>{if(_selected!=null)await RunAutomation(_selected);});
        AddButton(buttons,"◇ Dry Run",async()=>{if(_selected!=null)await RunAutomation(_selected,true);});
        AddButton(buttons,"✓ Validate",()=>{if(_selected!=null)ShowValidation(_selected);});
        AddButton(buttons,"Duplicate",()=>{if(_selected!=null)Duplicate(_selected);});
        AddButton(buttons,"Export",()=>{if(_selected!=null)ExportAutomation(_selected);});
        AddButton(buttons,"Import",ImportAutomation);
        AddButton(buttons,"Delete",DeleteSelected);
        details.Children.Add(buttons);
        root.Children.Add(new Border{Background=(Brush)FindResource("PanelBrush"),CornerRadius=new CornerRadius(14),Padding=new Thickness(20),Margin=new Thickness(12,0,0,0),Child=details});
        MainContent.Content=root;
    }

    private void AddButton(Panel panel,string text,Action action,bool primary=false)
    {
        var b=new Button{Content=text,Style=primary?(Style)FindResource("PrimaryButton"):(Style)FindResource(typeof(Button))};
        b.Click+=(_,_)=>action();
        panel.Children.Add(b);
    }

    private void OpenDesigner(Automation automation)
    {
        _selected=automation;_selectedNode=null;_pendingConnectionLabel="";
        PageTitle.Text=automation.Name;
        PageSubtitle.Text="Visual Workflow Designer • click a node to configure it; drag to move it.";
        var root=new Grid();
        root.Width = 1200;
        root.MinWidth = 1200;
        root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(190)});
        root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
        root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(300)});

        var palette=new StackPanel{Background=(Brush)FindResource("PanelBrush")};
        palette.Children.Add(new TextBlock{Text="ACTION PALETTE",FontWeight=FontWeights.Bold,Margin=new Thickness(15,15,15,8)});
        foreach(var kind in Enum.GetValues<ActionKind>())
        {
            var b=new Button{Content=Friendly(kind),HorizontalContentAlignment=HorizontalAlignment.Left};
            b.Click+=(_,_)=>
            {
                if(kind==ActionKind.Start)
                {
                    var existingStart=automation.Nodes.FirstOrDefault(n=>n.Kind==ActionKind.Start);
                    if(existingStart!=null)
                    {
                        _selectedNode=existingStart;
                        _designerInspectorHost?.SetValue(ContentControl.ContentProperty, BuildInspector(automation));
                        return;
                    }
                }
                automation.Nodes.Add(NewNode(kind,automation.Nodes.Count));
                Save();
                OpenDesigner(automation);
            };
            palette.Children.Add(b);
        }
        Grid.SetColumn(palette,0);root.Children.Add(palette);

        _designerCanvas=new Canvas{Background=new SolidColorBrush(Color.FromRgb(8,13,26)),Width=710,Height=Math.Max(700,automation.Nodes.Count*125)};
        DrawDesigner(automation);
        var scroll=new ScrollViewer{HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(0),Content=_designerCanvas};
        Grid.SetColumn(scroll,1);root.Children.Add(scroll);

        _designerInspectorHost = new ContentControl
        {
            Content = BuildInspector(automation)
        };
        Grid.SetColumn(_designerInspectorHost,2);root.Children.Add(_designerInspectorHost);
        MainContent.Content=root;
    }

    private void DrawDesigner(Automation automation)
    {
        if(_designerCanvas==null)return;
        _designerCanvas.Children.Clear();

        foreach(var link in automation.Connections)
        {
            var a=automation.Nodes.FirstOrDefault(n=>n.Id==link.FromNodeId);
            var b=automation.Nodes.FirstOrDefault(n=>n.Id==link.ToNodeId);
            if(a==null||b==null)continue;
            var line=new Line{X1=a.X+95,Y1=a.Y+82,X2=b.X+95,Y2=b.Y,Stroke=(Brush)FindResource("AccentBrush"),StrokeThickness=2,Opacity=.75};
            _designerCanvas.Children.Add(line);
            if(!string.IsNullOrWhiteSpace(link.Label))
                _designerCanvas.Children.Add(new TextBlock{Text=link.Label,Foreground=(Brush)FindResource("Accent2Brush"),FontSize=10});
        }

        foreach(var node in automation.Nodes)
        {
            var card=NodeCard(automation,node);
            Canvas.SetLeft(card,Math.Max(10,node.X));Canvas.SetTop(card,Math.Max(10,node.Y));
            _designerCanvas.Children.Add(card);
        }
    }

    private Border NodeCard(Automation automation, WorkflowNode node)
    {
        var border = new Border
        {
            Width = 190,
            MinHeight = 86,
            Background = node == _selectedNode
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("Panel2Brush"),
            BorderBrush = (Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Cursor = Cursors.Hand,
            Focusable = true,
            Tag = node.Id
        };

        var p = new StackPanel { IsHitTestVisible = false };
        p.Children.Add(new TextBlock
        {
            Text = Friendly(node.Kind).ToUpperInvariant(),
            FontSize = 10,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold
        });
        p.Children.Add(new TextBlock
        {
            Text = node.Title,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 4, 0, 3)
        });
        p.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(node.Parameter) ? "Click to configure" : node.Parameter,
            Foreground = Brushes.White,
            Opacity = .72,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11
        });
        if (node.RetryCount > 0)
            p.Children.Add(new TextBlock
            {
                Text = $"Retry: {node.RetryCount}",
                Foreground = (Brush)FindResource("Accent2Brush"),
                FontSize = 10
            });

        border.Child = p;

        // Use PreviewMouse events so clicks are captured reliably even when
        // the node contains child controls or is hosted inside a ScrollViewer.
        border.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) &&
                _selectedNode != null && _selectedNode.Id != node.Id)
            {
                var label = _pendingConnectionLabel;
                if (!automation.Connections.Any(c =>
                    c.FromNodeId == _selectedNode.Id &&
                    c.ToNodeId == node.Id &&
                    c.Label == label))
                {
                    automation.Connections.Add(new WorkflowConnection
                    {
                        FromNodeId = _selectedNode.Id,
                        ToNodeId = node.Id,
                        Label = label
                    });
                }

                _pendingConnectionLabel = "";
                Save();
                _selectedNode = node;
                OpenDesigner(automation);
                e.Handled = true;
                return;
            }

            _selectedNode = node;
            border.Focus();
            UpdateNodeSelectionVisuals(automation);
            _designerInspectorHost?.SetValue(
                ContentControl.ContentProperty,
                BuildInspector(automation));

            if (_designerCanvas != null)
            {
                _dragNode = node;
                var pos = e.GetPosition(_designerCanvas);
                _dragOffset = new Point(pos.X - node.X, pos.Y - node.Y);
                border.CaptureMouse();
            }

            e.Handled = true;
        };

        border.PreviewMouseMove += (_, e) =>
        {
            if (_dragNode == node &&
                e.LeftButton == MouseButtonState.Pressed &&
                _designerCanvas != null)
            {
                var pos = e.GetPosition(_designerCanvas);
                node.X = Math.Max(10, pos.X - _dragOffset.X);
                node.Y = Math.Max(10, pos.Y - _dragOffset.Y);
                Canvas.SetLeft(border, node.X);
                Canvas.SetTop(border, node.Y);
            }
        };

        border.PreviewMouseLeftButtonUp += (_, e) =>
        {
            if (_dragNode == node)
            {
                _dragNode = null;
                border.ReleaseMouseCapture();
                Save();
            }
            e.Handled = true;
        };

        return border;
    }

    private void UpdateNodeSelectionVisuals(Automation automation)
    {
        if (_designerCanvas == null) return;

        foreach (var child in _designerCanvas.Children.OfType<Border>())
        {
            if (child.Tag is Guid id)
            {
                child.Background = _selectedNode != null && id == _selectedNode.Id
                    ? (Brush)FindResource("AccentBrush")
                    : (Brush)FindResource("Panel2Brush");
            }
        }
    }

    private StackPanel BuildInspector(Automation a)
    {
        var inspector=new StackPanel{Background=(Brush)FindResource("PanelBrush"),Margin=new Thickness(10,0,0,0)};
        inspector.Children.Add(new TextBlock{Text="INSPECTOR",FontWeight=FontWeights.Bold,Margin=new Thickness(16,16,16,10)});
        inspector.Children.Add(new TextBlock{Text="Automation Name",Foreground=Muted()});
        var name=new TextBox{Text=a.Name};name.TextChanged+=(_,_)=>a.Name=name.Text;inspector.Children.Add(name);

        inspector.Children.Add(new TextBlock{Text="Schedule",Foreground=Muted(),Margin=new Thickness(0,12,0,0)});
        var schedule=new ComboBox{ItemsSource=new[]{"Manual","At Startup","Every 15 minutes","Hourly","Daily","Weekly"},SelectedItem=a.Schedule};schedule.SelectionChanged+=(_,_)=>{if(schedule.SelectedItem!=null){a.Schedule=schedule.SelectedItem.ToString()!;Save();}};inspector.Children.Add(schedule);

        inspector.Children.Add(new TextBlock{Text="Workflow Actions",Foreground=Muted(),Margin=new Thickness(0,14,0,6)});
        AddButton(inspector,"Connect Selected → Shift-click Target",()=>ConnectNodes(a));
        AddButton(inspector,"Connect TRUE → Shift-click Target",()=>ConnectNodes(a,"True"));
        AddButton(inspector,"Connect FALSE → Shift-click Target",()=>ConnectNodes(a,"False"));
        AddButton(inspector,"Remove Connections",()=>RemoveConnections(a));
        AddButton(inspector,"✓ Validate Workflow",()=>ShowValidation(a));

        inspector.Children.Add(new TextBlock{Text="Selected Node",Foreground=Muted(),Margin=new Thickness(0,14,0,6)});
        if(_selectedNode!=null)
        {
            var title=new TextBox{Text=_selectedNode.Title};title.TextChanged+=(_,_)=>_selectedNode.Title=title.Text;inspector.Children.Add(title);
            inspector.Children.Add(new TextBlock{Text="Parameter",Foreground=Muted(),Margin=new Thickness(0,8,0,0)});
            var param=new TextBox{Text=_selectedNode.Parameter,Height=65,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap};param.TextChanged+=(_,_)=>_selectedNode.Parameter=param.Text;inspector.Children.Add(param);
            inspector.Children.Add(new TextBlock{Text="Condition",Foreground=Muted(),Margin=new Thickness(0,8,0,0)});
            var cond=new TextBox{Text=_selectedNode.Condition};cond.TextChanged+=(_,_)=>_selectedNode.Condition=cond.Text;inspector.Children.Add(cond);
            inspector.Children.Add(new TextBlock{Text="Retry Count",Foreground=Muted(),Margin=new Thickness(0,8,0,0)});
            var retry=new TextBox{Text=_selectedNode.RetryCount.ToString()};retry.TextChanged+=(_,_)=>{if(int.TryParse(retry.Text,out var n))_selectedNode.RetryCount=Math.Max(0,Math.Min(5,n));};inspector.Children.Add(retry);
            var enabled=new CheckBox{Content="Enabled",IsChecked=_selectedNode.Enabled,Margin=new Thickness(5,8,5,5),Foreground=Muted()};enabled.Checked+=(_,_)=>_selectedNode.Enabled=true;enabled.Unchecked+=(_,_)=>_selectedNode.Enabled=false;inspector.Children.Add(enabled);
            AddButton(inspector,"Delete Node",()=>{a.Nodes.Remove(_selectedNode);a.Connections.RemoveAll(c=>c.FromNodeId==_selectedNode.Id||c.ToNodeId==_selectedNode.Id);_selectedNode=null;Save();OpenDesigner(a);});
        }
        else inspector.Children.Add(new TextBlock{Text="Click a node to select/configure it. Drag to move it. For connections, select the source, then Shift-click the target.",Foreground=Muted(),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});

        AddButton(inspector,"Variables",()=>ShowVariables(a));
        AddButton(inspector,"Import / Export",()=>ShowAutomations());
        AddButton(inspector,"Save Workflow",()=>{Save();MessageBox.Show("Workflow saved.","JASS Automation Hub",MessageBoxButton.OK,MessageBoxImage.Information);},true);
        AddButton(inspector,"◇ Dry Run",async()=>await RunAutomation(a,true));
        AddButton(inspector,"▶ Run Workflow",async()=>await RunAutomation(a));
        AddButton(inspector,"⏹ Stop",StopRun);
        return inspector;
    }

    private void ConnectNodes(Automation a,string label="")
    {
        if(_selectedNode==null)
        {
            MessageBox.Show("Select the source node first.","Connect Nodes",MessageBoxButton.OK,MessageBoxImage.Information);
            return;
        }

        _pendingConnectionLabel=label;
        MessageBox.Show(
            string.IsNullOrWhiteSpace(label)
                ? "Now hold Shift and click the target node."
                : $"Now hold Shift and click the target node to create the {label} branch.",
            "Connect Nodes",MessageBoxButton.OK,MessageBoxImage.Information);
    }

    private void RemoveConnections(Automation a)
    {
        if(_selectedNode==null)return;
        a.Connections.RemoveAll(c=>c.FromNodeId==_selectedNode.Id||c.ToNodeId==_selectedNode.Id);
        Save();OpenDesigner(a);
    }

    private void ShowValidation(Automation a)
    {
        var issues=new List<string>();
        if(a.Nodes.Count==0)issues.Add("No nodes.");
        if(!a.Nodes.Any(n=>n.Kind==ActionKind.Start))issues.Add("No Start node.");
        foreach(var n in a.Nodes.Where(n=>n.Kind==ActionKind.MoveFile||n.Kind==ActionKind.CopyFile||n.Kind==ActionKind.RenameFile))
            if(!n.Parameter.Contains("=>"))issues.Add($"{n.Title}: parameter should use Source => Destination.");
        foreach(var c in a.Connections)
        {
            if(!a.Nodes.Any(n=>n.Id==c.FromNodeId)||!a.Nodes.Any(n=>n.Id==c.ToNodeId))issues.Add("A connection references a missing node.");
        }
        var message=issues.Count==0?"✓ Workflow is valid and ready for Dry Run.":string.Join(Environment.NewLine,issues.Select(x=>"• "+x));
        MessageBox.Show(message,"Workflow Validation",MessageBoxButton.OK,issues.Count==0?MessageBoxImage.Information:MessageBoxImage.Warning);
    }

    private void ShowVariables(Automation a)
    {
        PageTitle.Text="Variables & Parameters";PageSubtitle.Text="Reusable runtime values. Use {VariableName} in action parameters.";
        var panel=new DockPanel();
        var top=new WrapPanel();
        AddButton(top,"+ Add Variable",()=>{a.Variables.Add(new WorkflowVariable{Name="NewVariable",Value=""});Save();ShowVariables(a);},true);
        AddButton(top,"← Back",()=>OpenDesigner(a));
        DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);
        var grid=new DataGrid{AutoGenerateColumns=false,CanUserAddRows=false,Background=(Brush)FindResource("PanelBrush"),Foreground=Brushes.White,BorderBrush=(Brush)FindResource("BorderBrush")};
        grid.Columns.Add(new DataGridTextColumn{Header="Name",Binding=new System.Windows.Data.Binding("Name")});
        grid.Columns.Add(new DataGridTextColumn{Header="Value",Binding=new System.Windows.Data.Binding("Value")});
        grid.ItemsSource=a.Variables;panel.Children.Add(grid);
        MainContent.Content=panel;
    }

    private void ShowHistory()
    {
        PageTitle.Text="Runs & History";PageSubtitle.Text="Live execution records stored locally in SQLite.";
        var root=new DockPanel();
        var top=new WrapPanel();
        AddButton(top,"↻ Refresh",ShowHistory);
        AddButton(top,"Clear History",()=>{if(MessageBox.Show("Clear run history?","Confirm",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes){/* intentionally retained for future DB cleanup */ MessageBox.Show("History cleanup is reserved for the database maintenance layer.","JASS Automation Hub");}});
        DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
        var grid=new DataGrid{AutoGenerateColumns=false,IsReadOnly=true,Background=(Brush)FindResource("PanelBrush"),Foreground=Brushes.White,BorderBrush=(Brush)FindResource("BorderBrush")};
        grid.Columns.Add(new DataGridTextColumn{Header="Started",Binding=new System.Windows.Data.Binding("Started")});
        grid.Columns.Add(new DataGridTextColumn{Header="Automation",Binding=new System.Windows.Data.Binding("AutomationName")});
        grid.Columns.Add(new DataGridTextColumn{Header="Success",Binding=new System.Windows.Data.Binding("Success")});
        grid.Columns.Add(new DataGridTextColumn{Header="Dry Run",Binding=new System.Windows.Data.Binding("DryRun")});
        grid.Columns.Add(new DataGridTextColumn{Header="Steps",Binding=new System.Windows.Data.Binding("CompletedSteps")});
        grid.Columns.Add(new DataGridTextColumn{Header="Message",Binding=new System.Windows.Data.Binding("Message")});
        grid.ItemsSource=_store.LoadRuns();root.Children.Add(grid);MainContent.Content=root;
    }

    private async Task RunAutomation(Automation a,bool dryRun=false)
    {
        if(!dryRun&&MessageBox.Show("Run this automation now?","JASS Automation Hub",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
        ShowRunMonitor(a,dryRun);
        _runCts=new CancellationTokenSource();
        var started=DateTime.Now;var lines=new ObservableCollection<string>();
        var progress=new Progress<string>(s=>{lines.Add($"{DateTime.Now:HH:mm:ss}  {s}");});
        try
        {
            var result=await _engine.RunAsync(a,dryRun,progress,_runCts.Token);
            var finished=DateTime.Now;
            if(!dryRun){a.RunCount++;a.LastRun=finished;Save();}
            _store.SaveRun(new AutomationRun{AutomationId=a.Id,AutomationName=a.Name,Started=started,Finished=finished,Success=result.Success,DryRun=dryRun,CompletedSteps=result.CompletedSteps,Message=result.Error??"Completed successfully."});
            ShowRunMonitor(a,dryRun,lines,result.Success,result.Error);
        }
        catch(OperationCanceledException){lines.Add("⏹ Cancelled by user.");ShowRunMonitor(a,dryRun,lines,false,"Cancelled");}
        finally{_runCts.Dispose();_runCts=null;}
    }

    private void ShowRunMonitor(Automation a,bool dryRun,ObservableCollection<string>? lines=null,bool? success=null,string? error=null)
    {
        PageTitle.Text="Live Execution Monitor";PageSubtitle.Text=$"{a.Name} • {(dryRun?"DRY RUN":"LIVE RUN")}";
        var root=new DockPanel();
        var top=new WrapPanel();
        AddButton(top,"⏹ Stop",StopRun);
        AddButton(top,"← Back to Designer",()=>OpenDesigner(a));
        DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
        var status=new TextBlock{Text=success==null?"● RUNNING":success.Value?"✓ SUCCESS":"✕ FAILED",FontSize=22,FontWeight=FontWeights.Bold,Foreground=success==null?(Brush)FindResource("AccentBrush"):success.Value?(Brush)FindResource("Accent2Brush"):(Brush)FindResource("DangerBrush"),Margin=new Thickness(5,12,5,10)};
        DockPanel.SetDock(status,Dock.Top);root.Children.Add(status);
        var log=new ListBox
        {
            ItemsSource=lines??new ObservableCollection<string>(),
            Background=(Brush)FindResource("PanelBrush"),
            Foreground=Brushes.White,
            BorderBrush=(Brush)FindResource("BorderBrush"),
            BorderThickness=new Thickness(1),
            Padding=new Thickness(8)
        };
        log.ItemContainerStyle = new Style(typeof(ListBoxItem))
        {
            Setters =
            {
                new Setter(ListBoxItem.BackgroundProperty, FindResource("Panel2Brush")),
                new Setter(ListBoxItem.ForegroundProperty, FindResource("TextBrush")),
                new Setter(ListBoxItem.PaddingProperty, new Thickness(10,7,10,7)),
                new Setter(ListBoxItem.MarginProperty, new Thickness(0,2,0,2)),
                new Setter(ListBoxItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)
            }
        };
        root.Children.Add(log);
        MainContent.Content=root;
        if(error!=null)lines?.Add("ERROR: "+error);
    }

    private void StopRun(){_runCts?.Cancel();}

    private void Duplicate(Automation a)
    {
        var copy=new Automation{Name=a.Name+" Copy",Description=a.Description,Schedule="Manual",Enabled=a.Enabled,
            Variables=a.Variables.Select(v=>new WorkflowVariable{Name=v.Name,Value=v.Value}).ToList()};
        var map=new Dictionary<Guid,Guid>();
        foreach(var n in a.Nodes){var clone=new WorkflowNode{Kind=n.Kind,Title=n.Title,Parameter=n.Parameter,Condition=n.Condition,X=n.X+30,Y=n.Y+30,Enabled=n.Enabled,RetryCount=n.RetryCount};map[n.Id]=clone.Id;copy.Nodes.Add(clone);}
        copy.Connections=a.Connections.Where(c=>map.ContainsKey(c.FromNodeId)&&map.ContainsKey(c.ToNodeId)).Select(c=>new WorkflowConnection{FromNodeId=map[c.FromNodeId],ToNodeId=map[c.ToNodeId],Label=c.Label}).ToList();
        _automations.Add(copy);Save();ShowAutomations();
    }

    private void DeleteSelected(){if(_selected==null)return;if(MessageBox.Show($"Delete '{_selected.Name}'?","Confirm",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes){_automations.Remove(_selected);_selected=null;Save();ShowAutomations();}}

    private void ExportAutomation(Automation a)
    {
        var dlg=new SaveFileDialog{Filter="JASS Automation (*.jassautomation)|*.jassautomation|JSON (*.json)|*.json",FileName=a.Name.Replace(" ","_")+".jassautomation"};
        if(dlg.ShowDialog()!=true)return;
        File.WriteAllText(dlg.FileName,System.Text.Json.JsonSerializer.Serialize(a,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
    }

    private void ImportAutomation()
    {
        var dlg=new OpenFileDialog{Filter="JASS Automation (*.jassautomation;*.json)|*.jassautomation;*.json|All files (*.*)|*.*"};
        if(dlg.ShowDialog()!=true)return;
        try
        {
            var a=System.Text.Json.JsonSerializer.Deserialize<Automation>(File.ReadAllText(dlg.FileName));
            if(a==null)throw new InvalidOperationException("Invalid workflow file.");
            a.Id=Guid.NewGuid();_automations.Add(a);Save();ShowAutomations();
        }
        catch(Exception ex){MessageBox.Show(ex.Message,"Import Failed",MessageBoxButton.OK,MessageBoxImage.Error);}
    }

    private async void SchedulerTimer_Tick(object? sender,EventArgs e)
    {
        foreach(var a in _automations.Where(x=>x.Enabled&&x.Schedule!="Manual"))
        {
            var now=DateTime.Now;
            var due=a.Schedule switch
            {
                "At Startup"=>a.LastRun==null,
                "Every 15 minutes"=>a.LastRun==null||now-a.LastRun.Value>=TimeSpan.FromMinutes(15),
                "Hourly"=>a.LastRun==null||now-a.LastRun.Value>=TimeSpan.FromHours(1),
                "Daily"=>a.LastRun==null||now.Date>a.LastRun.Value.Date,
                "Weekly"=>a.LastRun==null||now-a.LastRun.Value>=TimeSpan.FromDays(7),
                _=>false
            };
            if(due&&!_lastScheduled.ContainsKey(a.Id)){_lastScheduled[a.Id]=now;await RunAutomation(a,a.DryRunDefault);}
        }
    }

    private static string Friendly(ActionKind k)=>k switch
    {
        ActionKind.CreateFolder=>"📁 Create Folder",ActionKind.MoveFile=>"↪ Move File",ActionKind.CopyFile=>"⧉ Copy File",
        ActionKind.RenameFile=>"✎ Rename File",ActionKind.DeleteFile=>"⌫ Delete File",ActionKind.WriteLog=>"▤ Write Log",
        ActionKind.RunProcess=>"▶ Run Process",ActionKind.HttpGet=>"🌐 HTTP GET",ActionKind.Delay=>"◷ Delay",
        ActionKind.SetVariable=>"= Set Variable",ActionKind.IfCondition=>"◇ If Condition",ActionKind.ForEachFile=>"▦ For Each File",
        ActionKind.LoopEnd=>"↩ Loop End",_=>"● Start"
    };

    private static WorkflowNode NewNode(ActionKind kind,int index)=>new()
    {
        Kind=kind,
        Title=Friendly(kind).Replace("📁 ","").Replace("↪ ","").Replace("⧉ ","").Replace("✎ ","").Replace("⌫ ","").Replace("▤ ","").Replace("▶ ","").Replace("🌐 ","").Replace("◷ ","").Replace("= ","").Replace("◇ ","").Replace("▦ ","").Replace("↩ ",""),
        Parameter=kind==ActionKind.WriteLog?"Automation step completed":kind==ActionKind.Delay?"1000":"",
        X=80+(index%3)*250,Y=70+(index/3)*150
    };

    private void NewAutomation_Click(object sender,RoutedEventArgs e)=>NewAutomation("New Automation");
    private void NewAutomation(string name){var a=CreateTemplate(name,"Describe this automation.");a.Nodes=a.Nodes.Take(1).ToList();a.Connections.Clear();_automations.Add(a);Save();OpenDesigner(a);}
    private void Dashboard_Click(object sender,RoutedEventArgs e)=>ShowDashboard();
    private void Automations_Click(object sender,RoutedEventArgs e)=>ShowAutomations();
    private void Designer_Click(object sender,RoutedEventArgs e){if(_selected!=null)OpenDesigner(_selected);else if(_automations.Count>0)OpenDesigner(_automations[0]);else NewAutomation("New Automation");}
    private void History_Click(object sender,RoutedEventArgs e)=>ShowHistory();
    private void Templates_Click(object sender,RoutedEventArgs e)=>ShowAutomations();
    private void Logs_Click(object sender,RoutedEventArgs e){PageTitle.Text="Logs";PageSubtitle.Text="Local execution log.";var path=System.IO.Path.Combine(App.DataFolder,"automation.log");MainContent.Content=new TextBox{Text=File.Exists(path)?File.ReadAllText(path):"No log entries yet.",IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};}
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Settings";
        PageSubtitle.Text = "Local-first application settings.";

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "Storage",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold
        });
        stack.Children.Add(new TextBlock
        {
            Text = App.DataFolder,
            Foreground = Muted(),
            Margin = new Thickness(0, 8, 0, 20),
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Database",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold
        });
        stack.Children.Add(new TextBlock
        {
            Text = "automation-hub.db • SQLite local persistence",
            Foreground = Muted(),
            Margin = new Thickness(0, 8, 0, 20)
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Execution",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold
        });
        stack.Children.Add(new TextBlock
        {
            Text = "The scheduler runs while JASS Automation Hub is open. Workflows can be Dry Run by default.",
            Foreground = Muted(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        });

        MainContent.Content = new Border
        {
            Background = (Brush)FindResource("PanelBrush"),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(25),
            Child = stack
        };
    }
    private void Refresh_Click(object sender,RoutedEventArgs e){LoadData();ShowDashboard();}
}
