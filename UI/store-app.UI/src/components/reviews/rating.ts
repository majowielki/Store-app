/** The stars a review gives, as the review service accepts them. */
export const RATING_MIN = 1;
export const RATING_MAX = 5;

/** Every number of stars, from one up. */
export const ratingScale: readonly number[] = Array.from({ length: RATING_MAX - RATING_MIN + 1 }, (_, index) => RATING_MIN + index);

/** The limits the review service checks (ADR 012); the forms say so before anything is sent. */
export const REVIEW_BODY_MIN = 20;
export const REVIEW_BODY_MAX = 1000;
export const REVIEW_TITLE_MAX = 80;
/** A report's or a rejection's reason. */
export const REVIEW_REASON_MAX = 300;

/** "1 star", "4 stars". */
export const starsLabel = (stars: number) => (stars === 1 ? '1 star' : `${stars} stars`);

/** "4.5" - one decimal, the way the shop writes a rating. */
export const formatRating = (rating: number) => rating.toFixed(1);
