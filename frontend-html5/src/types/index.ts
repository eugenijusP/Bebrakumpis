export interface User {
  id: string;
  username: string;
  role: 'Admin' | 'User' | 'Guest';
}

export interface House {
  id: string;
  name: string;
  bookingColor: string;
  reservedColor: string;
  createdAt: string;
}
