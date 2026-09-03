/** Student picker row — mirrors `StudentListItemDto` from `GET /api/v2/students`. */
export interface Student {
  id: number;
  name: string;
  registrationNumber: string;
  isActive: boolean;
}

export interface CurrentStudent {
  id: number;
  name: string;
  registrationNumber: string;
  gpa: number;
  isActive: boolean;
}
