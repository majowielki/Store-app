import InfoPage, { DemoNote, InfoSection } from './InfoPage';

/** What the shop keeps about you, why, and for how long. */
const Privacy = () => (
  <InfoPage eyebrow="Privacy policy" title="Your data, plainly" lead="What we keep, why we keep it and how long we keep it for.">
    <DemoNote />
    <InfoSection title="What we keep">
      <ul className="list-disc space-y-2 pl-6">
        <li>Your account: name, e-mail address and a hash of your password - never the password itself.</li>
        <li>The delivery address you give at checkout, if you ask us to remember it.</li>
        <li>Your orders, for as long as your account exists.</li>
        <li>A record of changes made in the admin panel, kept for 90 days.</li>
      </ul>
    </InfoSection>
    <InfoSection title="Cookies and your browser">
      <p>
        One cookie keeps you signed in; it cannot be read by scripts on the page. Your bag, while you shop as a guest, and
        the light or dark theme are stored in your browser. We use no advertising or tracking cookies.
      </p>
    </InfoSection>
    <InfoSection title="Your rights">
      <p>
        You can ask to see, correct or delete your data at any time from the contact page. Deleting your account removes
        your personal data; orders are kept without it for as long as the law requires.
      </p>
    </InfoSection>
  </InfoPage>
);

export default Privacy;
