// Validation helpers for Register and Login forms

export function validateRegister(values: {
  email: string;
  password: string;
  confirmPassword: string;
  firstName?: string;
  lastName?: string;
}) {
  const errors: Record<string, string> = {};
  if (!values.email) {
    errors.email = 'Email is required';
  } else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(values.email)) {
    errors.email = 'Invalid email address';
  }
  if (!values.password) {
    errors.password = 'Password is required';
  } else if (values.password.length < 6) {
    errors.password = 'Password must be at least 6 characters';
  } else if (values.password.length > 100) {
    errors.password = 'Password must be at most 100 characters';
  }
  if (!values.confirmPassword) {
    errors.confirmPassword = 'Confirm password is required';
  } else if (values.confirmPassword !== values.password) {
    errors.confirmPassword = 'Passwords do not match';
  }
  if (values.firstName && values.firstName.length > 100) {
    errors.firstName = 'First name must be at most 100 characters';
  }
  if (values.lastName && values.lastName.length > 100) {
    errors.lastName = 'Last name must be at most 100 characters';
  }
  return errors;
}

export function validateLogin(values: { email: string; password: string }) {
  const errors: Record<string, string> = {};
  if (!values.email) {
    errors.email = 'Email is required';
  } else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(values.email)) {
    errors.email = 'Invalid email address';
  }
  if (!values.password) {
    errors.password = 'Password is required';
  }
  return errors;
}
