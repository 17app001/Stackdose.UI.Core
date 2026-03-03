# Shell Quickstart (Single Page Designer (Local Editable Page))

1. Edit Config/Machine1.config.json for PLC connection and addresses.
2. Open the designer page in Visual Studio:
   - local editable mode: Pages/SingleDetailWorkspacePage.xaml
   - template mode: Templates:SingleDetailWorkspacePage inside MainWindow.xaml
3. Drag UI.Core controls into Group A/B/C and run.
4. Optional layout preset at generation: -DesignerLayoutPreset ThreeColumn|TwoColumn64|TwoByTwo

Reference:
- Repo root Stackdose.App.SingleDetailLab/README_SINGLE_PAGE_QUICKSTART.md
"@ | Set-Content -Path D:\工作區\Project\Stackdose.UI.Core\scripts\Stackdose.App.YourPage\SHELL_QUICKSTART.md -Encoding UTF8
} else {
    @"
# Shell Quickstart

1. Configure your app in Config/app-meta.json.
2. Update machine/alarm/sensor json files under Config/.
   - required keys in machine config: alarmConfigFile, sensorConfigFile
3. Build and run your project.

Reference:
- Repo root QUICKSTART.md (recommended)
- Stackdose.UI.Core/Shell/SECOND_APP_QUICKSTART.md (advanced wiring details)
