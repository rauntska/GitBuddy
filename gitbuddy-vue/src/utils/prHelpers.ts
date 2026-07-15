/**
 * Utility functions for PR row enhancements
 */

/**
 * Calculate PR size based on total lines changed
 */
export function getPRSize(totalLines: number): string {
  if (totalLines < 50) return 'XS';
  if (totalLines < 200) return 'S';
  if (totalLines < 500) return 'M';
  if (totalLines < 1000) return 'L';
  return 'XL';
}

/**
 * Get Tailwind CSS classes for PR size badge
 * Dense/pro: text-only, no fill background.
 */
export function getSizeBadgeClass(totalLines: number): string {
  const size = getPRSize(totalLines);
  const classes = {
    'XS': 'text-emerald-400',
    'S': 'text-teal-400',
    'M': 'text-slate-300',
    'L': 'text-amber-400',
    'XL': 'text-red-400',
  } as const;
  return classes[size as keyof typeof classes];
}

/**
 * Check if PR is stale (older than 7 days)
 */
export function isStale(createdAt: string): boolean {
  const created = new Date(createdAt);
  const now = new Date();
  const daysDiff = (now.getTime() - created.getTime()) / (1000 * 60 * 60 * 24);
  return daysDiff > 7;
}

/**
 * Format PR age in human-readable format
 */
export function formatAge(createdAt: string): string {
  const created = new Date(createdAt);
  const now = new Date();
  const daysDiff = Math.floor((now.getTime() - created.getTime()) / (1000 * 60 * 60 * 24));
  
  if (daysDiff === 0) return 'today';
  if (daysDiff === 1) return '1 day';
  if (daysDiff < 7) return `${daysDiff} days`;
  const weeks = Math.floor(daysDiff / 7);
  if (weeks < 4) return `${weeks} ${weeks === 1 ? 'week' : 'weeks'}`;
  const months = Math.floor(daysDiff / 30);
  return `${months} ${months === 1 ? 'month' : 'months'}`;
}

/**
 * Get color class for age indicator
 */
export function getAgeColorClass(createdAt: string): string {
  const created = new Date(createdAt);
  const now = new Date();
  const daysDiff = (now.getTime() - created.getTime()) / (1000 * 60 * 60 * 24);
  
  if (daysDiff > 7) return 'text-red-400';
  if (daysDiff > 3) return 'text-yellow-400';
  if (daysDiff > 1) return 'text-slate-400';
  return 'text-slate-500';
}

/**
 * Format date in short format (e.g., "Jan 20" or "Jan 20, 2023")
 */
export function formatDate(dateString: string): string {
  const date = new Date(dateString);
  const currentYear = new Date().getFullYear();
  const dateYear = date.getFullYear();
  
  return date.toLocaleDateString('en-US', { 
    month: 'short', 
    day: 'numeric',
    year: dateYear !== currentYear ? 'numeric' : undefined 
  });
}

/**
 * Get border color class based on status for hover effect
 */
export function getStatusBorderClass(status: string): string {
  // Dense/pro look: single neutral hover border, no per-status glow.
  void status;
  return 'hover:border-slate-600';
}

/**
 * Get shadow color class based on status for hover effect.
 * @deprecated Dense/pro restyle removes colored hover glows. Returns empty string.
 */
export function getStatusShadowClass(_status: string): string {
  return '';
}

/**
 * Faint row tint per status. Replaces the old neutral row fill so groups
 * become visually distinct without saturated backgrounds.
 */
export function getStatusTintClass(status: string): string {
  const classes: Record<string, string> = {
    AwaitingReview: 'bg-blue-950/20',
    Approved: 'bg-emerald-950/20',
    Reviewed: 'bg-violet-950/20',
    ChangesRequested: 'bg-orange-950/20',
    Draft: 'bg-slate-800/30',
    Merged: 'bg-violet-950/10',
    Closed: 'bg-slate-800/20',
  };
  return classes[status] || 'bg-slate-800/30';
}

/**
 * One-char status glyph + tailwind text color for compact badges.
 */
export function getStatusGlyph(status: string): { char: string; color: string } {
  const map: Record<string, { char: string; color: string }> = {
    AwaitingReview: { char: '●', color: 'text-blue-400' },
    Approved: { char: '✓', color: 'text-emerald-400' },
    Reviewed: { char: '◐', color: 'text-violet-400' },
    ChangesRequested: { char: '✕', color: 'text-orange-400' },
    Draft: { char: '○', color: 'text-slate-500' },
    Merged: { char: 'Ⓜ', color: 'text-violet-400' },
    Closed: { char: '—', color: 'text-slate-500' },
  };
  return map[status] || { char: '–', color: 'text-slate-500' };
}

/**
 * One-char CI glyph + tailwind text color.
 * checksStatus values: SUCCESS, FAILURE, PENDING, NONE (or empty).
 */
export function getCIGlyph(checksStatus: string | null | undefined): { char: string; color: string } {
  const map: Record<string, { char: string; color: string }> = {
    SUCCESS: { char: '✓', color: 'text-emerald-400' },
    FAILURE: { char: '✗', color: 'text-red-400' },
    PENDING: { char: '⋯', color: 'text-amber-400' },
  };
  return map[(checksStatus || '').toUpperCase()] || { char: '–', color: 'text-slate-500' };
}

/**
 * Compact reviewer-state string. Each reviewer becomes a single char:
 *   Approved → ●, Commented → ◐, ChangesRequested → ✕, others → ○
 * Returned value is monospace-friendly for column alignment.
 */
export function getReviewerDots(reviews: { state: string }[]): string {
  const map: Record<string, string> = {
    Approved: '●',
    Commented: '◐',
    ChangesRequested: '✕',
    Dismissed: '○',
  };
  return reviews.map(r => map[r.state] || '○').join('');
}

/**
 * Priority scale: 0=Low, 1=Normal, 2=High, 3=Urgent.
 * `null`/`undefined` are treated as Normal (derived default).
 */
export const PRIORITY_LOW = 0;
export const PRIORITY_NORMAL = 1;
export const PRIORITY_HIGH = 2;
export const PRIORITY_URGENT = 3;

export function getPriorityLabel(priority: number | null | undefined): string {
  switch (priority) {
    case PRIORITY_LOW: return 'Low';
    case PRIORITY_HIGH: return 'High';
    case PRIORITY_URGENT: return 'Urgent';
    default: return 'Normal';
  }
}

/**
 * Short glyph shown in the dense row (one char for alignment).
 */
export function getPriorityGlyph(priority: number | null | undefined): string {
  switch (priority) {
    case PRIORITY_LOW: return '▽';
    case PRIORITY_HIGH: return '▲';
    case PRIORITY_URGENT: return '⏫';
    default: return '';
  }
}

/**
 * Tailwind text color for a priority level.
 */
export function getPriorityColor(priority: number | null | undefined): string {
  switch (priority) {
    case PRIORITY_LOW: return 'text-slate-500';
    case PRIORITY_HIGH: return 'text-amber-400';
    case PRIORITY_URGENT: return 'text-red-400';
    default: return 'text-slate-400';
  }
}
