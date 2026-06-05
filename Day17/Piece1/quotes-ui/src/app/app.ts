import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { QuotesService } from './services/quotes.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly svc = inject(QuotesService);
  private readonly router = inject(Router);

  logout() {
    this.svc.logout();
    this.router.navigateByUrl('/quotes');
  }
}
