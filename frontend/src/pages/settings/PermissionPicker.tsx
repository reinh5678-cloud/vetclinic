export interface Permission {
  code: string;
  group: string;
  label: string;
}

export default function PermissionPicker({
  catalog,
  selected,
  onToggle,
  disabled = false,
}: {
  catalog: Permission[];
  selected: string[];
  onToggle: (code: string) => void;
  disabled?: boolean;
}) {
  const groups = catalog.reduce<Record<string, Permission[]>>((acc, p) => {
    (acc[p.group] ??= []).push(p);
    return acc;
  }, {});

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {Object.entries(groups).map(([group, items]) => (
        <div key={group} className="rounded-xl border border-slate-200 p-3">
          <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">{group}</p>
          <div className="space-y-2">
            {items.map((p) => (
              <label key={p.code} className="flex items-start gap-2 text-sm text-slate-700">
                <input
                  type="checkbox"
                  className="checkbox mt-0.5"
                  checked={selected.includes(p.code)}
                  disabled={disabled}
                  onChange={() => onToggle(p.code)}
                />
                <span>{p.label}</span>
              </label>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
