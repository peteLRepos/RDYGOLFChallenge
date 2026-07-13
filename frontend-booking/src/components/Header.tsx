import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { AuthModals, type AuthMode } from './AuthModals';
import './Header.css';

export function Header({ title }: { title: string }) {
  const { user, isAuthenticated, logout } = useAuth();
  const [authMode, setAuthMode] = useState<AuthMode | null>(null);

  return (
    <header className="site-header">
      <span className="site-title">{title}</span>
      <div className="site-header-actions">
        {isAuthenticated ? (
          <>
            <span className="site-user">{user!.name}</span>
            <button type="button" className="header-button" onClick={logout}>
              Log out
            </button>
          </>
        ) : (
          <>
            <button type="button" className="header-button" onClick={() => setAuthMode('login')}>
              Log in
            </button>
            <button
              type="button"
              className="header-button header-button-primary"
              onClick={() => setAuthMode('register')}
            >
              Register
            </button>
          </>
        )}
      </div>

      {authMode && (
        <AuthModals mode={authMode} onModeChange={setAuthMode} onClose={() => setAuthMode(null)} />
      )}
    </header>
  );
}
