import os
import re
import yaml
import shutil
import time
import subprocess
import webbrowser
import sys
from pathlib import Path

def value_constructor(loader, node):
    return loader.construct_scalar(node)
yaml.SafeLoader.add_constructor('tag:yaml.org,2002:value', value_constructor)

# --- Terminal Colors ---
class C:
    INFO = '\033[96m'
    SUCCESS = '\033[92m'
    WARN = '\033[93m'
    ERROR = '\033[91m'
    RESET = '\033[0m'
    DIM = '\033[2m'

# --- Configuration ---
ENGINE_DIR = Path('Engine')
OUTPUT_DIR = Path('DocsSite')
DOCS_DIR = OUTPUT_DIR / 'docs'
CSS_DIR = DOCS_DIR / 'css'

def generate_docfx_json():
    config = {
        "metadata": [
            {
                "src": [{"files": ["MagicEngine.csproj"], "src": "."}],
                "dest": "obj/api",
                "disableGitFeatures": False
            }
        ]
    }
    with open('docfx.json', 'w', encoding='utf-8') as f:
        yaml.dump(config, f, default_flow_style=False, sort_keys=False)

def run_docfx():
    if not os.path.exists('docfx.json'):
        generate_docfx_json()
    print(f"{C.INFO}Running DocFX metadata extraction...{C.RESET}")
    docfx_cmd = shutil.which('docfx') or os.path.expanduser('~/.dotnet/tools/docfx')
    subprocess.run([docfx_cmd, 'metadata', 'docfx.json'], check=True)

def load_docfx_metadata(api_dir=Path('obj/api')):
    metadata_by_file = {}
    if not api_dir.exists():
        return metadata_by_file
        
    for yml_file in api_dir.glob('*.yml'):
        if yml_file.name in ('toc.yml', '.manifest') or yml_file.name.startswith('obj'):
            continue
        try:
            with open(yml_file, 'r', encoding='utf-8') as f:
                content = yaml.safe_load(f)
                
            if not content or 'items' not in content:
                continue
                
            for item in content['items']:
                if 'source' in item and 'path' in item['source']:
                    src_path = str(Path(item['source']['path']).resolve())
                    if src_path not in metadata_by_file:
                        metadata_by_file[src_path] = []
                    metadata_by_file[src_path].append(item)
        except Exception as e:
            print(f"{C.WARN}Failed to parse {yml_file}: {e}{C.RESET}")
            
    return metadata_by_file

def add_to_nav(nav_list, path_parts, target_file):
    if not path_parts: return
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

