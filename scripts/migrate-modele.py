import re
import json
import os

def migrate_modele(designer_path, output_json):
    with open(designer_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Pass 1: Extract declarations
    # private UserControls.PlcLabelField plcLabelTotalLayout;
    declarations = re.findall(r'private ([\w\.]+) (\w+);', content)
    controls = {}
    for ctrl_type, name in declarations:
        controls[name] = {
            'type': ctrl_type,
            'name': name,
            'x': 0,
            'y': 0,
            'w': 0,
            'h': 0,
            'text': '',
            'parent': None,
            'props': {}
        }

    # Pass 2: Extract properties
    # name.Location = new Point(12, 34);
    locations = re.findall(r'(\w+)\.Location = new Point\((\d+), (\d+)\);', content)
    for name, x, y in locations:
        if name in controls:
            controls[name]['x'] = int(x)
            controls[name]['y'] = int(y)

    # name.Size = new Size(100, 20);
    sizes = re.findall(r'(\w+)\.Size = new Size\((\d+), (\d+)\);', content)
    for name, w, h in sizes:
        if name in controls:
            controls[name]['w'] = int(w)
            controls[name]['h'] = int(h)

    # name.Text = "Hello";
    texts = re.findall(r'(\w+)\.Text = "(.*?)";', content)
    for name, text in texts:
        if name in controls:
            controls[name]['text'] = text

    # Parenting: parent.Controls.Add(child);
    parenting = re.findall(r'(\w+)\.Controls\.Add\((\w+)\);', content)
    for parent, child in parenting:
        if child in controls:
            controls[child]['parent'] = parent

    # Custom Props (Device, DWord, BitPosition, Title)
    # plcLabelTotalLayout.Device = "D100";
    custom_props = re.findall(r'(\w+)\.(\w+) = (.*?);', content)
    for name, prop, val in custom_props:
        if name in controls:
            # Clean value (strip quotes, convert to bool/int)
            val = val.strip('"')
            if val.lower() == 'true': val = True
            elif val.lower() == 'false': val = False
            elif val.isdigit(): val = int(val)
            
            controls[name]['props'][prop] = val

    # Pass 3: Calculate absolute coordinates
    def get_abs_pos(name):
        ctrl = controls[name]
        abs_x, abs_y = ctrl['x'], ctrl['y']
        curr_parent = ctrl['parent']
        while curr_parent and curr_parent in controls:
            parent_ctrl = controls[curr_parent]
            abs_x += parent_ctrl['x']
            abs_y += parent_ctrl['y']
            # If parent is a TabPage or Panel, it might have offsets. 
            # WinForms TabPage/Panel inside SplitContainer might be tricky.
            curr_parent = parent_ctrl['parent']
        return abs_x, abs_y

    # Pass 4: Map to UI.Core JSON
    items = []
    for name, ctrl in controls.items():
        # Filter: only migrate meaningful controls
        supported_types = ['PlcLabelField', 'Button', 'Label', 'GroupBox']
        is_supported = any(t in ctrl['type'] for t in supported_types)
        if not is_supported: continue

        abs_x, abs_y = get_abs_pos(name)
        
        item = {
            "id": name,
            "type": "PlcLabel", # Default mapping
            "order": 0,
            "x": abs_x,
            "y": abs_y,
            "width": ctrl['w'],
            "height": ctrl['h'],
            "locked": False,
            "props": {},
            "events": []
        }

        if 'PlcLabelField' in ctrl['type']:
            item["type"] = "PlcLabel"
            item["props"]["address"] = ctrl['props'].get('Device', '')
            item["props"]["label"] = ctrl['props'].get('Title', ctrl['text'])
            
            bit_pos = ctrl['props'].get('BitPosition', -1)
            is_dword = ctrl['props'].get('DWord', False)
            
            if bit_pos != -1 and str(bit_pos) != '-1':
                item["props"]["dataType"] = "Bit"
                if item["props"]["address"]:
                    item["props"]["address"] += f".{bit_pos}"
            elif is_dword:
                item["props"]["dataType"] = "DWord"
            else:
                item["props"]["dataType"] = "Word"
        
        elif 'Button' in ctrl['type']:
            item["type"] = "SecuredButton"
            item["props"]["label"] = ctrl['text']
            item["props"]["address"] = ""
        
        elif 'Label' in ctrl['type']:
            item["type"] = "PlcLabel"
            item["props"]["label"] = ctrl['text']
            item["props"]["address"] = "" # Static label
            
        elif 'GroupBox' in ctrl['type']:
            item["type"] = "Spacer" # GroupBox maps to Spacer in Runtime
            item["props"]["title"] = ctrl['text']

        items.append(item)

    # Wrap in DesignDocument structure
    doc = {
        "version": "1.0",
        "meta": {
            "title": "Migrated from ModelE",
            "author": "Gemini Migration Tool",
            "description": f"Imported from {designer_path}"
        },
        "layout": {
            "mode": "Standard",
            "showLiveLog": True,
            "showAlarmViewer": True
        },
        "shellMode": "Standard",
        "canvasWidth": 1284,
        "canvasHeight": 729,
        "canvasItems": items,
        "pages": []
    }

    import codecs
    with open(output_json, 'w', encoding='utf-8-sig') as f:
        json.dump(doc, f, indent=2, ensure_ascii=False)
    
    print(f"Migration completed: {len(items)} items migrated to {output_json}")

if __name__ == "__main__":
    designer = r'D:\工作區\Project\Stackdose.Solution.ModelE\Stackdose.Solution.ModelE\Form1.Designer.cs'
    output = 'machinedesign.json'
    migrate_modele(designer, output)
