import type { DiffHunk } from '../types';

export function parsePatch(patch: string): DiffHunk[] {
  if (!patch) return [];

  const hunks: DiffHunk[] = [];
  const lines = patch.split('\n');
  let currentHunk: DiffHunk | null = null;

  for (const line of lines) {
    // Parse hunk header: @@ -oldStart,oldLines +newStart,newLines @@
    const hunkMatch = line.match(/^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@/);
    if (hunkMatch) {
      if (currentHunk) {
        hunks.push(currentHunk);
      }
      currentHunk = {
        oldStart: parseInt(hunkMatch[1] || '0'),
        oldLines: parseInt(hunkMatch[2] || '1'),
        newStart: parseInt(hunkMatch[3] || '0'),
        newLines: parseInt(hunkMatch[4] || '1'),
        lines: [],
      };
      continue;
    }

    if (!currentHunk) continue;

    // Parse diff lines
    if (line.startsWith('+') && !line.startsWith('+++')) {
      currentHunk.lines.push({
        type: 'add',
        content: line.substring(1),
        newLineNumber: currentHunk.newStart + currentHunk.lines.filter(l => l.type !== 'delete').length,
      });
    } else if (line.startsWith('-') && !line.startsWith('---')) {
      currentHunk.lines.push({
        type: 'delete',
        content: line.substring(1),
        oldLineNumber: currentHunk.oldStart + currentHunk.lines.filter(l => l.type !== 'add').length,
      });
    } else if (line.startsWith(' ') || line === '') {
      const contextLineCount = currentHunk.lines.filter(l => l.type !== 'delete').length;
      currentHunk.lines.push({
        type: 'context',
        content: line.substring(1),
        oldLineNumber: currentHunk.oldStart + currentHunk.lines.filter(l => l.type !== 'add').length,
        newLineNumber: currentHunk.newStart + contextLineCount,
      });
    }
  }

  if (currentHunk) {
    hunks.push(currentHunk);
  }

  return hunks;
}

export function getLanguageFromPath(path: string): string {
  const ext = path.split('.').pop()?.toLowerCase();
  const languageMap: Record<string, string> = {
    ts: 'typescript',
    tsx: 'typescript',
    js: 'javascript',
    jsx: 'javascript',
    vue: 'vue',
    css: 'css',
    scss: 'scss',
    html: 'html',
    json: 'json',
    md: 'markdown',
    py: 'python',
    java: 'java',
    cs: 'csharp',
    go: 'go',
    rs: 'rust',
    rb: 'ruby',
    php: 'php',
    cpp: 'cpp',
    c: 'c',
    sql: 'sql',
    sh: 'bash',
    yaml: 'yaml',
    yml: 'yaml',
  };
  return languageMap[ext || ''] || 'text';
}

export function buildFileTree(files: { path: string; status: string }[]): FileTreeNode[] {
  const root: Record<string, FileTreeNode> = {};

  files.forEach((file) => {
    const parts = file.path.split('/');
    let current = root;

    parts.forEach((part, index) => {
      if (!current[part]) {
        current[part] = {
          name: part,
          path: parts.slice(0, index + 1).join('/'),
          type: index === parts.length - 1 ? 'file' : 'folder',
          status: file.status,
          children: {},
        };
      }
      if (current[part].children) {
        current = current[part].children!;
      }
    });
  });

  // Flatten single-child folder chains
  return flattenSingleChildFolders(Object.values(root));
}

// Flatten consecutive folders with single children into a single node
function flattenSingleChildFolders(nodes: FileTreeNode[]): FileTreeNode[] {
  return nodes.map(node => {
    if (node.type === 'file') {
      return node;
    }

    // Check if this folder has only one child and that child is also a folder
    const children = node.children ? Object.values(node.children) : [];
    
    if (children.length === 1 && children[0] && children[0].type === 'folder') {
      // Merge this folder with its single child
      const child = children[0];
      const flattened = flattenSingleChildFolders([{
        ...child,
        name: `${node.name}/${child.name}`,
        path: child.path,
        type: child.type,
        status: child.status,
        children: child.children,
      }]);
      return flattened[0]!;
    }

    // Otherwise, recursively process children
    if (node.children) {
      const newChildren: Record<string, FileTreeNode> = {};
      children.forEach(child => {
        if (child) {
          const flattened = flattenSingleChildFolders([child]);
          if (flattened[0]) {
            newChildren[flattened[0].name] = flattened[0];
          }
        }
      });
      node.children = newChildren;
    }

    return node;
  }).filter((node): node is FileTreeNode => node !== undefined);
}

export interface FileTreeNode {
  name: string;
  path: string;
  type: 'file' | 'folder';
  status: string;
  children?: Record<string, FileTreeNode>;
}

export function flattenFileTree(nodes: FileTreeNode[]): FileTreeNode[] {
  const result: FileTreeNode[] = [];
  
  function traverse(nodes: FileTreeNode[], depth: number = 0) {
    nodes.forEach(node => {
      result.push({ ...node, depth } as any);
      if (node.children) {
        traverse(Object.values(node.children), depth + 1);
      }
    });
  }
  
  traverse(nodes);
  return result;
}
