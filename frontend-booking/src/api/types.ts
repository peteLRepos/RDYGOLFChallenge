export type ResourceType =
  | 'TeeTime'
  | 'DrivingRangeBay'
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
  accountFound: boolean;
  newPassword: string | null;
}

export type PaymentMethod = 'Cash' | 'Card' | 'SerialTicket';

export type BookingStatus = 'Pending' | 'Ready' | 'Cancelled';

export interface UserSearchResult {
  id: string;
  name: string;
  handicap: number;
}

export interface BookingPlayer {
  userId: string;
  name: string;
  handicap: number;
  paymentMethod: PaymentMethod;
  addedByUserId: string;
}

export interface Booking {
  id: string;
  resourceId: string;
  resourceName: string;
  bookerId: string;
  customerName: string;
  customerEmail: string;
  start: string;
  end: string;
  isPaid: boolean;
  status: BookingStatus;
  playerCount: number;
  combinedHandicap: number;
  players: BookingPlayer[];
  totalPrice: number;
  createdAt: string;
  cartId: string | null;
  cartName: string | null;
}

export interface PlayerSelection {
  userId: string;
  paymentMethod: PaymentMethod;
}

export interface CreateBookingRequest {
  resourceId: string;
  start: string;
  end: string;
  players: PlayerSelection[];
  wantsCart?: boolean;
}

export interface CartAvailability {
  isAvailable: boolean;
}

export interface AddPlayerRequest {
  userId: string;
  paymentMethod: PaymentMethod;
}

export interface JoinWaitlistRequest {
  resourceId: string;
  slotStart: string;
}

export interface WaitlistEntry {
  id: string;
  resourceId: string;
  resourceName: string;
  slotStart: string;
  userId: string;
  userName: string;
  userEmail: string;
  createdAt: string;
}
