import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { api, ApiError } from '../api/client';
import type { Cart } from '../api/types';
import './CartsPage.css';

export function CartsPage() {
  const [carts, setCarts] = useState<Cart[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [newName, setNewName] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const [pendingId, setPendingId] = useState<string | null>(null);

  const load = useCallback(() => {
    setIsLoading(true);
    setError(null);
    api
      .get<Cart[]>('/api/admin/carts')
      .then(setCarts)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load carts.'))
      .finally(() => setIsLoading(false));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const createCart = async (e: FormEvent) => {
    e.preventDefault();
    setActionError(null);
    setIsCreating(true);
    try {
      await api.post('/api/admin/carts', { name: newName.trim() });
      setNewName('');
      load();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not create the cart.');
    } finally {
      setIsCreating(false);
    }
  };

  const toggleActive = async (cart: Cart) => {
    setActionError(null);
    setPendingId(cart.id);
    try {
      await api.patch(`/api/admin/carts/${cart.id}/active`, !cart.isActive);
      load();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not update the cart.');
    } finally {
      setPendingId(null);
    }
  };

  const deleteCart = async (cart: Cart) => {
    setActionError(null);
    setPendingId(cart.id);
    try {
      await api.delete(`/api/admin/carts/${cart.id}`);
      load();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not delete the cart.');
    } finally {
      setPendingId(null);
    }
  };

  return (
    <main className="page">
      <h1 className="carts-title">Golf Carts</h1>
      <p className="subtitle">The fleet available for players to add to a booking.</p>

      <form className="add-cart-form" onSubmit={createCart}>
        <input
          type="text"
          placeholder="Cart name (e.g. Cart 4)"
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          required
        />
        <button type="submit" className="price-save-button" disabled={isCreating}>
          {isCreating ? 'Adding…' : 'Add cart'}
        </button>
      </form>

      {isLoading && <p>Loading carts…</p>}
      {error && <p className="error">{error}</p>}
      {actionError && <p className="error">{actionError}</p>}
      {!isLoading && !error && carts.length === 0 && <p>No carts have been added yet.</p>}

      <ul className="cart-list">
        {carts.map((cart) => {
          const isBusy = pendingId === cart.id;
          return (
            <li key={cart.id} className="cart-row">
              <span className="cart-name">{cart.name}</span>
              <span className={'cart-status' + (cart.isActive ? ' active' : ' inactive')}>
                {cart.isActive ? 'Active' : 'Disabled'}
              </span>
              <div className="cart-actions">
                <button type="button" disabled={isBusy} onClick={() => toggleActive(cart)}>
                  {cart.isActive ? 'Disable' : 'Enable'}
                </button>
                <button type="button" className="cart-delete" disabled={isBusy} onClick={() => deleteCart(cart)}>
                  Remove
                </button>
              </div>
            </li>
          );
        })}
      </ul>
    </main>
  );
}
