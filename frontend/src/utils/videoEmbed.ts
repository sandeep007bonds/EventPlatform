/**
 * Parses a YouTube or Vimeo URL into an embeddable iframe src. Deliberately narrow — just these
 * two well-known providers' URL shapes, no general oEmbed lookup or extra dependency. Returns
 * `null` for anything else, so callers can fall back to a plain external link instead of
 * silently dropping the video.
 */
export function toEmbedUrl(url: string): string | null {
  try {
    const parsed = new URL(url);
    const host = parsed.hostname.replace(/^www\./, '');

    if (host === 'youtube.com' || host === 'm.youtube.com') {
      if (parsed.pathname === '/watch') {
        const videoId = parsed.searchParams.get('v');
        return videoId ? `https://www.youtube.com/embed/${videoId}` : null;
      }
      if (parsed.pathname.startsWith('/embed/')) {
        return url;
      }
      return null;
    }

    if (host === 'youtu.be') {
      const videoId = parsed.pathname.slice(1);
      return videoId ? `https://www.youtube.com/embed/${videoId}` : null;
    }

    if (host === 'vimeo.com') {
      const videoId = parsed.pathname.slice(1);
      return /^\d+$/.test(videoId) ? `https://player.vimeo.com/video/${videoId}` : null;
    }

    if (host === 'player.vimeo.com') {
      return url;
    }

    return null;
  } catch {
    return null;
  }
}
