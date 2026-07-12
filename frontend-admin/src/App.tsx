import { useEffect, useState } from 'react';
import { api, ApiError } from './api/client';
import { getAdminKey, setAdminKey } from './api/adminKey';
import type { Booking, Resource } from './api/types';
import './App.css';

function App() {
  const [adminKey, setAdminKeyInput] = useState(getAdminKey());
  const [resources, setResources] = useState<Resource[]>([]);
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [error, setError] = useState<string | null>(null);

  const loadData = () => {
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
  };

  useEffect(() => {
    if (adminKey) loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSaveKey = (e: React.FormEvent) => {
    e.preventDefault();
    setAdminKey(adminKey);
    loadData();
  };

  return (
    <main className="page">
      <h1>Golf Club Admin</h1>

      <form className="key-form" onSubmit={handleSaveKey}>
        <label htmlFor="admin-key">Admin key</label>
        <input
          id="admin-key"
          type="password"
          value={adminKey}
          onChange={(e) => setAdminKeyInput(e.target.value)}
        />
        <button type="submit">Connect</button>
      </form>

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

export default App;