def create_modern_css():
    """Injects custom CSS to flatten the Material theme into a modern SaaS look."""
    CSS_DIR.mkdir(parents=True, exist_ok=True)
    css_content = """
    :root {
        --md-text-font: "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", sans-serif;
        --md-code-font: "Fira Code", "Menlo", "Monaco", "Consolas", monospace;
        --md-typeset-color: var(--md-default-fg-color);
    }

    /* Light Theme (Vercel-like) */
    [data-md-color-scheme="default"] {
        --md-default-bg-color: #ffffff;
        --md-default-fg-color: #111111;
        --md-default-fg-color--light: #666666;
        --md-default-fg-color--lightest: #eaeaea;
        --md-primary-fg-color: #000000;
        --md-accent-fg-color: #000000;
        --md-code-bg-color: #fafbfc;
        --md-code-fg-color: #24292e;
        --md-typeset-a-color: #0070f3;
    }

    /* Dark Theme (Vercel-like) */
    [data-md-color-scheme="slate"] {
        --md-default-bg-color: #000000;
        --md-default-fg-color: #ededed;
        --md-default-fg-color--light: #a1a1aa;
        --md-default-fg-color--lightest: #333333;
        --md-primary-fg-color: #ffffff;
        --md-accent-fg-color: #ffffff;
        --md-code-bg-color: #111111;
        --md-code-fg-color: #ededed;
        --md-typeset-a-color: #3291ff;
    }

    /* Header (Glassmorphism) */
    .md-header {
        background-color: var(--md-default-bg-color);
        backdrop-filter: saturate(180%) blur(5px);
        box-shadow: none !important;
        border-bottom: 1px solid var(--md-default-fg-color--lightest);
    }
    
    @supports (backdrop-filter: blur(5px)) {
        .md-header {
            background-color: transparent !important;
        }
        [data-md-color-scheme="default"] .md-header {
            background-color: rgba(255, 255, 255, 0.8) !important;
        }
        [data-md-color-scheme="slate"] .md-header {
            background-color: rgba(0, 0, 0, 0.8) !important;
        }
    }

    .md-header__button { color: var(--md-default-fg-color); }
    .md-header__title { font-weight: 600; color: var(--md-primary-fg-color); letter-spacing: -0.02em; }

    /* Typography tweaks */
    .md-typeset h1, .md-typeset h2, .md-typeset h3 {
        font-weight: 600;
        letter-spacing: -0.02em;
        color: var(--md-primary-fg-color);
    }
    .md-typeset h1 { font-size: 2.5em; margin-bottom: 0.5em; }
    .md-typeset h2 { font-size: 1.75em; border-bottom: none; }

    /* API Categories (h4) */
    .md-typeset h4 {
        margin-top: 1.5em;
        font-weight: 600;
        color: var(--md-typeset-a-color);
        text-transform: uppercase;
        font-size: 0.9em;
        letter-spacing: 0.05em;
        border-bottom: 1px solid var(--md-default-fg-color--lightest);
        padding-bottom: 0.3em;
    }

    /* API Members (h5) - reset default */
    .md-typeset h5 {
        margin: 0;
    }
    
    .api-member-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 0.75rem;
        padding-bottom: 0.5rem;
        border-bottom: 1px solid var(--md-default-fg-color--lightest);
    }

    .api-member-title {
        font-family: var(--md-code-font);
        font-size: 1.1em;
        font-weight: 600;
        color: var(--md-primary-fg-color);
    }
    
    .api-member-context {
        font-size: 0.75em;
        color: var(--md-default-fg-color--light);
        background: var(--md-default-bg-color);
        padding: 0.2em 0.6em;
        border-radius: 4px;
        border: 1px solid var(--md-default-fg-color--lightest);
    }
    
    .api-member-context .kind-label {
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }

    .api-member-context code {
        background: transparent;
        padding: 0;
        font-size: 1em;
        color: inherit;
        border: none;
        box-shadow: none;
    }
    
    .md-typeset a { color: var(--md-typeset-a-color); text-decoration: none; transition: opacity 0.2s; }
    .md-typeset a:hover { opacity: 0.8; text-decoration: underline; }

    /* Modernize Code blocks */
    .md-typeset pre>code { 
        border-radius: 8px; 
        border: 1px solid var(--md-default-fg-color--lightest); 
        background: var(--md-code-bg-color);
        box-shadow: none;
    }
    .md-typeset .highlight, .md-typeset pre { background: var(--md-code-bg-color) !important; }

    /* Inline Code */
    .md-typeset code {
        border-radius: 6px;
        padding: 0.2em 0.4em;
        background: var(--md-default-fg-color--lightest);
        font-size: 0.85em;
        word-break: break-word;
        color: var(--md-primary-fg-color);
    }

    /* Search Bar */
    .md-search__input {
        background-color: var(--md-default-fg-color--lightest) !important;
        border-radius: 6px;
        border: 1px solid transparent;
        transition: border 0.2s ease, background-color 0.2s ease;
    }
    .md-search__input:focus {
        border-color: var(--md-default-fg-color--lightest);
        background-color: var(--md-default-bg-color) !important;
        box-shadow: 0 0 0 2px var(--md-default-fg-color--lightest);
    }

    /* Modern Docs Design: Nested Cards */
    details.api-type-card {
        background: var(--md-default-bg-color) !important;
        border: 1px solid var(--md-default-fg-color--lightest) !important;
        border-left: 1px solid var(--md-default-fg-color--lightest) !important;
        border-inline-start: 1px solid var(--md-default-fg-color--lightest) !important;
        border-radius: 8px;
        padding: 0 1rem 1rem 1rem;
        margin-bottom: 1rem;
        box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05);
        font-size: inherit !important;
    }
    
    /* Remove Material admonition icon from summary */
    details.api-type-card > summary::before {
        display: none !important;
        content: none !important;
        mask-image: none !important;
        -webkit-mask-image: none !important;
        background-color: transparent !important;
    }
    
    details.api-type-card[open] summary {
        border-bottom: 1px solid var(--md-default-fg-color--lightest);
        margin-bottom: 0.75rem;
    }
    
    details.api-type-card > summary {
        cursor: pointer;
        padding: 0.75rem 0;
        outline: none;
        list-style: none !important;
        display: flex;
        align-items: center;
        justify-content: space-between;
        background: transparent !important;
    }
    
    details.api-type-card > summary::-webkit-details-marker {
        display: none;
    }
    
    details.api-type-card > summary > h3 {
        margin: 0;
        border: none;
        padding: 0;
    }

    details.api-type-card > summary::after {
        content: "▶" !important;
        font-size: 0.8em;
        color: var(--md-default-fg-color--light);
        transition: transform 0.2s;
        display: inline-block !important;
        mask-image: none !important;
        -webkit-mask-image: none !important;
        background-color: transparent !important;
        width: auto !important;
        height: auto !important;
    }
    
    details.api-type-card[open] > summary::after {
        transform: rotate(90deg);
    }

    .api-category-block {
        margin-top: 0.75rem;
    }

    .api-member-card {
        background: var(--md-code-bg-color);
        border: 1px solid var(--md-default-fg-color--lightest);
        border-radius: 6px;
        padding: 0.5rem 0.75rem;
        margin-top: 0.5rem;
        margin-bottom: 0.5rem;
        transition: border-color 0.2s;
    }
    
    .api-member-card > p:first-of-type {
        margin-top: 0.5rem;
    }
    
    .api-member-card > p:last-child {
        margin-bottom: 0;
    }
    
    .api-member-card .highlight {
        margin: 0.5rem 0;
    }
    
    .api-member-card:hover {
        border-color: var(--md-typeset-a-color);
    }

    /* Tabs / Nav */
    .md-tabs { 
        background-color: var(--md-default-bg-color); 
        border-bottom: 1px solid var(--md-default-fg-color--lightest); 
    }
    .md-nav__link--active {
        font-weight: 500;
        color: var(--md-primary-fg-color) !important;
    }
    .md-sidebar--secondary {
        border-left: 1px solid var(--md-default-fg-color--lightest);
    }
    
    /* Content spacing */
    .md-content__inner {
        padding-top: 1.5rem;
    }

    .[dir=ltr] .md-typeset .admonition-title, [dir=ltr] .md-typeset summary {
        padding-left: 1rem !important;
    }

    .[dir=ltr] .md-typeset summary:after {
        top: 1.3rem !important
    }
    """
    with open(CSS_DIR / 'custom.css', 'w', encoding='utf-8') as f:
        f.write(css_content)

