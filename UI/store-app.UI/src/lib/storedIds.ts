/**
 * A short list of product ids kept in this browser (recently viewed, compared). Anything
 * unreadable - storage off, a value from an older version - reads as an empty list.
 */
export const loadIds = (key: string, max: number): number[] => {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(key) ?? '[]');
    return Array.isArray(parsed) ? parsed.filter((id): id is number => Number.isInteger(id) && id > 0).slice(0, max) : [];
  } catch {
    return [];
  }
};

export const saveIds = (key: string, ids: number[]): void => {
  try {
    localStorage.setItem(key, JSON.stringify(ids));
  } catch {
    // Storage may be full or disabled; the list then lasts for the page only
  }
};
