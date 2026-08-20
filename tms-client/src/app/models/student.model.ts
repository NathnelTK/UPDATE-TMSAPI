/** Student picker row — mirrors `StudentListItemDto` from `GET /api/v2/students`. */
export interface Student {
  id: number;
  name: string;
  registrationNumber: string;
  isActive: boolean;
}
