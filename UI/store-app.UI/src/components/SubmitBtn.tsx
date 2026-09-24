import { Loader2 } from 'lucide-react';
import { Button, type ButtonProps } from '@/components/ui/button';

interface SubmitBtnProps {
  text: string;
  className?: string;
  size?: ButtonProps['size'];
  /** The form is being sent: the button shows a spinner and cannot be pressed again. */
  isSubmitting?: boolean;
  disabled?: boolean;
}

const SubmitBtn = ({ text, className, size = 'lg', isSubmitting = false, disabled = false }: SubmitBtnProps) => (
  <Button type="submit" size={size} className={className} disabled={isSubmitting || disabled}>
    {isSubmitting ? (
      <>
        <Loader2 className="animate-spin" />
        Submitting...
      </>
    ) : (
      text
    )}
  </Button>
);
export default SubmitBtn;
