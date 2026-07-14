import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { UserSearchResult } from '../api/types';
import './PlayerSearch.css';

const MIN_QUERY_LENGTH = 2;
const DEBOUNCE_MS = 250;

interface PlayerSearchProps {
  excludeUserIds: string[];
  onSelect: (user: UserSearchResult) => void;
  onCancel: () => void;
}

export function PlayerSearch({ excludeUserIds, onSelect, onCancel }: PlayerSearchProps) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<UserSearchResult[]>([]);

  useEffect(() => {
    if (query.trim().length < MIN_QUERY_LENGTH) {
      setResults([]);
      return;
    }

    const handle = setTimeout(() => {
      api
        .get<UserSearchResult[]>(`/api/users/search?q=${encodeURIComponent(query.trim())}`)
        .then((users) => setResults(users.filter((u) => !excludeUserIds.includes(u.id))))
        .catch(() => setResults([]));
    }, DEBOUNCE_MS);

    return () => clearTimeout(handle);
  }, [query, excludeUserIds]);

  return (
    <div className="player-search">
      <input
        autoFocus
        type="text"
        placeholder="Search players by name…"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
      />
      {results.length > 0 && (
        <ul className="player-search-results">
          {results.map((user) => (
            <li key={user.id}>
              <button type="button" onClick={() => onSelect(user)}>
                {user.name} <span className="player-search-handicap">hcp {user.handicap}</span>
              </button>
            </li>
          ))}
        </ul>
      )}
      {results.length === 0 && query.trim().length >= MIN_QUERY_LENGTH && (
        <p className="player-search-empty">No players found.</p>
      )}
      <button type="button" className="player-search-cancel" onClick={onCancel}>
        Cancel
      </button>
    </div>
  );
}
