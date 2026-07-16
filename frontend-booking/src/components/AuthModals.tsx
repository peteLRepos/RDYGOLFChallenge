import { useState, type FormEvent } from 'react';
import { Modal } from './Modal';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../api/client';

export type AuthMode = 'login' | 'register' | 'forgot';

interface AuthModalsProps {
  mode: AuthMode;
  onModeChange: (mode: AuthMode) => void;
  onClose: () => void;
}

export function AuthModals({ mode, onModeChange, onClose }: AuthModalsProps) {
  if (mode === 'login') return <LoginForm onModeChange={onModeChange} onClose={onClose} />;
  if (mode === 'register') return <RegisterForm onModeChange={onModeChange} onClose={onClose} />;
  return <ForgotPasswordForm onModeChange={onModeChange} onClose={onClose} />;
}

interface FormProps {
  onModeChange: (mode: AuthMode) => void;
  onClose: () => void;
}

function LoginForm({ onModeChange, onClose }: FormProps) {
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login({ email, password });
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Login failed.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal title="Log in" onClose={onClose}>
      <form className="modal-form" onSubmit={handleSubmit}>
        <label>
          Email
          <input
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        {error && <p className="modal-error">{error}</p>}
        <button type="submit" className="modal-submit" disabled={isSubmitting}>
          {isSubmitting ? 'Logging in…' : 'Log in'}
        </button>
      </form>
      <p className="modal-switch">
        <button type="button" onClick={() => onModeChange('forgot')}>
          Forgot password?
        </button>
        {' · '}
        <button type="button" onClick={() => onModeChange('register')}>
          Need an account? Register
        </button>
      </p>
    </Modal>
  );
}

function RegisterForm({ onModeChange, onClose }: FormProps) {
  const { register } = useAuth();
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [handicap, setHandicap] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await register({
        name,
        email,
        password,
        handicap: handicap.trim() === '' ? null : Number(handicap),
      });
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Registration failed.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal title="Create an account" onClose={onClose}>
      <form className="modal-form" onSubmit={handleSubmit}>
        <label>
          Name
          <input autoComplete="name" required value={name} onChange={(e) => setName(e.target.value)} />
        </label>
        <label>
          Email
          <input
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        <label>
          Handicap (optional)
          <input
            type="number"
            min={-10}
            max={56}
            value={handicap}
            onChange={(e) => setHandicap(e.target.value)}
          />
        </label>
        {error && <p className="modal-error">{error}</p>}
        <button type="submit" className="modal-submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creating account…' : 'Create account'}
        </button>
      </form>
      <p className="modal-switch">
        <button type="button" onClick={() => onModeChange('login')}>
          Already have an account? Log in
        </button>
      </p>
    </Modal>
  );
}

function ForgotPasswordForm({ onModeChange, onClose }: FormProps) {
  const { forgotPassword } = useAuth();
  const [email, setEmail] = useState('');
  const [result, setResult] = useState<{ accountFound: boolean; newPassword: string | null } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      setResult(await forgotPassword(email));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not reset password.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal title="Forgot password" onClose={onClose}>
      {result ? (
        <>
          <p className="modal-success">
            {result.accountFound ? (
              <>
                Your new password is: <strong>{result.newPassword}</strong>
              </>
            ) : (
              "If an account with that email exists, its password has been reset."
            )}
          </p>
          <p className="modal-switch">
            <button type="button" onClick={() => onModeChange('login')}>
              Back to log in
            </button>
          </p>
        </>
      ) : (
        <form className="modal-form" onSubmit={handleSubmit}>
          <label>
            Email
            <input
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </label>
          {error && <p className="modal-error">{error}</p>}
          <button type="submit" className="modal-submit" disabled={isSubmitting}>
            {isSubmitting ? 'Generating…' : 'Reset password'}
          </button>
        </form>
      )}
    </Modal>
  );
}
