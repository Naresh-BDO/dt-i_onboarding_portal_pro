import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { SignupRequest } from '../models/signup-request';
import { RoleDto } from '../models/role-dto';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.css']
})
export class SignupComponent implements OnInit {
  username = '';
  email = '';
  password = '';
  confirmPassword = '';
  selectedRoleId = 0;

  roles: RoleDto[] = [];
  isSubmitting = false;
  error = '';
  success = '';

  constructor(
    private auth: AuthService,
    private router: Router
  ) { }

  ngOnInit() {
    this.loadRoles();
  }

  loadRoles() {
    this.auth.getAvailableRoles().subscribe({
      next: (roles) => {
        this.roles = roles;
      },
      error: (err) => {
        console.error('Error loading roles:', err);
        this.error = 'Failed to load available roles.';
      }
    });
  }

  onSignup() {
    this.error = '';
    this.success = '';

    if (!this.username || !this.email || !this.password || !this.confirmPassword || !this.selectedRoleId) {
      this.error = 'Please fill in all fields and select a role.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }

    if (this.password.length < 6) {
      this.error = 'Password must be at least 6 characters long.';
      return;
    }

    this.isSubmitting = true;

    const signupRequest: SignupRequest = {
      username: this.username.trim(),
      email: this.email.trim(),
      password: this.password,
      roleId: parseInt(this.selectedRoleId.toString())
    };

    this.auth.signup(signupRequest).subscribe({
      next: (res) => {
        this.success = 'Account created successfully! Redirecting to login...';
        this.isSubmitting = false;
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1500);
      },
      error: (err) => {
        console.error('Signup error:', err);
        const status = err?.status;
        if (status === 400) {
          const message = err?.error?.message || 'Signup failed. Please check your inputs.';
          this.error = message;
        } else if (status === 0) {
          this.error = 'Cannot reach server. Check if https://localhost:7107 is running.';
        } else {
          this.error = `Signup failed (status ${status}).`;
        }
        this.isSubmitting = false;
      }
    });
  }
}
