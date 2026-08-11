type MermaidApi = typeof import('mermaid').default;

let mermaidModule: MermaidApi | null = null;
let initPromise: Promise<MermaidApi> | null = null;

async function loadMermaid(): Promise<MermaidApi> {
  if (mermaidModule) return mermaidModule;
  if (!initPromise) {
    initPromise = import('mermaid').then((mod) => {
      const mermaid = mod.default;
      mermaid.initialize({
        startOnLoad: false,
        theme: 'dark',
        themeVariables: {
          darkMode: true,
          background: '#020617',
          primaryColor: '#1e293b',
          primaryTextColor: '#e2e8f0',
          primaryBorderColor: '#334155',
          lineColor: '#64748b',
          secondaryColor: '#172033',
          tertiaryColor: '#0f172a',
          nodeBorder: '#475569',
          edgeLabelBackground: '#0f172a',
          fontFamily: 'ui-monospace, SFMono-Regular, monospace',
        },
      });
      mermaidModule = mermaid;
      return mermaid;
    });
  }
  return initPromise;
}

const MERMAID_SELECTOR = 'pre > code.language-mermaid';

export function hasMermaidBlocks(container: HTMLElement): boolean {
  return container.querySelector(MERMAID_SELECTOR) !== null;
}

export async function renderMermaidBlocks(container: HTMLElement): Promise<void> {
  if (!hasMermaidBlocks(container)) return;

  const mermaid = await loadMermaid();

  const codeBlocks = Array.from(container.querySelectorAll<HTMLElement>(MERMAID_SELECTOR));
  const targets: HTMLElement[] = [];

  for (const codeBlock of codeBlocks) {
    const pre = codeBlock.parentElement;
    if (!pre || pre.tagName.toLowerCase() !== 'pre') continue;

    const graphDefinition = codeBlock.textContent ?? '';
    const diagramHost = document.createElement('div');
    diagramHost.className = 'mermaid-diagram';
    pre.replaceWith(diagramHost);
    diagramHost.textContent = graphDefinition;
    targets.push(diagramHost);
  }

  if (targets.length === 0) return;

  try {
    await mermaid.run({ nodes: targets });
  } catch (err) {
    console.error('Mermaid rendering failed:', err);
  }
}
