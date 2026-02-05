import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NewJoinerService, NewJoinerStats } from '../services/new-joiner.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  providers: [NewJoinerService]
})
export class DashboardComponent implements OnInit, OnDestroy {
  stats: NewJoinerStats | null = null;
  isLoading = false;
  error = '';

  private destroy$ = new Subject<void>();
  private refreshInterval: number = 0;

  constructor(private newJoinerService: NewJoinerService) {}

  ngOnInit() {
    this.loadStatistics();
    // Refresh stats every 30 seconds
    this.refreshInterval = window.setInterval(() => {
      this.loadStatistics();
    }, 30000);
  }

  ngOnDestroy() {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadStatistics() {
    this.isLoading = true;
    this.error = '';

    this.newJoinerService
      .getStatistics()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (stats: NewJoinerStats) => {
          this.stats = stats;
          this.isLoading = false;
        },
        error: (err: any) => {
          console.error('Error loading statistics:', err);
          this.error = 'Failed to load statistics';
          this.isLoading = false;
        }
      });
  }

  getEmailSuccessRate(): number {
    if (!this.stats || this.stats.totalNewJoiners === 0) return 0;
    return Math.round((this.stats.emailSent / this.stats.totalNewJoiners) * 100);
  }

  getEmailFailureRate(): number {
    if (!this.stats || this.stats.totalNewJoiners === 0) return 0;
    return Math.round((this.stats.emailFailed / this.stats.totalNewJoiners) * 100);
  }
}
