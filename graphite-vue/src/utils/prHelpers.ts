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
 */
export function getSizeBadgeClass(totalLines: number): string {
  const size = getPRSize(totalLines);
  const classes = {
    'XS': 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30',
    'S': 'bg-teal-500/20 text-teal-400 border border-teal-500/30',
    'M': 'bg-blue-500/20 text-blue-400 border border-blue-500/30',
    'L': 'bg-yellow-500/20 text-yellow-400 border border-yellow-500/30',
    'XL': 'bg-red-500/20 text-red-400 border border-red-500/30',
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
  const classes: Record<string, string> = {
    AwaitingReview: 'hover:border-blue-500/30',
    Approved: 'hover:border-green-500/30',
    Reviewed: 'hover:border-purple-500/30',
    ChangesRequested: 'hover:border-orange-500/30',
    Draft: 'hover:border-gray-500/30',
  };
  return classes[status] || 'hover:border-slate-600/30';
}

/**
 * Get shadow color class based on status for hover effect
 */
export function getStatusShadowClass(status: string): string {
  const classes: Record<string, string> = {
    AwaitingReview: 'hover:shadow-blue-900/20',
    Approved: 'hover:shadow-green-900/20',
    Reviewed: 'hover:shadow-purple-900/20',
    ChangesRequested: 'hover:shadow-orange-900/20',
    Draft: 'hover:shadow-gray-900/20',
  };
  return classes[status] || 'hover:shadow-slate-900/20';
}
