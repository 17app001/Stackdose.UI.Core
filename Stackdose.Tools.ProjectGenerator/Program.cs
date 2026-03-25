namespace Stackdose.Tools.ProjectGenerator;

/// <summary>
/// CLI entry point for the DeviceFramework Project Generator.
///
/// Usage:
///   dotnet run --project Stackdose.Tools.ProjectGenerator -- --spec "path/to/Spec.csv" [--output "path/to/output"]
///
/// If --output is omitted, the project is generated in the same directory as the solution root.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║  Stackdose DeviceFramework Project Generator    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Parse arguments
        var specPath = GetArg(args, "--spec");
        var outputPath = GetArg(args, "--output");

        if (string.IsNullOrWhiteSpace(specPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("? 缺少 --spec 參數");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("用法：");
            Console.WriteLine("  dotnet run --project Stackdose.Tools.ProjectGenerator -- --spec \"path/to/Spec.csv\" [--output \"path/to/output\"]");
            Console.WriteLine();
            Console.WriteLine("範例：");
            Console.WriteLine("  dotnet run --project Stackdose.Tools.ProjectGenerator -- --spec \"Stackdose.App.DeviceFramework/docs/examples/SimpleDemo-Spec.csv\"");
            return 1;
        }

        // ── 路徑解析 ────────────────────────────────────────────────
        // `dotnet run --project <proj>` 會把 CWD 切換到專案目錄，
        // 但使用者輸入的相對路徑是以「呼叫端目錄」為基準。
        // 我們用 DOTNET_CLI_CWD（dotnet run 會注入）或 fallback 到 Solution Root。
        var callerCwd = Environment.GetEnvironmentVariable("DOTNET_CLI_CWD")
                     ?? FindSolutionRoot()
                     ?? Directory.GetCurrentDirectory();

        // 若路徑不是絕對路徑，以 callerCwd 為 base 解析
        if (!Path.IsPathRooted(specPath))
            specPath = Path.GetFullPath(Path.Combine(callerCwd, specPath));

        if (!string.IsNullOrWhiteSpace(outputPath) && !Path.IsPathRooted(outputPath))
            outputPath = Path.GetFullPath(Path.Combine(callerCwd, outputPath));
        // ────────────────────────────────────────────────────────────

        // Resolve output path — default to the directory containing the .sln
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = FindSolutionRoot() ?? Directory.GetCurrentDirectory();
        }

        try
        {
            // 1. Parse CSV
            Console.WriteLine($"?? 讀取規格檔: {Path.GetFullPath(specPath)}");
            var spec = CsvParser.Parse(specPath);

            // 2. Print summary
            PrintSummary(spec);

            // 3. Generate project
            Console.WriteLine();
            Console.WriteLine("??  產生專案中...");
            Console.WriteLine();

            var generator = new ProjectGenerator(spec, outputPath);
            generator.Generate();

            // 4. Print results
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("? 專案產生完成！");
            Console.ResetColor();
            Console.WriteLine();

            var projectDir = Path.Combine(outputPath, spec.Project.ProjectName);
            Console.WriteLine($"?? {spec.Project.ProjectName}/");
            foreach (var file in generator.GeneratedFiles)
            {
                Console.WriteLine($"   ?? {file}");
            }

            Console.WriteLine();
            Console.WriteLine("?? 下一步：");
            Console.WriteLine($"   1. Visual Studio → 右鍵 Solution → Add → Existing Project → 選擇 {spec.Project.ProjectName}.csproj");
            Console.WriteLine("   2. dotnet build 驗證編譯");
            Console.WriteLine("   3. 開啟 Handlers/CommandHandlers.cs 填入業務邏輯");
            Console.WriteLine("   4. F5 執行");
            Console.WriteLine();

            // 5. Attempt build verification
            Console.WriteLine("?? 嘗試驗證編譯...");
            var buildResult = RunBuild(projectDir, spec.Project.ProjectName);
            if (buildResult)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("? dotnet build 成功！專案已可直接使用。");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("??  自動編譯驗證失敗（可能需要先將專案加入 Solution）。請手動執行 dotnet build 確認。");
                Console.ResetColor();
            }

            Console.WriteLine();
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"? 錯誤: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    private static void PrintSummary(DeviceSpec spec)
    {
        Console.WriteLine();
        Console.WriteLine("┌─ 專案摘要 ─────────────────────────────────────┐");
        Console.WriteLine($"│ 專案：{spec.Project.ProjectName,-42}│");
        Console.WriteLine($"│ 顯示：{spec.Project.HeaderDeviceName,-42}│");
        Console.WriteLine($"│ 版本：{spec.Project.Version,-42}│");
        Console.WriteLine($"│ 模式：{spec.Project.PageMode,-42}│");
        Console.WriteLine($"│ 機台：{spec.Machines.Count} 台{new string(' ', 39)}│");

        foreach (var m in spec.Machines)
        {
            var commands = spec.Commands.Where(c => c.MachineId == m.MachineId).ToList();
            var labels = spec.Labels.Where(l => l.MachineId == m.MachineId).ToList();

            Console.WriteLine("│                                                │");
            Console.WriteLine($"│ [{m.MachineId}] {m.MachineName,-40}│");
            Console.WriteLine($"│   PLC: {m.PlcIp}:{m.PlcPort,-33}│");
            Console.WriteLine($"│   Commands: {string.Join(", ", commands.Select(c => c.CommandName)),-36}│");
            Console.WriteLine($"│   Labels: {labels.Count} 個{new string(' ', 36)}│");
        }

        // Panels summary
        if (spec.Panels.Count > 0)
        {
            Console.WriteLine("│                                                │");
            var panelNames = string.Join(", ", spec.Panels.Select(p => p.PanelType));
            Console.WriteLine($"│ 面板：{panelNames,-42}│");
        }

        if (spec.MaintenanceItems.Count > 0)
        {
            Console.WriteLine($"│ 維護項：{spec.MaintenanceItems.Count} 個{new string(' ', 37)}│");
        }

        // File count
        var fileCount = 7 + spec.Machines.Count; // csproj + App.xaml + .cs + MainWindow.xaml + .cs + app-meta.json + CommandHandlers.cs + N machines
        if (spec.Project.PageMode.Equals("CustomPage", StringComparison.OrdinalIgnoreCase))
            fileCount += 2;
        if (spec.Panels.Any(p => p.PanelType.Equals("Settings", StringComparison.OrdinalIgnoreCase)))
            fileCount += 2;
        if (spec.Panels.Any(p => p.PanelType.Equals("MaintenanceMode", StringComparison.OrdinalIgnoreCase)))
            fileCount += 3; // MaintenancePage.xaml + .cs + MaintenanceHandlers.cs

        Console.WriteLine("│                                                │");
        Console.WriteLine($"│ 將產生 {fileCount} 個檔案{new string(' ', 36)}│");
        Console.WriteLine("└────────────────────────────────────────────────┘");
    }

    private static bool RunBuild(string projectDir, string projectName)
    {
        try
        {
            var csprojPath = Path.Combine(projectDir, $"{projectName}.csproj");
            if (!File.Exists(csprojPath)) return false;

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{csprojPath}\" --nologo -v q",
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null) return false;

            proc.WaitForExit(60_000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string? GetArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }

    private static string? FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 10 && dir is not null; i++)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        // Also try from current directory
        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int i = 0; i < 10 && dir is not null; i++)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }
}
