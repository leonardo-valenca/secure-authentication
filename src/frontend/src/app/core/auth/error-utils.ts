import { HttpErrorResponse } from '@angular/common/http';

/**
 * ASP.NET Core's ValidationProblem responses shape errors as { [code]: string[] }.
 * This pulls the first message out for a simple, single-line UI display.
 */
export function extractErrorMessage(
  error: unknown,
  fallback = 'Something went wrong. Please try again.',
): string {
  if (error instanceof HttpErrorResponse) {
    const problemErrors = error.error?.errors as Record<string, string[]> | undefined;
    const firstKey = problemErrors ? Object.keys(problemErrors)[0] : undefined;

    if (firstKey && problemErrors?.[firstKey]?.[0]) {
      return problemErrors[firstKey][0];
    }
  }

  return fallback;
}

/** Checks for a specific ValidationProblem error code, e.g. distinguishing "email not confirmed" from a generic invalid-credentials error. */
export function hasErrorCode(error: unknown, code: string): boolean {
  if (error instanceof HttpErrorResponse) {
    const problemErrors = error.error?.errors as Record<string, string[]> | undefined;
    return !!problemErrors && code in problemErrors;
  }

  return false;
}
