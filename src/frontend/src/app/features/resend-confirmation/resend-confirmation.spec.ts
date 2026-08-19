import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ResendConfirmation } from './resend-confirmation';

describe('ResendConfirmation', () => {
  let component: ResendConfirmation;
  let fixture: ComponentFixture<ResendConfirmation>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResendConfirmation],
    }).compileComponents();

    fixture = TestBed.createComponent(ResendConfirmation);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
