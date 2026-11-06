import { TestBed } from '@angular/core/testing';
import { FieldErrors } from './field-errors';

describe('FieldErrors', () => {
  it('shows one line per message', async () => {
    const fixture = TestBed.createComponent(FieldErrors);
    fixture.componentRef.setInput('messages', ['Name is required.', 'Use at most 100 characters.']);
    await fixture.whenStable();

    const lines = [...(fixture.nativeElement as HTMLElement).querySelectorAll('p')];
    expect(lines.map((line) => line.textContent)).toEqual([
      'Name is required.',
      'Use at most 100 characters.',
    ]);
  });

  it('shows nothing by default', async () => {
    const fixture = TestBed.createComponent(FieldErrors);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelectorAll('p')).toHaveLength(0);
  });
});
