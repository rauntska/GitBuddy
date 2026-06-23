import { ref, watch, onMounted, onUnmounted, type Ref } from 'vue';
import { marked } from 'marked';
import apiClient from '../utils/api';

marked.setOptions({ breaks: true, gfm: true });

const proxyPath = '/images/proxy';

// Matches GitHub-hosted image URLs whether they appear in HTML attributes or markdown links.
const githubAssetPattern =
  /https:\/\/(?:github\.com\/user-attachments\/[^)\s"'<>]+|[a-z0-9\-]+\.githubusercontent\.com\/[^)\s"'<>]+|raw\.githubusercontent\.com\/[^)\s"'<>]+|objects\.githubusercontent\.com\/[^)\s"'<>]+)/g;

function collectGitHubImageUrls(html: string): string[] {
  const found = new Set<string>();
  for (const m of html.matchAll(githubAssetPattern)) {
    if (m[0]) found.add(m[0]);
  }
  return [...found];
}

export interface UseProxiedHtmlOptions {
  isMarkdown?: boolean;
}

export function useProxiedHtml(
  source: Ref<string | undefined> | (() => string | undefined),
  options: UseProxiedHtmlOptions = {}
) {
  const html = ref('');
  const loading = ref(false);
  const blobUrls = new Map<string, string>();
  let currentRendered = '';

  const resolve = typeof source === 'function' ? source : () => source.value;

  const revokeAll = () => {
    blobUrls.forEach((u) => URL.revokeObjectURL(u));
    blobUrls.clear();
  };

  const applyBlobs = () => {
    if (!currentRendered) return;
    let finalHtml = currentRendered;
    blobUrls.forEach((blob, original) => {
      finalHtml = finalHtml.split(original).join(blob);
    });
    html.value = finalHtml;
  };

  const process = async () => {
    const content = resolve() ?? '';
    if (!content) {
      currentRendered = '';
      html.value = '';
      return;
    }

    const prepared = content.replace(/\r\n/g, '\n');
    currentRendered = options.isMarkdown ? (marked.parse(prepared) as string) : prepared;
    // Render immediately with original URLs so the UI doesn't flash empty while blobs load.
    applyBlobs();

    const imageUrls = collectGitHubImageUrls(currentRendered);
    if (imageUrls.length === 0) return;

    loading.value = true;
    try {
      await Promise.all(
        imageUrls.map(async (imageUrl) => {
          if (blobUrls.has(imageUrl)) return;
          try {
            const response = await apiClient.get(`${proxyPath}?url=${encodeURIComponent(imageUrl)}`, {
              responseType: 'blob',
            });
            blobUrls.set(imageUrl, URL.createObjectURL(response.data));
          } catch (err) {
            console.error('Failed to load image:', imageUrl, err);
          }
        })
      );
      applyBlobs();
    } finally {
      loading.value = false;
    }
  };

  watch(() => resolve(), process);
  onMounted(process);
  onUnmounted(revokeAll);

  return { html, loading, refresh: process };
}
