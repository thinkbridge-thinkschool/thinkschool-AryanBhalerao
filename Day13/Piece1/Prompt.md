# Prompt To Claude

Act as an expert Angular 21 developer. I need you to scaffold a standalone, zoneless Angular component that integrates with my existing "QuotesApi". 

Follow these strict modern Angular guidelines:
1. No NgModules: Use standalone components and provideHttpClient() if a config is generated.
2. No constructor injection: Use the modern inject() function for the API service.
3. Modern Control Flow: Use @for (with a strict 'track' statement) and @if/@else. Do not use *ngIf or *ngFor.
4. Signals-First State: Manage all state using signal(). 
5. Derived State & Side Effects: Create at least one computed() value derived from two different signals (e.g., filtering/counting quotes based on a search term signal and a quotes array signal), and use an effect() to log state changes.

API Contract (QuotesApi):
- Endpoint: /api/quotes
- Response schema per quote: { id: number, text: string, author: string, category: string, likes: number }

Requirements:
- Create a QuotesService using inject(HttpClient) to fetch the data.
- Create a QuotesComponent that displays the list of quotes, handles an empty state gracefully, and shows the computed summary metric (e.g., "Showing X quotes by Author Y").