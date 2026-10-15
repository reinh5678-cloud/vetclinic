import SettingsSection from './SettingsSection';
import { useSettingsSection } from './useSettingsSection';
import type { AgendaSettings } from '../../api/settings';

const WEEKDAYS = [
  { value: 'Monday', label: 'Lunes' },
  { value: 'Tuesday', label: 'Martes' },
  { value: 'Wednesday', label: 'Miércoles' },
  { value: 'Thursday', label: 'Jueves' },
  { value: 'Friday', label: 'Viernes' },
  { value: 'Saturday', label: 'Sábado' },
  { value: 'Sunday', label: 'Domingo' },
];

export default function AgendaPanel() {
  const { value, update, save, err, msg, busy } = useSettingsSection<AgendaSettings>('agenda');

  const toggleDay = (day: string) => {
    if (!value) return;
    const next = value.closedWeekdays.includes(day)
      ? value.closedWeekdays.filter((d) => d !== day)
      : [...value.closedWeekdays, day];
    update('closedWeekdays', next);
  };

  return (
    <SettingsSection
      title="Agenda"
      subtitle="Horario de atención, duración de turnos y días cerrados."
      err={err}
      msg={msg}
      onSave={save}
      busy={busy}
    >
      {value ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="field">
            <label className="label">Apertura</label>
            <input type="time" className="input" value={value.openingTime} onChange={(e) => update('openingTime', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Cierre</label>
            <input type="time" className="input" value={value.closingTime} onChange={(e) => update('closingTime', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Intervalo de agenda (min)</label>
            <input type="number" min={5} max={240} className="input" value={value.slotMinutes} onChange={(e) => update('slotMinutes', Number(e.target.value))} />
          </div>
          <div className="field">
            <label className="label">Duración por defecto (min)</label>
            <input type="number" min={5} className="input" value={value.defaultDurationMinutes} onChange={(e) => update('defaultDurationMinutes', Number(e.target.value))} />
          </div>
          <div className="field">
            <label className="label">Antelación máxima (días)</label>
            <input type="number" min={1} max={730} className="input" value={value.maxAdvanceDays} onChange={(e) => update('maxAdvanceDays', Number(e.target.value))} />
          </div>
          <div className="field sm:col-span-2">
            <label className="label">Días cerrados</label>
            <div className="flex flex-wrap gap-3">
              {WEEKDAYS.map((d) => (
                <label key={d.value} className="flex items-center gap-2 text-sm text-slate-600">
                  <input
                    type="checkbox"
                    className="checkbox"
                    checked={value.closedWeekdays.includes(d.value)}
                    onChange={() => toggleDay(d.value)}
                  />
                  {d.label}
                </label>
              ))}
            </div>
          </div>
        </div>
      ) : (
        <p className="text-sm text-slate-400">Cargando…</p>
      )}
    </SettingsSection>
  );
}
