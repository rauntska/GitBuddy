import type { DiffHunk, DiffLine, AlignedRow, InlineDiffSegment, FileLineContent, ExpandPosition } from '../types';
import DiffMatchPatch from 'diff-match-patch';

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

/**
 * Aligns diff lines for side-by-side view.
 * Pairs delete lines with add lines on the same row,
 * with spacers for unbalanced changes.
 */
export function alignDiffLines(lines: DiffLine[]): AlignedRow[] {
  const result: AlignedRow[] = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];
    if (!line) {
      i++;
      continue;
    }

    if (line.type === 'context') {
      // Context lines appear on both sides, perfectly aligned
      result.push({
        leftLine: {
          type: 'context',
          content: line.content,
          lineNumber: line.oldLineNumber,
          isExpanded: line.isExpanded,
        },
        rightLine: {
          type: 'context',
          content: line.content,
          lineNumber: line.newLineNumber,
          isExpanded: line.isExpanded,
        },
      });
      i++;
    } else if (line.type === 'delete') {
      // Collect all consecutive delete lines
      const deleteLines: DiffLine[] = [];
      while (i < lines.length && lines[i] && lines[i]!.type === 'delete') {
        deleteLines.push(lines[i]!);
        i++;
      }

      // Collect all consecutive add lines that follow
      const addLines: DiffLine[] = [];
      while (i < lines.length && lines[i] && lines[i]!.type === 'add') {
        addLines.push(lines[i]!);
        i++;
      }

      // Pair them up
      const maxLen = Math.max(deleteLines.length, addLines.length);
      for (let j = 0; j < maxLen; j++) {
        const delLine = deleteLines[j];
        const addLine = addLines[j];

        let inlineDiff: { oldSegments: InlineDiffSegment[]; newSegments: InlineDiffSegment[] } | null = null;

        // Compute inline diff when we have both a delete and add line
        if (delLine && addLine) {
          inlineDiff = computeInlineDiff(delLine.content, addLine.content);
        }

        result.push({
          leftLine: delLine
            ? {
                type: 'delete',
                content: delLine.content,
                lineNumber: delLine.oldLineNumber,
                inlineDiff: inlineDiff?.oldSegments,
              }
            : {
                type: 'spacer',
                content: '',
              },
          rightLine: addLine
            ? {
                type: 'add',
                content: addLine.content,
                lineNumber: addLine.newLineNumber,
                inlineDiff: inlineDiff?.newSegments,
              }
            : {
                type: 'spacer',
                content: '',
              },
        });
      }
    } else if (line.type === 'add') {
      // Pure additions (no preceding deletes)
      result.push({
        leftLine: {
          type: 'spacer',
          content: '',
        },
        rightLine: {
          type: 'add',
          content: line.content,
          lineNumber: line.newLineNumber,
        },
      });
      i++;
    } else {
      i++;
    }
  }

  return result;
}

/**
 * Computes word-level inline diff between two lines
 */
export function computeInlineDiff(
  oldLine: string,
  newLine: string
): { oldSegments: InlineDiffSegment[]; newSegments: InlineDiffSegment[] } {
  const dmp = new DiffMatchPatch();
  const diffs = dmp.diff_main(oldLine, newLine);
  dmp.diff_cleanupSemantic(diffs);

  const oldSegments: InlineDiffSegment[] = [];
  const newSegments: InlineDiffSegment[] = [];

  for (const [op, text] of diffs) {
    // op: -1 = delete, 0 = equal, 1 = insert
    if (op === -1) {
      oldSegments.push({ type: 'delete', content: text });
    } else if (op === 1) {
      newSegments.push({ type: 'add', content: text });
    } else {
      // Equal - appears in both
      oldSegments.push({ type: 'equal', content: text });
      newSegments.push({ type: 'equal', content: text });
    }
  }

  return { oldSegments, newSegments };
}

/**
 * Escapes HTML special characters
 */
function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

/**
 * Renders inline diff segments as HTML
 */
export function renderInlineDiffSegments(segments: InlineDiffSegment[]): string {
  let result = '';
  for (const segment of segments) {
    const escaped = escapeHtml(segment.content);
    if (segment.type === 'delete') {
      result += `<span class="inline-diff-delete">${escaped}</span>`;
    } else if (segment.type === 'add') {
      result += `<span class="inline-diff-add">${escaped}</span>`;
    } else {
      result += escaped;
    }
  }
  return result;
}

export interface GapInfo {
  oldStart: number;
  oldEnd: number;
  newStart: number;
  newEnd: number;
  oldLineCount: number;
  newLineCount: number;
}

