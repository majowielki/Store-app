// Error handling utilities
export interface ApiError {
  response?: {
    data?: {
      message?: string;
    };
    status?: number;
  };
  message?: string;
}

export const getErrorMessage = (error: unknown): string => {
  if (error && typeof error === 'object' && 'response' in error) {
    const apiError = error as ApiError;
    return apiError.response?.data?.message || 'An error occurred';
  }
  
  if (error instanceof Error) {
    return error.message;
  }
  
  return 'An unknown error occurred';
};


// More robust error extractor for API errors
export function extractApiErrorMessage(error: unknown): string {
  if (
    error &&
    typeof error === 'object' &&
    'response' in error &&
    error.response &&
    typeof error.response === 'object'
  ) {
    const response = (error as { response: unknown }).response;
    if (
      response &&
      typeof response === 'object' &&
      'data' in response &&
      response.data !== undefined
    ) {
      const data = (response as { data: unknown }).data;
      if (
        data &&
        typeof data === 'object' &&
        'message' in data &&
        typeof (data as { message?: unknown }).message === 'string'
      ) {
        return (data as { message: string }).message;
      }
      if (typeof data === 'string') return data;
    }
  }
  if (error instanceof Error && error.message) return error.message;
  return 'An unexpected error occurred. Please try again.';
}
