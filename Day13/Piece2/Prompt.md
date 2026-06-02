# Prompt To Claude

Act as an expert Angular 21 developer. I need you to scaffold a standalone, zoneless Angular component that integrates with my existing "QuotesApi". 

Follow these strict modern Angular guidelines:
1. No NgModules: Use standalone components and provideHttpClient() if a config is generated.
2. No constructor injection: Use the modern inject() function for the API service.
3. Modern Control Flow: Use @for (with a strict 'track' statement) and @if/@else. Do not use *ngIf or *ngFor.
4. Signals-First State: Manage all state using signal(). 
5. Derived State & Side Effects: Create at least one computed() value derived from two different signals (e.g., filtering/counting quotes based on a search term signal and a quotes array signal), and use an effect() to log state changes.

API Contract (QuotesApi at http://localhost:5051):
- GET /api/quotes?page=<int>&size=<int> → QuoteReadModel[]
  Fields: id: number, authorName: string (mapped via Author AS AuthorName in Dapper), text: string, createdAt: string (ISO-8601)
- GET /api/quotes/{id} → QuoteReadModel (same fields, 404 → null)
- GET /api/quotes/with-metadata?page=<int>&size=<int> → QuoteMetadataReadModel[]
  Fields: quoteId: number, quote: string, user: string, tags: string[], categories: string[]

Requirements:
- Create a QuotesService using inject(HttpClient) to fetch the data.
- Create a QuotesListComponent that displays quotes with list/metadata view toggle, handles loading/error/empty states, and uses signals + computed + effect.
- Create an App root component with a detail panel: selectedId signal triggers a getById() effect; detailStatus: 'idle'|'loading'|'found'|'notFound'.

Also apply these fixes to the initial agent output:
1. Read view() BEFORE untracked() so view changes re-trigger the data-loading effect; remove the duplicate fetch logic from switchView().
2. Type the detail signal as signal<QuoteReadModel | null>(null) — not an inline object type.
3. Add a stale-response guard in the getById() callbacks: check selectedId() === id before writing to signals.
4. Make isEmpty view-aware: this.view() === 'list' ? this.quotes().length === 0 : this.metadata().length === 0.