export function calculateExpandRange(
  hunks: DiffHunk[],
  position: ExpandPosition,
  hunkIndex: number,
  lineCount: number = 25
): { oldStart: number | null; oldEnd: number | null; newStart: number | null; newEnd: number | null } {
  if (hunks.length === 0) {
    return { oldStart: null, oldEnd: null, newStart: null, newEnd: null };
  }

  if (position === 'before') {
    const firstHunk = hunks[0];
    if (!firstHunk) {
      return { oldStart: null, oldEnd: null, newStart: null, newEnd: null };
    }
    const oldStart = Math.max(1, firstHunk.oldStart - lineCount);
    const oldEnd = firstHunk.oldStart - 1;
    const newStart = Math.max(1, firstHunk.newStart - lineCount);
    const newEnd = firstHunk.newStart - 1;
    
    return {
      oldStart: oldEnd >= oldStart ? oldStart : null,
      oldEnd: oldEnd >= oldStart ? oldEnd : null,
      newStart: newEnd >= newStart ? newStart : null,
      newEnd: newEnd >= newStart ? newEnd : null,
    };
  }

  if (position === 'after') {
    const lastHunk = hunks[hunks.length - 1];
    if (!lastHunk) {
      return { oldStart: null, oldEnd: null, newStart: null, newEnd: null };
    }
    const oldEnd = lastHunk.oldStart + lastHunk.oldLines - 1 + lineCount;
    const oldStart = lastHunk.oldStart + lastHunk.oldLines;
    const newEnd = lastHunk.newStart + lastHunk.newLines - 1 + lineCount;
    const newStart = lastHunk.newStart + lastHunk.newLines;
    
    return {
      oldStart,
      oldEnd,
      newStart,
      newEnd,
    };
  }

  if (position === 'between' && hunkIndex >= 0 && hunkIndex < hunks.length - 1) {
    const currentHunk = hunks[hunkIndex];
    const nextHunk = hunks[hunkIndex + 1];
    if (!currentHunk || !nextHunk) {
      return { oldStart: null, oldEnd: null, newStart: null, newEnd: null };
    }
    
    const oldStart = currentHunk.oldStart + currentHunk.oldLines;
    const oldEnd = nextHunk.oldStart - 1;
    const newStart = currentHunk.newStart + currentHunk.newLines;
    const newEnd = nextHunk.newStart - 1;
    
    return {
      oldStart: oldEnd >= oldStart ? oldStart : null,
      oldEnd: oldEnd >= oldStart ? oldEnd : null,
      newStart: newEnd >= newStart ? newStart : null,
      newEnd: newEnd >= newStart ? newEnd : null,
    };
  }

  return { oldStart: null, oldEnd: null, newStart: null, newEnd: null };
}

export function getGapBetweenHunks(hunks: DiffHunk[], hunkIndex: number): GapInfo | null {
  if (hunkIndex < 0 || hunkIndex >= hunks.length - 1) {
    return null;
  }

  const currentHunk = hunks[hunkIndex];
  const nextHunk = hunks[hunkIndex + 1];
  if (!currentHunk || !nextHunk) {
    return null;
  }

  const oldStart = currentHunk.oldStart + currentHunk.oldLines;
  const oldEnd = nextHunk.oldStart - 1;
  const newStart = currentHunk.newStart + currentHunk.newLines;
  const newEnd = nextHunk.newStart - 1;

  const oldLineCount = Math.max(0, oldEnd - oldStart + 1);
  const newLineCount = Math.max(0, newEnd - newStart + 1);

  if (oldLineCount === 0 && newLineCount === 0) {
    return null;
  }

  return {
    oldStart,
    oldEnd,
    newStart,
    newEnd,
    oldLineCount,
    newLineCount,
  };
}

export function mergeExpandedLines(
  hunks: DiffHunk[],
  oldLines: FileLineContent[],
  newLines: FileLineContent[],
  position: ExpandPosition,
  hunkIndex: number
): DiffHunk[] {
  if (oldLines.length === 0 && newLines.length === 0) {
    return hunks;
  }

  const newHunks = [...hunks.map(h => ({ ...h, lines: [...h.lines] }))];

  const createContextLines = (oldLineData: FileLineContent[], newLineData: FileLineContent[]): DiffLine[] => {
    const lines: DiffLine[] = [];
    const maxLines = Math.max(oldLineData.length, newLineData.length);
    
    for (let i = 0; i < maxLines; i++) {
      const oldLine = oldLineData[i];
      const newLine = newLineData[i];
      
      if (oldLine && newLine) {
        lines.push({
          type: 'context',
          content: newLine.content,
          oldLineNumber: oldLine.lineNumber,
          newLineNumber: newLine.lineNumber,
          isExpanded: true,
        });
      } else if (oldLine) {
        lines.push({
          type: 'context',
          content: oldLine.content,
          oldLineNumber: oldLine.lineNumber,
          isExpanded: true,
        });
      } else if (newLine) {
        lines.push({
          type: 'context',
          content: newLine.content,
          newLineNumber: newLine.lineNumber,
          isExpanded: true,
        });
      }
    }
    return lines;
  };

  const expandedLines = createContextLines(oldLines, newLines);

  if (position === 'before' && newHunks.length > 0) {
    const firstHunk = newHunks[0];
    if (firstHunk) {
      firstHunk.lines = [...expandedLines, ...firstHunk.lines];
      if (oldLines.length > 0 && oldLines[0]) {
        firstHunk.oldStart = oldLines[0].lineNumber;
      }
      if (newLines.length > 0 && newLines[0]) {
        firstHunk.newStart = newLines[0].lineNumber;
      }
      firstHunk.oldLines += oldLines.length;
      firstHunk.newLines += newLines.length;
    }
  } else if (position === 'after' && newHunks.length > 0) {
    const lastHunk = newHunks[newHunks.length - 1];
    if (lastHunk) {
      lastHunk.lines = [...lastHunk.lines, ...expandedLines];
      lastHunk.oldLines += oldLines.length;
      lastHunk.newLines += newLines.length;
    }
  } else if (position === 'between' && hunkIndex >= 0 && hunkIndex < newHunks.length - 1) {
    const currentHunk = newHunks[hunkIndex];
    const nextHunk = newHunks[hunkIndex + 1];
    if (currentHunk && nextHunk) {
      currentHunk.lines = [...currentHunk.lines, ...expandedLines, ...nextHunk.lines];
      currentHunk.oldLines = currentHunk.oldLines + oldLines.length + nextHunk.oldLines;
      currentHunk.newLines = currentHunk.newLines + newLines.length + nextHunk.newLines;
      
      newHunks.splice(hunkIndex + 1, 1);
    }
  }

  return newHunks;
}
