import Prism from 'prismjs';
import 'prismjs/components/prism-typescript';
import 'prismjs/components/prism-javascript';
import 'prismjs/components/prism-csharp';
import 'prismjs/components/prism-css';
import 'prismjs/components/prism-markup'; // HTML
import 'prismjs/components/prism-markup-templating'; // Required for Vue
import 'prismjs/components/prism-json';
import 'prismjs/components/prism-markdown';
import 'prismjs/components/prism-python';
import 'prismjs/components/prism-java';
import 'prismjs/components/prism-go';
import 'prismjs/components/prism-rust';
import 'prismjs/components/prism-sql';
import 'prismjs/components/prism-bash';
import 'prismjs/components/prism-yaml';

export function highlightCode(code: string, language: string): string {
  const lang = getLanguageForHighlight(language);
  
  // For Vue files, use markup (HTML) highlighting
  const grammar = lang === 'vue' ? Prism.languages.markup : Prism.languages[lang];
  
  if (!grammar) {
    return escapeHtml(code);
  }
  
  try {
    return Prism.highlight(code, grammar, lang === 'vue' ? 'markup' : lang);
  } catch (e) {
    return escapeHtml(code);
  }
}

function getLanguageForHighlight(lang: string): string {
  const languageMap: Record<string, string> = {
    ts: 'typescript',
    tsx: 'typescript',
    js: 'javascript',
    jsx: 'javascript',
    vue: 'markup', // Vue templates are HTML-like
    cs: 'csharp',
    html: 'markup',
    xml: 'markup',
    md: 'markdown',
    py: 'python',
    rb: 'ruby',
    php: 'php',
    cpp: 'cpp',
    c: 'c',
    sh: 'bash',
    yml: 'yaml',
  };
  
  return languageMap[lang.toLowerCase()] || lang.toLowerCase();
}

function escapeHtml(text: string): string {
  const map: Record<string, string> = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#039;'
  };
  return text.replace(/[&<>"']/g, m => map[m] || m);
}

export function detectLanguageFromPath(path: string): string {
  const parts = path.split('.');
  if (parts.length === 1) return 'text';
  
  const ext = parts[parts.length - 1]?.toLowerCase() || '';
  if (!ext) return 'text';
  
  const languageMap: Record<string, string> = {
    ts: 'typescript',
    tsx: 'typescript',
    js: 'javascript',
    jsx: 'javascript',
    vue: 'vue',
    css: 'css',
    scss: 'scss',
    html: 'markup',
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
  return languageMap[ext] || 'text';
}
