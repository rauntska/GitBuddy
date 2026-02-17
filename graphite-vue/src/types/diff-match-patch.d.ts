declare module 'diff-match-patch' {
  export default class DiffMatchPatch {
    diff_main(text1: string, text2: string): [number, string][];
    diff_cleanupSemantic(diffs: [number, string][]): void;
    diff_levenshtein(diffs: [number, string][]): number;
    diff_prettyHtml(diffs: [number, string][]): string;
    patch_make(text1: string, text2: string): object[];
    patch_apply(patches: object[], text: string): [string, boolean[]];
  }
}
