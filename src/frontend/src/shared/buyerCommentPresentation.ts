const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

/** Source wall-clock components, deliberately not passed to Date or converted to a local/UTC zone. */
export function formatBuyerCommentMetadata(recordedAt?: string | null, author?: string | null): string {
  const match = recordedAt?.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):\d{2}(?:\.\d+)?$/);
  let date = 'Date unknown';
  if (match) {
    const [, year, month, day, hour, minute] = match;
    const h = Number(hour);
    const m = Number(month);
    if (m >= 1 && m <= 12 && h <= 23 && Number(day) >= 1 && Number(day) <= 31 && Number(minute) <= 59) {
      date = `${months[m - 1]} ${Number(day)}, ${year} · ${h % 12 || 12}:${minute} ${h >= 12 ? 'PM' : 'AM'}`;
    }
  }
  return author ? `${date} · ${author}` : date;
}
