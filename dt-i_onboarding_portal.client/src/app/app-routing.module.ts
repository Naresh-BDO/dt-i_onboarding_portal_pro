import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { HomeComponent } from './home/home.component';
import { LoginComponent } from './login/login.component';
import { SignupComponent } from './signup/signup.component';
import { NewJoinerComponent } from './new-joiner/new-joiner.component';
import { NewJoinerListComponent } from './new-joiner/new-joiner-list.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { AuthGuard } from './auth/auth.guard';
import { RoleGuard } from './auth/role.guard';

const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'login', component: LoginComponent },
  { path: 'signup', component: SignupComponent },
  { 
    path: 'new-joiner-form', 
    component: NewJoinerComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'User'] }
  },
  { 
    path: 'new-joiner-list', 
    component: NewJoinerListComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'User'] }
  },
  { 
    path: 'dashboard', 
    component: DashboardComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
