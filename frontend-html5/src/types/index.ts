export interface User {
  id: string;
  username: string;
  role: 'Admin' | 'User' | 'Guest';
}

export interface UserRecord {
  id: string;
  firstName: string;
  lastName: string;
  username: string;
  role: 'Admin' | 'User';
  isActive: boolean;
  createdAt: string;
}

export interface House {
  id: string;
  name: string;
  bookingColor: string;
  reservedColor: string;
  createdAt: string;
}

export interface Booking {
  id: string;
  houseId: string;
  type: 'B' | 'R';
  startDate: string;
  endDate: string;
  displayText: string;
  notes: string | null;
  createdByName: string | null;
  createdAt: string | null;
}
