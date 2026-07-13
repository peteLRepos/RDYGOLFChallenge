import { useEffect, useState } from 'react';
import { Routes, Route } from 'react-router-dom';
import { api, ApiError } from './api/client';
import type { Booking, Resource } from './api/types';
import { useAuth } from './auth/AuthContext';
import { LoginScreen } from './components/LoginScreen';
import { Header } from './components/Header';
import './App.css';

function DashboardPage() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setError(null);
    Promise.all([
      api.get<Resource[]>('/api/admin/resources'),
      api.get<Booking[]>('/api/admin/bookings'),
    ])
      .then(([r, b]) => {
        setResources(r);
        setBookings(b);
      })
      .catch((err) =>
        setError(err instanceof ApiError ? `${err.status}: ${err.message}` : 'Failed to load admin data.'),
      );
  }, []);

  return (
    <main className="page">
      {error && <p className="error">{error}</p>}

      <section>
        <h2>Resources ({resources.length})</h2>
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Type</th>
              <th>Hours</th>
              <th>Active</th>
            </tr>
          </thead>
          <tbody>
            {resources.map((r) => (
              <tr key={r.id}>
                <td>{r.name}</td>
                <td>{r.type}</td>
                <td>
                  {r.openingTime}–{r.closingTime}
                </td>
                <td>{r.isActive ? 'Yes' : 'No'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section>
        <h2>Bookings ({bookings.length})</h2>
        <table>
          <thead>
            <tr>
              <th>Resource</th>
              <th>When</th>
              <th>Customer</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {bookings.map((b) => (
              <tr key={b.id}>
                <td>{b.resourceName}</td>
                <td>
                  {b.start} – {b.end}
                </td>
                <td>
                  {b.customerName} ({b.customerEmail})
                </td>
                <td>{b.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </main>
  );
}

function App() {
  const { isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <LoginScreen />;
  }

  return (
    <>
      <Header title="Golf Club Admin" />
      <Routes>
        <Route path="/" element={<DashboardPage />} />
      </Routes>
    </>
  );
}

export default App;
