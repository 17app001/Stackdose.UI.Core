# Shell Quickstart (Single Page Designer (Local Editable Page))

1. Edit `Config/Machine1.config.json` for PLC connection and addresses.
2. Open the designer page in Visual Studio:
   - local editable mode: `Pages/SingleDetailWorkspacePage.xaml`
   - template mode: `Templates:SingleDetailWorkspacePage` inside `MainWindow.xaml`
3. Drag `UI.Core` controls into Group A/B/C and run.
4. Optional layout preset at generation: `-DesignerLayoutPreset ThreeColumn|TwoColumn64|TwoByTwo|Blank|BlankTabs`
5. For `TwoColumn64`, adjust ratio with: `-DesignerSplitLeftWeight <N> -DesignerSplitRightWeight <N>`
6. `Blank` preset includes one SecuredButton and one PlcEventTrigger starter.
7. `BlankTabs` preset provides a styled TabControl; add/remove TabItem pages as needed.

Reference:
- Repo root `Stackdose.App.SingleDetailLab/README_SINGLE_PAGE_QUICKSTART.md`
