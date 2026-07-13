export type ResourceType =
  | 'TeeTime'
  | 'DrivingRangeBay'
  | 'GolfCart'
  | 'LessonSlot'
  | 'Simulator';

export interface Resource {
  id: string;
  name: string;
  type: ResourceType;
  slotDurationMinutes: number;
  openingTime: string;
  closingTime: string;
  pricePerPlayer: number | null;
  isActive: boolean;
}

/** BookingId/PlayerCount/CombinedHandicap are only set when the slot isn't available. */
export interface TimeSlot {
  start: string;
  end: string;
  isAvailable: boolean;
  bookingId: string | null;
  playerCount: number | null;
  combinedHandicap: number | null;
}

export interface User {
  id: string;
  name: string;
  email: string;
  isAdmin: boolean;
  isActive: boolean;
  handicap: number;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  handicap?: number | null;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  newPassword: string;
}
