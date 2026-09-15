import { onMounted, onUnmounted, ref, watch, type Ref } from 'vue';

/** Height of the app-level header — `sticky top-20` in PRDetail's markup. */
const APP_HEADER_PX = 80;

/**
 * Height the condensed strip adds to the PR header when it appears. This is the hysteresis
 * margin — see below. Keep in sync with `pr-context-strip.vue`'s `py-1.5` + border row.
 */
const STRIP_PX = 32;

/**
 * Tracks whether the context rail has scrolled out from under the sticky chrome, so the header
 * can swap in a condensed version of it.
 *
 * The subtlety: showing the strip makes the header taller, which shifts everything below it up.
 * With a naive threshold that shift can push the sentinel back into view, which hides the strip,
 * which shifts content back down — a flicker loop. So the threshold already includes STRIP_PX:
 * by the time we condense, the sentinel is far enough gone that growing the header cannot bring
 * it back.
 */
export function useCondensedRail(sentinel: Ref<HTMLElement | null>) {
  const condensed = ref(false);
  let observer: IntersectionObserver | null = null;

  const currentHeaderPx = () => {
    const raw = getComputedStyle(document.documentElement).getPropertyValue('--pr-header-h');
    const parsed = Number.parseFloat(raw);
    return Number.isFinite(parsed) ? parsed : 108;
  };

  const connect = () => {
    disconnect();
    if (!sentinel.value || typeof IntersectionObserver === 'undefined') return;

    // Subtract the strip's own height so condensing cannot un-condense itself.
    const offset = APP_HEADER_PX + currentHeaderPx() + STRIP_PX;

    observer = new IntersectionObserver(
      ([entry]) => {
        if (entry) condensed.value = !entry.isIntersecting;
      },
      { rootMargin: `-${offset}px 0px 0px 0px`, threshold: 0 }
    );
    observer.observe(sentinel.value);
  };

  const disconnect = () => {
    observer?.disconnect();
    observer = null;
  };

  // The header wraps to a second row on narrow widths, which changes the offset. Only recompute
  // while expanded — reconnecting mid-scroll in the condensed state is what invites flicker.
  const handleResize = () => {
    if (!condensed.value) connect();
  };

  // The rail mounts with the PR data, so the sentinel arrives after this composable does.
  watch(sentinel, (el) => {
    if (el) connect();
    else disconnect();
  });

  onMounted(() => {
    connect();
    window.addEventListener('resize', handleResize);
  });

  onUnmounted(() => {
    disconnect();
    window.removeEventListener('resize', handleResize);
  });

  return { condensed };
}
