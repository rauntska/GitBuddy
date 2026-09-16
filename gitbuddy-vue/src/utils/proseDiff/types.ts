/**
 * Shared types for the prose diff pipeline.
 *
 * Pipeline: parseBlocks (both revisions) -> pairBlocks -> classify -> renderPairedBlock
 *
 * Deliberately free of markdown-specific assumptions where possible, so the pairing layer
 * can later serve .txt/.rst without rewriting it.
 */

export type BlockKind =
  | 'heading'
  | 'paragraph'
  | 'listItem'
  | 'code'
  | 'table'
  | 'blockquote'
  | 'html'
  | 'other';

export interface Block {
  /** Index in document order within its own revision. */
  id: number;
  kind: BlockKind;
  /** Heading level 1-6; undefined for non-headings. */
  depth?: number;
  /** Source text of the block, verbatim. */
  raw: string;
  /** Inline text with markup stripped by the lexer, used for similarity scoring. */
  text: string;
  /** 1-based, inclusive. */
  startLine: number;
  endLine: number;
  /** Ancestor heading titles, innermost last. A heading includes itself. */
  sectionPath: string[];
  /**
   * False for blocks where a word-level diff would be noise rather than signal
   * (code, tables, mermaid, raw html). Such blocks are still paired, but a changed
   * one is marked at block granularity.
   */
  wordDiffable: boolean;
}

export interface Section {
  /** Ancestor titles including this heading. Identity key for pairing. */
  path: string[];
  title: string;
  depth: number;
  headingBlockId: number;
  /** Ids of every block under this heading, excluding the heading itself. */
  blockIds: number[];
}

export interface BlockDoc {
  blocks: Block[];
  sections: Section[];
  /**
   * Heading depth the section map should display — the shallowest depth that occurs
   * more than once, else the shallowest present. Keeps a single `# Title` from
   * collapsing the whole document into one section.
   */
  mapDepth: number;
}

export type PairStatus = 'added' | 'removed' | 'changed' | 'unchanged';

export interface PairedBlock {
  old?: Block;
  new?: Block;
  status: PairStatus;
}

export type ChangeClass = 'unchanged' | 'formatting-only' | 'content';

export interface ClassifiedPair extends PairedBlock {
  changeClass: ChangeClass;
  /** Populated only when changeClass is 'formatting-only'. */
  suppressionReason?: string;
}

export interface SectionSummary {
  path: string[];
  title: string;
  /** DOM id of the rendered section, for scroll targeting. */
  domId: string;
  status: PairStatus;
  /** Count of content-changed pairs in this section. */
  changedCount: number;
  /** Total pairs rendered in this section (excludes suppressed). */
  blockCount: number;
  suppressedCount: number;
}
