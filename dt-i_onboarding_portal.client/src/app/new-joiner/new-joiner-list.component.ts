import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NewJoinerService, NewJoiner } from '../services/new-joiner.service';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime } from 'rxjs/operators';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-new-joiner-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './new-joiner-list.component.html',
  styleUrls: ['./new-joiner-list.component.css'],
  providers: [NewJoinerService]
})
export class NewJoinerListComponent implements OnInit, OnDestroy {
  newJoiners: NewJoiner[] = [];
  filteredJoiners: NewJoiner[] = [];
  isLoading = false;
  error = '';
  success = '';

  searchTerm = '';
  startDateFrom: string = '';
  startDateTo: string = '';

  sortBy: 'name' | 'date' | 'created' = 'date';
  sortOrder: 'asc' | 'desc' = 'desc';

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<void>();

  constructor(
    private newJoinerService: NewJoinerService,
    public auth: AuthService
  ) {}

  ngOnInit() {
    this.loadNewJoiners();

    // Debounce search input
    this.searchSubject
      .pipe(
        debounceTime(300),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.applyFilters();
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadNewJoiners() {
    this.isLoading = true;
    this.error = '';

    const startFrom = this.startDateFrom ? new Date(this.startDateFrom) : undefined;
    const startTo = this.startDateTo ? new Date(this.startDateTo) : undefined;

    this.newJoinerService
      .getAllNewJoiners(this.searchTerm || undefined, startFrom, startTo)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (joiners: NewJoiner[]) => {
          this.newJoiners = joiners;
          this.applyFilters();
          this.isLoading = false;
        },
        error: (err: any) => {
          console.error('Error loading new joiners:', err);
          this.error = 'Failed to load new joiners. Please try again.';
          this.isLoading = false;
        }
      });
  }

  applyFilters() {
    this.filteredJoiners = [...this.newJoiners];

    // Apply sorting
    switch (this.sortBy) {
      case 'name':
        this.filteredJoiners.sort((a, b) =>
          this.sortOrder === 'asc'
            ? a.fullName.localeCompare(b.fullName)
            : b.fullName.localeCompare(a.fullName)
        );
        break;
      case 'date':
        this.filteredJoiners.sort((a, b) =>
          this.sortOrder === 'asc'
            ? new Date(a.startDate).getTime() - new Date(b.startDate).getTime()
            : new Date(b.startDate).getTime() - new Date(a.startDate).getTime()
        );
        break;
      case 'created':
        this.filteredJoiners.sort((a, b) =>
          this.sortOrder === 'asc'
            ? new Date(a.createdAtUtc).getTime() - new Date(b.createdAtUtc).getTime()
            : new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime()
        );
        break;
    }
  }

  onSearchChange() {
    this.searchSubject.next();
  }

  onFilterChange() {
    this.loadNewJoiners();
  }

  onSortChange() {
    this.applyFilters();
  }

  onDelete(id: number, name: string) {
    if (confirm(`Are you sure you want to delete ${name}?`)) {
      this.newJoinerService
        .deleteNewJoiner(id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.success = `${name} deleted successfully`;
            this.loadNewJoiners();
            setTimeout(() => (this.success = ''), 3000);
          },
          error: (err: any) => {
            console.error('Error deleting new joiner:', err);
            this.error = 'Failed to delete new joiner';
          }
        });
    }
  }

  onResendEmail(id: number, name: string) {
    if (confirm(`Resend welcome email to ${name}?`)) {
      this.newJoinerService
        .resendWelcomeEmail(id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.success = `Welcome email sent to ${name}`;
            this.loadNewJoiners();
            setTimeout(() => (this.success = ''), 3000);
          },
          error: (err: any) => {
            console.error('Error resending email:', err);
            this.error = 'Failed to send welcome email';
          }
        });
    }
  }

  canDelete(): boolean {
    return this.auth.isAdmin();
  }

  canResendEmail(): boolean {
    return this.auth.isAdmin();
  }

  getEmailStatus(joiner: NewJoiner): string {
    if (joiner.lastSendError) {
      return 'Failed';
    }
    if (joiner.welcomeEmailSentAtUtc) {
      return 'Sent';
    }
    return 'Pending';
  }

  getEmailStatusClass(joiner: NewJoiner): string {
    if (joiner.lastSendError) {
      return 'status-failed';
    }
    if (joiner.welcomeEmailSentAtUtc) {
      return 'status-sent';
    }
    return 'status-pending';
  }

  formatDate(date: Date | string): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}
