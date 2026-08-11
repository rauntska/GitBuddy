import type { FileDiff } from '../types';
import { parsePatch } from './diffHelpers';

const MARKDOWN_EXTENSIONS = /\.(md|markdown|mdx)$/i;

export function isMarkdownFile(file: FileDiff): boolean {
  if (file.language && file.language.toLowerCase() === 'markdown') {
    return true;
  }
  return MARKDOWN_EXTENSIONS.test(file.path ?? '');
}

export function canReconstruct(file: FileDiff): boolean {
  if (file.status === 'deleted') return false;
  return Boolean(file.patch && file.patch.trim().length > 0);
}

export interface DiffLineNumbers {
  added: Set<number>;
  deleted: Set<number>;
}

export function getDiffLineNumbers(file: FileDiff): DiffLineNumbers {
  const added = new Set<number>();
  const deleted = new Set<number>();

  if (!file.patch) return { added, deleted };

  const hunks = parsePatch(file.patch);
  for (const hunk of hunks) {
    for (const line of hunk.lines) {
      if (line.type === 'add' && typeof line.newLineNumber === 'number') {
        added.add(line.newLineNumber);
      } else if (line.type === 'delete' && typeof line.oldLineNumber === 'number') {
        deleted.add(line.oldLineNumber);
      }
    }
  }

  return { added, deleted };
}
