import { useEffect, useState } from 'react';
import { Routes, Route } from 'react-router-dom';
import { api, ApiError } from './api/client';
import type { Resource } from './api/types';
import { Header } from './components/Header';
import './App.css';

function HomePage() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    api
      .get<Resource[]>('/api/resources')
      .then(setResources)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load resources.'))
      .finally(() => setIsLoading(false));
  }, []);

  return (
    <main className="page">
      <p className="subtitle">Browse what's available and book a time slot.</p>

      {isLoading && <p>Loading resources…</p>}
      {error && <p className="error">{error}</p>}

      <ul className="resource-list">
        {resources.map((resource) => (
          <li key={resource.id} className="resource-card">
            <span className="resource-type">{resource.type}</span>
            <h2>{resource.name}</h2>
            <p>
              {resource.openingTime}–{resource.closingTime} · {resource.slotDurationMinutes} min slots
            </p>
          </li>
        ))}
      </ul>
    </main>
  );
}

function App() {
  return (
    <>
      <Header title="Golf Club Booking" />
      <Routes>
        <Route path="/" element={<HomePage />} />
      </Routes>
    </>
  );
}

export default App;
