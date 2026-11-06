import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { ApiHealthService } from '@career-skills-hub/shared/core';
import { Shell } from '@career-skills-hub/shared/ui';

@Component({
  selector: 'csh-root',
  imports: [RouterOutlet, Shell],
  template: `
    <csh-shell appName="Career Skills Hub" [apiStatus]="apiStatus()">
      <router-outlet />
    </csh-shell>
  `,
})
export class App {
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });
}
