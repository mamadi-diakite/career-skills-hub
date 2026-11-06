import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { of, Subject, throwError } from 'rxjs';
import { FormSubmission, NO_FORM_ERRORS, toFormErrors } from './form-submission';

const unexpected = { fields: {}, form: 'Something went wrong. Please try again.' };

describe('toFormErrors', () => {
  it('reads field errors of a validation problem, in camelCase', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        errors: { Email: ['Taken.'], password: ['Too short.'] },
      },
    });

    expect(toFormErrors(error)).toEqual({
      fields: { email: ['Taken.'], password: ['Too short.'] },
      form: null,
    });
  });

  it('uses the problem title for other client errors', () => {
    const error = new HttpErrorResponse({
      status: 403,
      error: { title: 'Only HR admins can manage the organisation.' },
    });

    expect(toFormErrors(error)).toEqual({
      fields: {},
      form: 'Only HR admins can manage the organisation.',
    });
  });

  it.each([
    ['a client error without body', new HttpErrorResponse({ status: 404 })],
    ['a server error', new HttpErrorResponse({ status: 500, error: { title: 'Boom' } })],
    ['a network error', new HttpErrorResponse({ status: 0 })],
    ['something else', new Error('boom')],
  ])('shows a generic message for %s', (_case, error) => {
    expect(toFormErrors(error)).toEqual(unexpected);
  });
});

describe('FormSubmission', () => {
  function setUp() {
    const form = new FormGroup({
      email: new FormControl('', { nonNullable: true, validators: Validators.required }),
    });
    return { form, submission: new FormSubmission(form, { email: 'Enter your email.' }) };
  }

  it('shows the client message only once an invalid field has been touched', () => {
    const { form, submission } = setUp();

    expect(submission.messagesFor('email')).toEqual([]);

    form.controls.email.markAsTouched();
    expect(submission.messagesFor('email')).toEqual(['Enter your email.']);

    form.controls.email.setValue('claire@example.com');
    expect(submission.messagesFor('email')).toEqual([]);
  });

  it('does not send an invalid form and shows every client message', () => {
    const { submission } = setUp();
    const request = vi.fn(() => of('sent'));

    submission.submit(request, vi.fn());

    expect(request).not.toHaveBeenCalled();
    expect(submission.messagesFor('email')).toEqual(['Enter your email.']);
  });

  it('is pending while the request runs, then calls back with the result', () => {
    const { form, submission } = setUp();
    form.setValue({ email: 'claire@example.com' });
    const response = new Subject<string>();
    const onSuccess = vi.fn();

    submission.submit(() => response, onSuccess);
    expect(submission.pending()).toBe(true);

    response.next('sent');
    response.complete();

    expect(onSuccess).toHaveBeenCalledWith('sent');
    expect(submission.pending()).toBe(false);
    expect(submission.errors()).toEqual(NO_FORM_ERRORS);
  });

  it('shows server messages before client ones, until the next submit', () => {
    const { form, submission } = setUp();
    form.setValue({ email: 'claire@example.com' });
    const error = new HttpErrorResponse({ status: 400, error: { errors: { email: ['Taken.'] } } });

    submission.submit(() => throwError(() => error), vi.fn());

    expect(submission.pending()).toBe(false);
    expect(submission.messagesFor('email')).toEqual(['Taken.']);

    submission.submit(() => of('sent'), vi.fn());
    expect(submission.messagesFor('email')).toEqual([]);
  });
});
