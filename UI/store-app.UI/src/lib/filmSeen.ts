/**
 * Films that play once on arrival are remembered for the browser tab, by their clip's address:
 * the next visit to the page opens on the picture straight away.
 */
const seenKey = (video: string) => `film-seen:${video}`;

export const wasSeen = (video: string) => {
  try {
    return sessionStorage.getItem(seenKey(video)) !== null;
  } catch {
    return false;
  }
};

export const markSeen = (video: string) => {
  try {
    sessionStorage.setItem(seenKey(video), '1');
  } catch {
    // private mode or blocked storage: the film just plays again next time
  }
};
