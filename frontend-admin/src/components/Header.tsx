import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import './Header.css';

export function Header({ title }: { title: string }) {
  const { user, logout } = useAuth();

  return (
    <header className="site-header">
      <span className="site-title">{title}</span>
      <nav className="site-nav">
        <Link to="/" className="header-button">
          Resources
        </Link>
        <Link to="/carts" className="header-button">
          Carts
        </Link>
        <Link to="/waitlist" className="header-button">
          Waitlist
        </Link>
      </nav>
      <div className="site-header-actions">
        <span className="site-user">{user?.name}</span>
        <button type="button" className="header-button" onClick={logout}>
          Log out
        </button>
      </div>
    </header>
  );
}
