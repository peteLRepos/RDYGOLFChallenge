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
  isActive: boolean;
}

export interface Booking {
  id: string;
  resourceId: string;
  resourceName: string;
  start: string;
  end: string;
  customerName: string;
  customerEmail: string;
  status: 'Confirmed' | 'Cancelled';
  createdAt: string;
}
