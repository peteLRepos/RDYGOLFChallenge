import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../api/client';
import type { Resource } from '../api/types';
import './CoursesPage.css';

// GolfCart isn't browsable as its own tee sheet — it's an add-on to a booking (coming later, see README).
const BROWSABLE_TYPES: Resource['type'][] = ['TeeTime', 'DrivingRangeBay', 'LessonSlot', 'Simulator'];

export function CoursesPage() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    api
      .get<Resource[]>('/api/resources')
      .then((all) => setResources(all.filter((r) => BROWSABLE_TYPES.includes(r.type))))
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load courses.'))
      .finally(() => setIsLoading(false));
  }, []);

  return (
    <main className="page">
      <p className="subtitle">Pick a course to see today's tee sheet.</p>

      {isLoading && <p>Loading courses…</p>}
      {error && <p className="error">{error}</p>}

      <ul className="course-grid">
        {resources.map((resource) => (
          <li key={resource.id}>
            <Link to={`/courses/${resource.id}`} className="course-card">
              <span className="resource-type">{resource.type}</span>
              <h2>{resource.name}</h2>
              <p>
                {resource.openingTime.slice(0, 5)}–{resource.closingTime.slice(0, 5)} ·{' '}
                {resource.slotDurationMinutes} min slots
              </p>
              {resource.pricePerPlayer != null && (
                <p className="course-price">€{resource.pricePerPlayer.toFixed(2)} / player</p>
              )}
            </Link>
          </li>
        ))}
      </ul>
    </main>
  );
}
