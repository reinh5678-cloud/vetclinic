import { useEffect, useState } from 'react';
import SettingsSection from './SettingsSection';
import { getSystemInfo, type SystemInfo } from '../../api/settings';

export default function SystemPanel() {
  const [system, setSystem] = useState<SystemInfo | null>(null);
  const [err, setErr] = useState('');

  useEffect(() => {
    getSystemInfo()
      .then(setSystem)
      .catch((e) => setErr((e as Error).message));
  }, []);

  return (
    <SettingsSection
      title="Sistema"
      subtitle="Información de solo lectura sobre el entorno de ejecución."
      err={err}
    >
      {system ? (
        <dl className="grid grid-cols-1 gap-x-4 gap-y-3 text-sm sm:grid-cols-2 lg:grid-cols-3">
          <div><dt className="text-xs text-slate-500">Entorno</dt><dd className="font-medium text-slate-800">{system.environment}</dd></div>
          <div><dt className="text-xs text-slate-500">Versión</dt><dd className="font-medium text-slate-800">{system.version}</dd></div>
          <div><dt className="text-xs text-slate-500">Base de datos</dt><dd className="font-medium text-slate-800">{system.database}</dd></div>
          <div className="sm:col-span-2 lg:col-span-3"><dt className="text-xs text-slate-500">Ruta de adjuntos</dt><dd className="font-medium text-slate-800 break-all">{system.attachmentsPath}</dd></div>
          <div><dt className="text-xs text-slate-500">Tamaño máx. adjunto</dt><dd className="font-medium text-slate-800">{system.maxAttachmentMb} MB</dd></div>
        </dl>
      ) : (
        <p className="text-sm text-slate-400">Cargando parámetros…</p>
      )}
    </SettingsSection>
  );
}
