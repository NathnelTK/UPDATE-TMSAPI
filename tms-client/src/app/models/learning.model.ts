export interface AttendanceRecord { id: number; studentId: number; courseId: number; date: string; status: string; remarks: string | null; }
export interface GradeRecord { id: number; assessmentId: number; studentId: number; assessmentTitle: string; score: number; maximumScore: number; weight: number; remarks: string | null; gradedAt: string; }
export interface LearningSummary { studentId: number; totalSessions: number; presentSessions: number; absentSessions: number; attendancePercentage: number; weightedScore: number; unreadNotifications: number; }
export interface NotificationRecord { id: number; title: string; message: string; type: string; isRead: boolean; createdAt: string; }
export interface GrantApplication { id: number; studentId: number; grantProgramId: number; grantProgramName: string; status: string; eligibilityScore: number; applicationDate: string; reviewNotes: string | null; }
export interface GrantProgram { id: number; name: string; description: string; fundingOrganization: string; totalBudget: number; amountPerStudent: number; allocatedAmount: number; remainingBudget: number; isActive: boolean; }
