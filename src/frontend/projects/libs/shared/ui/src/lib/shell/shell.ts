import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiStatus } from '@career-skills-hub/shared/core';

const statusLabels: Record<ApiStatus, string> = {
  checking: 'checking…',
  online: 'online',
  offline: 'offline',
};

const statusDots: Record<ApiStatus, string> = {
  checking: 'bg-slate-400',
  online: 'bg-emerald-500',
  offline: 'bg-rose-500',
};

/** Page frame: header with the app name, the routed content, and a footer with the API status. */
@Component({
  selector: 'csh-shell',
  imports: [RouterLink],
  template: `
    <div class="flex min-h-screen flex-col bg-slate-50 text-slate-900">
      <header class="border-b border-slate-200 bg-white">
        <div class="mx-auto flex max-w-5xl items-center px-4 py-3">
          <a routerLink="/" class="text-lg font-semibold tracking-tight">{{ appName() }}</a>
        </div>
      </header>

      <main class="mx-auto w-full max-w-5xl flex-1 px-4 py-8">
        <ng-content />
      </main>

      <footer class="border-t border-slate-200 bg-white">
        <div
          class="mx-auto flex max-w-5xl items-center justify-end gap-2 px-4 py-3 text-sm text-slate-600"
        >
          <span class="size-2 rounded-full" [class]="statusDot()" aria-hidden="true"></span>
          <span data-testid="api-status">API {{ statusLabel() }}</span>
        </div>
      </footer>
    </div>
  `,
})
export class Shell {
  readonly appName = input.required<string>();
  readonly apiStatus = input<ApiStatus>('checking');

  protected readonly statusLabel = computed(() => statusLabels[this.apiStatus()]);
  protected readonly statusDot = computed(() => statusDots[this.apiStatus()]);
}
