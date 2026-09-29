/** A number with its noun, singular for one: "1 item", "3 items". */
export const countOf = (count: number, noun: string, plural = `${noun}s`) => `${count} ${count === 1 ? noun : plural}`;
