import type { ReactNode } from 'react';

interface Props {
  title: string;
  subtitle?: string;
  err?: string;
  msg?: string;
  onSave?: () => void;
  busy?: boolean;
  children: ReactNode;
}

export default function SettingsSection({ title, subtitle, err, msg, onSave, busy, children }: Props) {
  return (
    <section className="card">
      {err && <div className="alert alert-error m-4">{err}</div>}
      {msg && <div className="alert alert-success m-4">{msg}</div>}

      <div className="card-header">
        <div>
          <h3 className="section-title">{title}</h3>
          {subtitle && <p className="mt-0.5 text-xs text-slate-500">{subtitle}</p>}
        </div>
        {onSave && (
          <button onClick={onSave} disabled={busy} className="btn btn-primary btn-sm">
            {busy ? 'Guardando…' : 'Guardar cambios'}
          </button>
        )}
      </div>

      <div className="card-body">{children}</div>
    </section>
  );
}
