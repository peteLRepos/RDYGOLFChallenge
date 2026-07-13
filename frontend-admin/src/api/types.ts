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

export interface UpdateResourceRequest {
  name: string;
  slotDurationMinutes: number;
  openingTime: string;
  closingTime: string;
  pricePerPlayer: number | null;
}

export type PaymentMethod = 'Cash' | 'Card' | 'SerialTicket';

export type BookingStatus = 'Pending' | 'Ready' | 'Cancelled';

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

export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  newPassword: string;
}
