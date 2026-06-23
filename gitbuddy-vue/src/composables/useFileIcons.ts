interface FileIconInfo {
  icon: string;
  color: string;
}

const iconMap: Record<string, FileIconInfo> = {
  ts: { icon: 'TS', color: 'text-blue-400' },
  tsx: { icon: 'TSX', color: 'text-blue-400' },
  js: { icon: 'JS', color: 'text-yellow-400' },
  jsx: { icon: 'JSX', color: 'text-yellow-400' },
  vue: { icon: 'VUE', color: 'text-green-400' },
  css: { icon: 'CSS', color: 'text-purple-400' },
  scss: { icon: 'SCSS', color: 'text-pink-400' },
  html: { icon: 'HTML', color: 'text-orange-400' },
  json: { icon: '{ }', color: 'text-yellow-300' },
  md: { icon: 'MD', color: 'text-slate-400' },
  py: { icon: 'PY', color: 'text-blue-300' },
  java: { icon: 'J', color: 'text-red-400' },
  cs: { icon: 'C#', color: 'text-purple-400' },
  go: { icon: 'GO', color: 'text-cyan-400' },
  rs: { icon: 'RS', color: 'text-orange-400' },
  rb: { icon: 'RB', color: 'text-red-400' },
  php: { icon: 'PHP', color: 'text-purple-400' },
  cpp: { icon: 'C++', color: 'text-blue-400' },
  c: { icon: 'C', color: 'text-blue-400' },
  sql: { icon: 'SQL', color: 'text-blue-300' },
  sh: { icon: 'SH', color: 'text-green-400' },
  yaml: { icon: 'YML', color: 'text-red-300' },
  yml: { icon: 'YML', color: 'text-red-300' },
  svg: { icon: 'SVG', color: 'text-yellow-400' },
  png: { icon: 'IMG', color: 'text-green-400' },
  jpg: { icon: 'IMG', color: 'text-green-400' },
  jpeg: { icon: 'IMG', color: 'text-green-400' },
  gif: { icon: 'IMG', color: 'text-green-400' },
  ico: { icon: 'IMG', color: 'text-green-400' },
  lock: { icon: 'LOCK', color: 'text-yellow-500' },
  env: { icon: 'ENV', color: 'text-yellow-500' },
  gitignore: { icon: 'GIT', color: 'text-orange-500' },
};

const defaultIcon: FileIconInfo = { icon: 'FILE', color: 'text-slate-500' };

export function useFileIcons() {
  const getFileIcon = (filename: string): FileIconInfo => {
    const basename = filename.split('/').pop() || filename;
    
    if (basename.startsWith('.env')) {
      return iconMap.env || defaultIcon;
    }
    
    if (basename === '.gitignore') {
      return iconMap.gitignore || defaultIcon;
    }
    
    const ext = basename.split('.').pop()?.toLowerCase();
    if (ext && iconMap[ext]) {
      return iconMap[ext];
    }
    
    return defaultIcon;
  };

  const getStatusIndicator = (status: string): { color: string; label: string } => {
    const statusMap: Record<string, { color: string; label: string }> = {
      added: { color: 'bg-green-500', label: 'Added' },
      modified: { color: 'bg-yellow-500', label: 'Modified' },
      deleted: { color: 'bg-red-500', label: 'Deleted' },
      renamed: { color: 'bg-blue-500', label: 'Renamed' },
    };
    return statusMap[status] ?? statusMap.modified ?? { color: 'bg-slate-500', label: 'Modified' };
  };

  return {
    getFileIcon,
    getStatusIndicator,
  };
}
