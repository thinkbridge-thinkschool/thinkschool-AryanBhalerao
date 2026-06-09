import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { quoteResolver, QuoteDetailVm } from './quote.resolver';
import { QuotesService } from '../services/quotes.service';

// The detail route resolves a QuoteDetailVm BEFORE activation. These specs pin
// the three outcomes the detail view switches on, driven by the real
// GET /api/quotes/{id} endpoint and its `id` route param.
describe('quoteResolver (ResolveFn)', () => {
  const QUOTE_ROW = {
    id: 7,
    authorName: 'Marcus Aurelius',
    text: 'You have power over your mind, not outside events.',
    createdAt: '2026-06-01T12:42:18.000+00:00',
  };
  const META_ROW = {
    quoteId: 7,
    quote: QUOTE_ROW.text,
    author: QUOTE_ROW.authorName,
    user: 'user1@email.com',
    createdAt: QUOTE_ROW.createdAt,
    tags: ['wisdom'],
    categories: ['classic'],
  };

  function resolve(idParam: string, svc: Partial<QuotesService>): QuoteDetailVm {
    TestBed.configureTestingModule({
      providers: [{ provide: QuotesService, useValue: svc }],
    });
    const route = { paramMap: convertToParamMap({ id: idParam }) } as ActivatedRouteSnapshot;
    let out!: QuoteDetailVm;
    TestBed.runInInjectionContext(() =>
      (quoteResolver(route, {} as any) as any).subscribe((vm: QuoteDetailVm) => (out = vm)),
    );
    return out;
  }

  it('resolves a real id to { status: "found", quote } and enriches tags from /with-metadata', () => {
    let calledWith: number | undefined;
    const vm = resolve('7', {
      getById: (id: number) => {
        calledWith = id;
        return of(QUOTE_ROW) as any;
      },
      getMetadataById: () => of(META_ROW) as any,
    });
    expect(calledWith).toBe(7); // param coerced from string to int
    expect(vm.status).toBe('found');
    expect(vm.quote).toEqual(QUOTE_ROW);
    // user/tags/categories are NOT on the by-id row — they come from getMetadataById.
    expect(vm.user).toBe('user1@email.com');
    expect(vm.tags).toEqual(['wisdom']);
    expect(vm.categories).toEqual(['classic']);
  });

  it('still resolves "found" with no user/empty tags when the metadata lookup fails', () => {
    const vm = resolve('7', {
      getById: () => of(QUOTE_ROW) as any,
      getMetadataById: () => throwError(() => new Error('boom')) as any,
    });
    expect(vm.status).toBe('found');
    expect(vm.user).toBeNull();
    expect(vm.tags).toEqual([]);
    expect(vm.categories).toEqual([]);
  });

  it('maps a 404 from GET /api/quotes/{id} to { status: "notFound" }', () => {
    const vm = resolve('99999', {
      getById: () => throwError(() => ({ status: 404 })) as any,
      getMetadataById: () => of(null) as any,
    });
    expect(vm.status).toBe('notFound');
    expect(vm.quote).toBeNull();
  });

  it('short-circuits a non-integer param to "invalid" WITHOUT touching the API', () => {
    let called = false;
    const vm = resolve('abc', {
      getById: () => {
        called = true;
        return of(QUOTE_ROW) as any;
      },
    });
    expect(called).toBe(false); // server route is {id:int}; never issue a doomed request
    expect(vm.status).toBe('invalid');
  });

  it('treats id <= 0 as invalid (0 and negatives are not valid quote ids)', () => {
    const vm = resolve('0', { getById: () => of(QUOTE_ROW) as any });
    expect(vm.status).toBe('invalid');
  });
});
