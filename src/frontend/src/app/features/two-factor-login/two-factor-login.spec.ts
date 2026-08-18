import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TwoFactorLogin } from './two-factor-login';

describe('TwoFactorLogin', () => {
  let component: TwoFactorLogin;
  let fixture: ComponentFixture<TwoFactorLogin>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TwoFactorLogin]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TwoFactorLogin);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
