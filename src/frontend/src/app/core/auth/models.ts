export interface AuthenticatedUser {
  id: string;
  email: string;
}

export interface AccountDataExport {
  id: string;
  email: string;
  createdAtUtc: string;
}

/** User is null exactly when requiresTwoFactor is true, no tokens exist yet to describe a user for. */
export interface LoginResponse {
  requiresTwoFactor: boolean;
  user: AuthenticatedUser | null;
}

export interface TwoFactorSetup {
  sharedKey: string;
  authenticatorUri: string;
}

export interface RecoveryCodesResponse {
  recoveryCodes: string[];
}