def build_docs_site():
    if OUTPUT_DIR.exists():
        shutil.rmtree(OUTPUT_DIR)

    DOCS_DIR.mkdir(parents=True, exist_ok=True)
    create_modern_css()

    run_docfx()
    metadata_by_file = load_docfx_metadata()

    navigation = []

    docs_locations = []
    for root, dirs, files in os.walk(ENGINE_DIR):
        if 'docs.md' in files:
            docs_locations.append(Path(root).resolve())

    for docs_loc in docs_locations:
        source_docs_path = docs_loc / 'docs.md'
        rel_path = docs_loc.relative_to(ENGINE_DIR.resolve())

        cs_files = []
        for root, dirs, files in os.walk(docs_loc):
            current_path = Path(root).resolve()
            if current_path != docs_loc and current_path in docs_locations:
                dirs.clear()
                continue
            for f in files:
                if f.endswith('.cs'):
                    cs_files.append(current_path / f)

        api_content = ""
        for cs_file in cs_files:
            abs_cs_file = str(cs_file)
            if abs_cs_file in metadata_by_file:
                items = metadata_by_file[abs_cs_file]

                types = [i for i in items if i.get('type') in ('Class', 'Struct', 'Interface', 'Enum', 'Delegate')]
                members = [i for i in items if i.get('type') not in ('Class', 'Struct', 'Interface', 'Enum', 'Delegate', 'Namespace')]

                members_by_parent = {}
                for m in members:
                    parent_uid = m.get('parent')
                    if parent_uid:
                        members_by_parent.setdefault(parent_uid, []).append(m)

                for t in types:
                    name = t.get('name', '')
                    kind = t.get('type', '')
                    summary = t.get('summary', '').strip()
                    signature = t.get('syntax', {}).get('content', '')

                    api_content += f"<details class=\"api-type-card\" markdown=\"1\">\n<summary><h3>{name} <small>({kind})</small></h3></summary>\n\n"
                    if summary:
                        api_content += f"{summary}\n\n"
                    api_content += f"```csharp\n{signature}\n```\n\n"

                    type_uid = t.get('uid')
                    type_members = members_by_parent.get(type_uid, [])
                    if type_members:
                        categories = {
                            'Enum Values': [],
                            'Constructors': [],
                            'Properties': [],
                            'Methods': [],
                            'Events': [],
                            'Fields': []
                        }

                        for m in type_members:
                            m_name = m.get('name', '')
                            if 'System.Object' in m_name or 'Equals' in m_name or 'GetHashCode' in m_name or 'ToString' in m_name or m_name in ('Finalize', 'MemberwiseClone', 'ReferenceEquals', 'GetType'):
                                continue

                            m_kind = m.get('type', '')
                            if kind == 'Enum': categories['Enum Values'].append(m)
                            elif m_kind == 'Constructor': categories['Constructors'].append(m)
                            elif m_kind == 'Property': categories['Properties'].append(m)
                            elif m_kind == 'Method': categories['Methods'].append(m)
                            elif m_kind == 'Event': categories['Events'].append(m)
                            elif m_kind == 'Field': categories['Fields'].append(m)
                            else: categories['Methods'].append(m)

                        for cat_name, items in categories.items():
                            if not items: continue

                            api_content += f"<div class=\"api-category-block\" markdown=\"1\">\n#### {cat_name}\n\n"

                            if cat_name == 'Enum Values':
                                api_content += "| Value | Summary |\n|---|---|\n"
                                for m in items:
                                    m_name = m.get('name', '')
                                    m_summary = m.get('summary', '').strip().replace('\n', '<br>')
                                    m_signature = m.get('syntax', {}).get('content', '')
                                    val = m_signature if m_signature else m_name
                                    api_content += f"| `{val}` | {m_summary} |\n"
                                api_content += "\n</div>\n\n"
                                continue

                            for m in items:
                                m_name = m.get('name', '')
                                m_summary = m.get('summary', '').strip()
                                m_signature = m.get('syntax', {}).get('content', '')
                                m_returns = m.get('syntax', {}).get('return', {}).get('type', '')
                                m_params = m.get('syntax', {}).get('parameters', [])
                                inner_kind = m.get('type', '')

                                display_name = m_name.split('(')[0] if '(' in m_name else m_name

                                api_content += f"<div class=\"api-member-card\" markdown=\"1\">\n"
                                api_content += f"<div class=\"api-member-header\">\n"
                                api_content += f"<div class=\"api-member-title\">{display_name}</div>\n"
                                api_content += f"<div class=\"api-member-context\"><span class=\"kind-label\">{inner_kind} of</span> <code>{name}</code></div>\n"
                                api_content += f"</div>\n\n"

                                if m_summary:
                                    api_content += f"{m_summary}\n\n"

                                if m_signature:
                                    api_content += f"```csharp\n{m_signature}\n```\n\n"

                                if m_params:
                                    api_content += "**Parameters:**\n\n"
                                    for p in m_params:
                                        p_id = p.get('id', '')
                                        p_desc = p.get('description', '')
                                        api_content += f"* `{p_id}`: {p_desc}\n"
                                    api_content += "\n"

                                if m_returns and m_returns != 'System.Void' and m_returns != 'void':
                                    m_return_desc = m.get('syntax', {}).get('return', {}).get('description', '')
                                    api_content += f"**Returns:** `{m_returns}`"
                                    if m_return_desc:
                                        api_content += f" - {m_return_desc}"
                                    api_content += "\n\n"
                                api_content += "</div>\n\n"
                            
                            api_content += "</div>\n\n"

                    api_content += "</details>\n\n"

        with open(source_docs_path, 'r', encoding='utf-8') as f:
            content = f.read()

        if api_content:
            content += "\n\n## API Reference\n\n" + api_content

        if str(rel_path) == '.':
            out_file = DOCS_DIR / 'index.md'
            nav_parts, target_md = ['Home'], 'index.md'
        else:
            out_file = DOCS_DIR / f"{rel_path}.md"
            out_file.parent.mkdir(parents=True, exist_ok=True)
            nav_parts, target_md = list(rel_path.parts), f"{rel_path}.md"

        with open(out_file, 'w', encoding='utf-8') as f:
            f.write(content)

        add_to_nav(navigation, nav_parts, target_md)

    if not (DOCS_DIR / 'index.md').exists():
        with open(DOCS_DIR / 'index.md', 'w', encoding='utf-8') as f:
            f.write("# MagicEngine Documentation\n\nWelcome.")
        navigation.insert(0, {'Home': 'index.md'})
    else:
        navigation.sort(key=lambda x: 0 if 'Home' in x else 1)

    # Modernized MkDocs Config
    mkdocs_config = {
        'site_name': 'MagicEngine API',
        'theme': {
            'name': 'material',
            'palette': [
                {'media': '(prefers-color-scheme: light)', 'scheme': 'default', 'primary': 'white', 'accent': 'black', 'toggle': {'icon': 'material/brightness-7', 'name': 'Dark mode'}},
                {'media': '(prefers-color-scheme: dark)', 'scheme': 'slate', 'primary': 'black', 'accent': 'white', 'toggle': {'icon': 'material/brightness-4', 'name': 'Light mode'}}
            ],
            'features': ['navigation.tabs', 'navigation.sections', 'content.code.copy']
        },
        'extra_css': ['css/custom.css'],
        'markdown_extensions': ['admonition', 'tables', 'md_in_html', {'pymdownx.highlight': {'anchor_linenums': True}}, 'pymdownx.superfences'],
        'nav': navigation
    }

    with open(OUTPUT_DIR / 'mkdocs.yml', 'w', encoding='utf-8') as f:
        yaml.dump(mkdocs_config, f, default_flow_style=False, sort_keys=False)

