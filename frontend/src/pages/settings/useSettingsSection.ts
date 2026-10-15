import { useCallback, useEffect, useState } from 'react';
import { api } from '../../api/client';

export function useSettingsSection<T extends object>(section: string) {
  const [value, setValue] = useState<T | null>(null);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const [busy, setBusy] = useState(false);

  const load = useCallback(() => {
    setErr('');
    api<T>(`/settings/${section}`)
      .then(setValue)
      .catch((e) => setErr((e as Error).message));
  }, [section]);

  useEffect(() => {
    load();
  }, [load]);

  const update = useCallback(<K extends keyof T>(key: K, v: T[K]) => {
    setValue((prev) => (prev ? { ...prev, [key]: v } : prev));
  }, []);

  const save = useCallback(async () => {
    if (!value) return;
    setErr('');
    setMsg('');
    setBusy(true);
    try {
      const saved = await api<T>(`/settings/${section}`, { method: 'PUT', body: JSON.stringify(value) });
      setValue(saved);
      setMsg('Cambios guardados.');
    } catch (e) {
      setErr((e as Error).message);
    } finally {
      setBusy(false);
    }
  }, [section, value]);

  return { value, setValue, update, save, err, msg, busy };
}

export function parseList(value: string): string[] {
  return value
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0);
}
