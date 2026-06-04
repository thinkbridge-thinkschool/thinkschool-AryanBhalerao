import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LoginFormComponent } from '../login-form/login-form.component';

@Component({
  selector: 'app-login',
  imports: [LoginFormComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  // After a successful login the user goes back to wherever the guard bounced
  // them from (?returnUrl), defaulting to the list.
  onLoggedIn() {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/quotes';
    this.router.navigateByUrl(returnUrl);
  }
}
