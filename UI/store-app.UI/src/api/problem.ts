import type { ProblemDetails } from './types';

/**
 * What a failed request leaves behind. The API answers every failure with a problem
 * (application/problem+json), so "problem" is missing only when the server could not be
 * reached or answered with something that is not JSON.
 */
export interface ApiError {
  /** HTTP status of the answer, or the reason there was none. */
  status: number | 'FETCH_ERROR' | 'PARSING_ERROR' | 'TIMEOUT_ERROR';
  problem?: ProblemDetails;
  /** What to tell the user; taken from the problem when there is one. */
  message: string;
  /** The access token could not be renewed: the user has been signed out, this is not their mistake. */
  sessionEnded?: boolean;
}

export const isApiError = (error: unknown): error is ApiError =>
  typeof error === 'object' && error !== null && 'status' in error && 'message' in error;

/** The messages per field of a validation problem (422), flattened; empty for any other error. */
export const fieldMessages = (problem?: ProblemDetails): string[] =>
  problem?.errors ? Object.values(problem.errors).flat().filter((m): m is string => typeof m === 'string') : [];

const byStatus: Record<string, string> = {
  FETCH_ERROR: 'The server could not be reached. Please try again.',
  TIMEOUT_ERROR: 'The server took too long to answer. Please try again.',
  PARSING_ERROR: 'The server sent an answer this app could not read.',
  401: 'Please sign in to continue.',
  403: 'You are not allowed to do this.',
  404: 'Not found.',
  429: 'Too many requests. Please wait a moment and try again.',
};

const fallback = 'Something went wrong. Please try again.';

/**
 * The message to show for a failed request: the problem's detail, with the field messages of
 * a validation problem appended; for a problem without a detail (the framework's own 403 or
 * 404) a sentence for the status rather than its bare title.
 */
export const describeProblem = (status: ApiError['status'], problem?: ProblemDetails): string => {
  const fields = fieldMessages(problem);
  const detail = problem?.detail?.trim();
  if (fields.length > 0) return `${detail || problem?.title || 'Please check the form'}: ${fields.join(' ')}`;
  if (detail) return detail;
  return byStatus[String(status)] ?? problem?.title?.trim() ?? fallback;
};

/** Message for any error a hook or thunk hands back, API error or not. */
export const errorMessage = (error: unknown): string => {
  if (isApiError(error)) return error.message;
  if (error instanceof Error && error.message) return error.message;
  return fallback;
};
