import { isAxiosError } from 'axios';
import type { ProblemDetails } from './types';

/** The problem response an API error carries, when it carries one. */
export function getProblem(error: unknown): ProblemDetails | undefined {
  if (!isAxiosError(error)) return undefined;
  const data: unknown = error.response?.data;
  if (data && typeof data === 'object' && ('title' in data || 'detail' in data || 'status' in data)) {
    return data as ProblemDetails;
  }
  return undefined;
}

/** HTTP status of an API error, or undefined for a network failure. */
export function getStatus(error: unknown): number | undefined {
  return isAxiosError(error) ? error.response?.status : undefined;
}

/**
 * The message to show for a failed request: the problem's detail (or title), with the
 * field messages of a validation problem appended; the transport error otherwise.
 */
export function extractApiErrorMessage(error: unknown): string {
  const problem = getProblem(error);
  if (problem) {
    const fieldMessages = problem.errors
      ? Object.values(problem.errors).flat().filter((m) => typeof m === 'string')
      : [];
    const headline = problem.detail || problem.title;
    if (fieldMessages.length > 0) {
      return headline ? `${headline}: ${fieldMessages.join(' ')}` : fieldMessages.join(' ');
    }
    if (headline) return headline;
  }
  if (isAxiosError(error) && !error.response) return 'The server could not be reached. Please try again.';
  if (error instanceof Error && error.message) return error.message;
  return 'An unexpected error occurred. Please try again.';
}

export const getErrorMessage = extractApiErrorMessage;
