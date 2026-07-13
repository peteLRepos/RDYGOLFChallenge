import { useAuth } from '../auth/AuthContext';
import './Header.css';

export function Header({ title }: { title: string }) {
  const { user, logout } = useAuth();

  return (
    <header className="site-header">
      <span className="site-title">{title}</span>
      <div className="site-header-actions">
        <span className="site-user">{user?.name}</span>
        <button type="button" className="header-button" onClick={logout}>
          Log out
        </button>
      </div>
    </header>
  );
}
