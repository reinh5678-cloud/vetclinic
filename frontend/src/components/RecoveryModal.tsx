import { useState, type FormEvent } from 'react';
import Modal from './Modal';
import { getRecoveryQuestions, recoverPassword, type RecoveryQuestion } from '../api/recovery';

interface Props {
  onClose: () => void;
  onSuccess: () => void;
}

export default function RecoveryModal({ onClose, onSuccess }: Props) {
  const [step, setStep] = useState<1 | 2>(1);
  const [username, setUsername] = useState('');
  const [questions, setQuestions] = useState<RecoveryQuestion[]>([]);
  const [answers, setAnswers] = useState<string[]>([]);
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [noQuestions, setNoQuestions] = useState('');

  const loadQuestions = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setNoQuestions('');
    setBusy(true);
    try {
      const data = await getRecoveryQuestions(username.trim());
      setQuestions(data.questions);
      setAnswers(data.questions.map(() => ''));
      setStep(2);
    } catch (err) {
      const message = (err as Error).message;
      if (message.toLowerCase().includes('no se encontró') || message.toLowerCase().includes('preguntas')) {
        setNoQuestions('Este usuario no tiene preguntas de seguridad configuradas. Pide al administrador que restablezca tu contraseña desde Configuración → Usuarios.');
      } else {
        setError(message);
      }
    } finally {
      setBusy(false);
    }
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    if (password !== confirm) {
      setError('La nueva contraseña y su confirmación no coinciden.');
      return;
    }
    setBusy(true);
    try {
      await recoverPassword({ username: username.trim(), answers, newPassword: password });
      onSuccess();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal title="Recuperar contraseña" onClose={onClose}>
      {error && <div className="alert alert-error mb-3">{error}</div>}

      {step === 1 && (
        <form onSubmit={loadQuestions} className="space-y-3">
          <p className="text-sm text-slate-500">
            Ingresa tu usuario para responder tus preguntas de seguridad y definir una nueva contraseña.
          </p>
          {noQuestions && <div className="alert alert-error">{noQuestions}</div>}
          <div className="field">
            <label className="label">Usuario</label>
            <input
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              className="input"
              autoFocus
            />
          </div>
          <div className="flex justify-end gap-2 pt-1">
            <button type="button" className="btn btn-secondary" onClick={onClose}>Cancelar</button>
            <button type="submit" className="btn btn-primary" disabled={busy}>
              {busy ? 'Verificando…' : 'Continuar'}
            </button>
          </div>
        </form>
      )}

      {step === 2 && (
        <form onSubmit={submit} className="space-y-3">
          <p className="text-sm text-slate-500">Responde tus preguntas de seguridad y define una nueva contraseña.</p>
          {questions.map((q, i) => (
            <div className="field" key={q.questionId}>
              <label className="label">{q.text}</label>
              <input
                value={answers[i] ?? ''}
                onChange={(e) => setAnswers(answers.map((a, idx) => (idx === i ? e.target.value : a)))}
                required
                className="input"
              />
            </div>
          ))}
          <div className="field">
            <label className="label">Nueva contraseña</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="new-password"
              className="input"
            />
          </div>
          <div className="field">
            <label className="label">Confirmar nueva contraseña</label>
            <input
              type="password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              required
              autoComplete="new-password"
              className="input"
            />
          </div>
          <div className="flex justify-between gap-2 pt-1">
            <button type="button" className="btn btn-ghost" onClick={() => setStep(1)}>Atrás</button>
            <button type="submit" className="btn btn-primary" disabled={busy}>
              {busy ? 'Guardando…' : 'Restablecer contraseña'}
            </button>
          </div>
        </form>
      )}
    </Modal>
  );
}
