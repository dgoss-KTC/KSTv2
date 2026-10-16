import { useEffect, useState } from 'react';
import { localIsoDate } from '../shared/localDate';

/** UI invalidation only; requests also revalidate the real clock at each prepare/save boundary. */
export function useLocalBusinessDate() {
  const [date, setDate] = useState(() => localIsoDate(new Date()));
  useEffect(() => {
    let timer: ReturnType<typeof setTimeout>;
    const update = () => {
      const now = new Date(); setDate(localIsoDate(now));
      clearTimeout(timer);
      timer = setTimeout(update, new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1).getTime() - now.getTime() + 20);
    };
    const schedule = new Date();
    timer = setTimeout(update, new Date(schedule.getFullYear(), schedule.getMonth(), schedule.getDate() + 1).getTime() - schedule.getTime() + 20);
    window.addEventListener('focus', update); document.addEventListener('visibilitychange', update);
    return () => { clearTimeout(timer); window.removeEventListener('focus', update); document.removeEventListener('visibilitychange', update); };
  }, []);
  return date;
}
