import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { AccountDataExport } from '../../core/auth/models';
import { Dashboard } from './dashboard';

describe('Dashboard', () => {
  let component: Dashboard;
  let fixture: ComponentFixture<Dashboard>;
  let authService: {
    currentUser: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
    exportAccountData: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  const user = { id: '1', email: 'user@example.com' };

  beforeEach(() => {
    authService = {
      currentUser: vi.fn(() => user),
      logout: vi.fn(() => of(void 0)),
      exportAccountData: vi.fn(),
    };

    TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    });

    fixture = TestBed.createComponent(Dashboard);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  });

  it('displays the current user', () => {
    expect(component['user']()).toEqual(user);
  });

  it('logs out and navigates to /login', () => {
    component['logout']();

    expect(authService.logout).toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('downloads the account data export as a JSON file', () => {
    const exportData: AccountDataExport = {
      id: user.id,
      email: user.email,
      createdAtUtc: '2026-01-01T00:00:00Z',
    };
    authService.exportAccountData.mockReturnValue(of(exportData));

    const createObjectURL = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:mock-url');
    const revokeObjectURL = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);

    component['exportData']();

    expect(authService.exportAccountData).toHaveBeenCalled();
    expect(createObjectURL).toHaveBeenCalledWith(expect.any(Blob));
    expect(click).toHaveBeenCalled();
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:mock-url');
  });
});
