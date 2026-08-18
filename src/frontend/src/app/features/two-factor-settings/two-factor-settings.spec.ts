import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TwoFactorSettings } from './two-factor-settings';

describe('TwoFactorSettings', () => {
  let component: TwoFactorSettings;
  let fixture: ComponentFixture<TwoFactorSettings>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TwoFactorSettings]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TwoFactorSettings);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
