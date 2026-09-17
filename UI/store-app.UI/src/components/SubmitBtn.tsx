import { ReloadIcon } from '@radix-ui/react-icons';
import { Button } from '@/components/ui/button';

interface SubmitBtnProps {
  text: string;
  className?: string;
  /** The form is being sent: the button shows a spinner and cannot be pressed again. */
  isSubmitting?: boolean;
  disabled?: boolean;
}

const SubmitBtn = ({ text, className, isSubmitting = false, disabled = false }: SubmitBtnProps) => (
  <Button type="submit" className={className} disabled={isSubmitting || disabled}>
    {isSubmitting ? (
      <span className="flex">
        <ReloadIcon className="mr-2 h-4 w-4 animate-spin" />
        Submitting...
      </span>
    ) : (
      text
    )}
  </Button>
);
export default SubmitBtn;
