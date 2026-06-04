import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { QuoteDetailVm } from './quote.resolver';

@Component({
  selector: 'app-quote-detail',
  imports: [DatePipe, RouterLink],
  templateUrl: './quote-detail.component.html',
  styleUrl: './quote-detail.component.css',
})
export class QuoteDetailComponent {
  // Bound from the route's resolved data (`resolve: { vm: quoteResolver }`) via
  // withComponentInputBinding(). It is present at activation time, so the
  // `found` card — and its view-transition-name — exists in the very first
  // render, which is exactly the snapshot the router's View Transition captures.
  // (The previous draft fetched in an effect AFTER activation, so the card did
  // not exist at capture time and the shared-element morph never fired.)
  readonly vm = input.required<QuoteDetailVm>();
}
