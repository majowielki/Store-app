/** The close button of the sign-in and register pages: back to where the visitor came from, or home. */
export const closeAuthPage = (): void => {
  const referrer = document.referrer;
  if (referrer && (referrer.includes('/login') || referrer.includes('/register'))) {
    window.location.href = '/';
    return;
  }
  if (window.history.length > 1) {
    window.history.back();
  } else {
    window.location.href = '/';
  }
};
