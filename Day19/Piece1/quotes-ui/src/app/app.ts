import { Component, inject, signal } from '@angular/core';
import {
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationStart,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { QuotesService } from './services/quotes.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly svc = inject(QuotesService);
  private readonly router = inject(Router);

  readonly loadingQuote = signal(false);

  constructor() {
    this.router.events.subscribe((e) => {
      if (e instanceof NavigationStart && /^\/quotes\/\d+/.test(e.url)) {
        this.loadingQuote.set(true);
      } else if (
        e instanceof NavigationEnd ||
        e instanceof NavigationCancel ||
        e instanceof NavigationError
      ) {
        this.loadingQuote.set(false);
      }
    });
  }

  logout() {
    this.svc.logout();
    this.router.navigateByUrl('/quotes');
  }

  login() {
    this.router.navigateByUrl('/login');
  }
}
