import os
import re
import yaml
import shutil
from pathlib import Path

# Configuration
ENGINE_DIR = Path('Engine')
OUTPUT_DIR = Path('DocsSite')
DOCS_DIR = OUTPUT_DIR / 'docs'

def extract_csharp_docs(file_path):
    docs = []
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            
        buffer_summary = []
        current_summary = []
        in_summary = False
        
        for line in lines:
            stripped = line.strip()
            
            if stripped.startswith('/// <summary>'):
                in_summary = True
                current_summary = []
            elif in_summary and stripped.startswith('/// </summary>'):
                in_summary = False
                buffer_summary = current_summary
            elif in_summary and stripped.startswith('///'):
                text = stripped[3:].strip()
                current_summary.append(text)
            elif not in_summary:
                if not stripped:
                    continue
                if stripped.startswith('//'):
                    continue
                if stripped.startswith('['):
                    continue
                
                match = re.search(r'\b(?:public|private|protected|internal)?\s*(?:static|sealed|abstract|partial|readonly|ref)?\s*\b(class|struct|interface|record)\s+(\w+)(.*)', stripped)
                if match:
                    decl = match.group(0)
                    if '{' in decl:
                        decl = decl.split('{')[0]
                    
                    docs.append({
                        'type': match.group(1),
                        'name': match.group(2),
                        'signature': decl.strip(),
                        'summary': '\n'.join(buffer_summary)
                    })
                    buffer_summary = []
                else:
                    if not stripped.startswith('namespace') and not stripped.startswith('using'):
                        buffer_summary = []
                        
    except Exception as e:
        print(f"Error reading {file_path}: {e}")
        
    return docs

def add_to_nav(nav_list, path_parts, target_file):
    if not path_parts:
        return
    part = path_parts[0]
    if len(path_parts) == 1:
        nav_list.append({part: target_file})
    else:
        found = False
        for item in nav_list:
            if isinstance(item, dict) and part in item:
                if isinstance(item[part], list):
                    add_to_nav(item[part], path_parts[1:], target_file)
                    found = True
                    break
        if not found:
            new_sublist = []
            add_to_nav(new_sublist, path_parts[1:], target_file)
            nav_list.append({part: new_sublist})

def build_docs_site():
    if OUTPUT_DIR.exists():
        shutil.rmtree(OUTPUT_DIR)
        
    DOCS_DIR.mkdir(parents=True, exist_ok=True)
    
    navigation = []
    
    # Traverse Engine directory
    for root, dirs, files in os.walk(ENGINE_DIR):
        if 'docs.md' in files:
            source_docs_path = Path(root) / 'docs.md'
            rel_path = Path(root).relative_to(ENGINE_DIR)
            
            # Find all .cs files in this directory
            cs_files = [f for f in files if f.endswith('.cs')]
            cs_docs = []
            for cs_file in cs_files:
                cs_path = Path(root) / cs_file
                extracted = extract_csharp_docs(cs_path)
                if extracted:
                    cs_docs.extend(extracted)
            
            # Read original docs.md
            with open(source_docs_path, 'r', encoding='utf-8') as f:
                content = f.read()
                
            # Append C# docs
            if cs_docs:
                content += "\n\n## API Reference\n\n"
                for doc in cs_docs:
                    content += f"### {doc['type'].capitalize()} `{doc['name']}`\n\n"
                    if doc['summary']:
                        content += f"{doc['summary']}\n\n"
                    content += f"```csharp\n{doc['signature']}\n```\n\n"
            
            if str(rel_path) == '.':
                out_file = DOCS_DIR / 'index.md'
                nav_parts = ['Home']
                target_md = 'index.md'
            else:
                out_file = DOCS_DIR / f"{rel_path}.md"
                out_file.parent.mkdir(parents=True, exist_ok=True)
                nav_parts = list(rel_path.parts)
                target_md = f"{rel_path}.md"
            
            with open(out_file, 'w', encoding='utf-8') as f:
                f.write(content)
                
            # Add to navigation
            add_to_nav(navigation, nav_parts, target_md)

    # Check if index.md was created, if not, create a dummy one
    if not (DOCS_DIR / 'index.md').exists():
        with open(DOCS_DIR / 'index.md', 'w', encoding='utf-8') as f:
            f.write("# MagicEngine Documentation\n\nWelcome to the MagicEngine documentation. Navigate using the sidebar.")
        navigation.insert(0, {'Home': 'index.md'})
    else:
        # Move Home to top
        home_idx = -1
        for i, item in enumerate(navigation):
            if 'Home' in item:
                home_idx = i
                break
        if home_idx > 0:
            home = navigation.pop(home_idx)
            navigation.insert(0, home)

    # Generate mkdocs.yml
    mkdocs_config = {
        'site_name': 'MagicEngine Documentation',
        'site_description': 'Internal game engine API and architecture documentation.',
        'theme': {
            'name': 'material',
            'font': {
                'text': 'Inter',
                'code': 'Fira Code'
            },
            'palette': [
                {
                    'media': '(prefers-color-scheme: light)',
                    'scheme': 'default',
                    'primary': 'indigo',
                    'accent': 'indigo',
                    'toggle': {
                        'icon': 'material/brightness-7',
                        'name': 'Switch to dark mode'
                    }
                },
                {
                    'media': '(prefers-color-scheme: dark)',
                    'scheme': 'slate',
                    'primary': 'indigo',
                    'accent': 'indigo',
                    'toggle': {
                        'icon': 'material/brightness-4',
                        'name': 'Switch to light mode'
                    }
                }
            ],
            'features': [
                'navigation.indexes',
                'navigation.sections',
                'navigation.tabs',
                'navigation.tabs.sticky',
                'navigation.top',
                'navigation.tracking',
                'search.suggest',
                'search.highlight',
                'search.share',
                'content.code.copy',
                'content.code.select',
                'content.tabs.link'
            ]
        },
        'markdown_extensions': [
            'abbr',
            'admonition',
            'attr_list',
            'def_list',
            'footnotes',
            'md_in_html',
            'tables',
            {'pymdownx.details': None},
            {'pymdownx.superfences': None},
            {'pymdownx.highlight': {'anchor_linenums': True}},
            {'pymdownx.inlinehilite': None},
            {'pymdownx.snippets': None},
            {'pymdownx.magiclink': None},
            {'pymdownx.tasklist': {'custom_checkbox': True}}
        ],
        'nav': navigation
    }
    
    with open(OUTPUT_DIR / 'mkdocs.yml', 'w', encoding='utf-8') as f:
        yaml.dump(mkdocs_config, f, default_flow_style=False, sort_keys=False)

    print("Documentation generated successfully in DocsSite/")
    print("To view it, install mkdocs and run it:")
    print("  pip install mkdocs-material")
    print("  cd DocsSite")
    print("  mkdocs serve")

if __name__ == '__main__':
    build_docs_site()
