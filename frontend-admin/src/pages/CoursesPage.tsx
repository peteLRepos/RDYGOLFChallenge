import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../api/client';
import type { Resource } from '../api/types';
import './CoursesPage.css';

export function CoursesPage() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    api
      .get<Resource[]>('/api/admin/resources')
      .then(setResources)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load resources.'))
      .finally(() => setIsLoading(false));
  }, []);

  return (
    <main className="page">
      <h1 className="courses-title">Resources</h1>

      {isLoading && <p>Loading resources…</p>}
      {error && <p className="error">{error}</p>}

      <ul className="course-grid">
        {resources.map((resource) => (
          <li key={resource.id}>
            <Link
              to={`/courses/${resource.id}`}
              className={'course-card' + (resource.isActive ? '' : ' course-card-inactive')}
            >
              <span className="resource-type">{resource.type}</span>
              <h2>{resource.name}</h2>
              <p>
                {resource.openingTime.slice(0, 5)}–{resource.closingTime.slice(0, 5)} ·{' '}
                {resource.slotDurationMinutes} min slots
              </p>
              <p className="course-price">
                {resource.pricePerPlayer != null ? `€${resource.pricePerPlayer.toFixed(2)} / player` : 'Unpriced'}
              </p>
              {!resource.isActive && <p className="course-inactive-label">Inactive</p>}
            </Link>
          </li>
        ))}
      </ul>
    </main>
  );
}