def get_latest_mtime(directory):
    latest = 0
    if not directory.exists(): return latest
    for root, _, files in os.walk(directory):
        for file in files:
            mtime = os.path.getmtime(Path(root) / file)
            if mtime > latest: latest = mtime
    return latest

def run_and_watch():
    print(f"{C.INFO}Starting initial build...{C.RESET}")
    build_docs_site()

    print(f"{C.INFO}Starting mkdocs server...{C.RESET}")
    process = subprocess.Popen([sys.executable, '-m', 'mkdocs', 'serve'], cwd=OUTPUT_DIR)

    time.sleep(2)
    webbrowser.open('http://127.0.0.1:8000')

    last_mtime = get_latest_mtime(ENGINE_DIR)

    try:
        print(f"\n{C.SUCCESS}Watching '{ENGINE_DIR}' for changes. Press Ctrl+C to stop.{C.RESET}\n")
        while True:
            time.sleep(2) # Reduced sleep for snappier feedback
            current_mtime = get_latest_mtime(ENGINE_DIR)

            if current_mtime > last_mtime:
                print(f"{C.WARN}Changes detected! Rebuilding site...{C.RESET}")
                process.terminate()
                process.wait()
                time.sleep(0.5)

                build_docs_site()
                process = subprocess.Popen([sys.executable, '-m', 'mkdocs', 'serve'], cwd=OUTPUT_DIR)
                last_mtime = current_mtime
                print(f"{C.SUCCESS}Rebuild complete. Server restarted.{C.RESET}\n")

    except KeyboardInterrupt:
        print(f"\n{C.INFO}Stopping process... Goodbye!{C.RESET}")
        process.terminate()
        process.wait()

if __name__ == '__main__':
    run_and_watch()