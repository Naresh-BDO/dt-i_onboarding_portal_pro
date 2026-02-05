import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';

export interface NewJoiner {
  id: number;
  fullName: string;
  email: string;
  department?: string;
  managerName?: string;
  startDate: Date;
  createdAtUtc: Date;
  welcomeEmailSentAtUtc?: Date;
  lastSendError?: string;
}

export interface CreateNewJoinerDto {
  fullName: string;
  email: string;
  department?: string;
  managerName?: string;
  startDate: Date;
}

export interface UpdateNewJoinerDto {
  fullName: string;
  department?: string;
  managerName?: string;
  startDate: Date;
}

export interface NewJoinerStats {
  totalNewJoiners: number;
  emailSent: number;
  emailFailed: number;
  upcomingJoiners: number;
}

@Injectable({
  providedIn: 'root'
})
export class NewJoinerService {
  private apiUrl = 'https://localhost:7107/api/new-joiners';
  private newJoinersSubject = new BehaviorSubject<NewJoiner[]>([]);
  public newJoiners$ = this.newJoinersSubject.asObservable();

  private statsSubject = new BehaviorSubject<NewJoinerStats | null>(null);
  public stats$ = this.statsSubject.asObservable();

  constructor(private http: HttpClient) {}

  // Create a new joiner
  createNewJoiner(joiner: CreateNewJoinerDto): Observable<any> {
    return this.http.post(`${this.apiUrl}`, joiner);
  }

  // Get all new joiners with optional filtering
  getAllNewJoiners(
    search?: string,
    startDateFrom?: Date,
    startDateTo?: Date
  ): Observable<NewJoiner[]> {
    let params = new HttpParams();

    if (search) {
      params = params.set('search', search);
    }
    if (startDateFrom) {
      params = params.set('startDateFrom', startDateFrom.toISOString());
    }
    if (startDateTo) {
      params = params.set('startDateTo', startDateTo.toISOString());
    }

    return this.http.get<NewJoiner[]>(`${this.apiUrl}`, { params });
  }

  // Get single new joiner by ID
  getNewJoinerById(id: number): Observable<NewJoiner> {
    return this.http.get<NewJoiner>(`${this.apiUrl}/${id}`);
  }

  // Update a new joiner
  updateNewJoiner(id: number, joiner: UpdateNewJoinerDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, joiner);
  }

  // Delete a new joiner
  deleteNewJoiner(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  // Resend welcome email
  resendWelcomeEmail(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/resend-email`, {});
  }

  // Get statistics
  getStatistics(): Observable<NewJoinerStats> {
    return this.http.get<NewJoinerStats>(`${this.apiUrl}/stats/summary`);
  }

  // Load all new joiners and update subject
  loadNewJoiners(
    search?: string,
    startDateFrom?: Date,
    startDateTo?: Date
  ): void {
    this.getAllNewJoiners(search, startDateFrom, startDateTo).subscribe({
      next: (joiners) => {
        this.newJoinersSubject.next(joiners);
      },
      error: (err) => {
        console.error('Error loading new joiners:', err);
        this.newJoinersSubject.next([]);
      }
    });
  }

  // Load statistics and update subject
  loadStatistics(): void {
    this.getStatistics().subscribe({
      next: (stats) => {
        this.statsSubject.next(stats);
      },
      error: (err) => {
        console.error('Error loading statistics:', err);
        this.statsSubject.next(null);
      }
    });
  }
}
