import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";

interface FormCheckboxProps {
  name: string;
  label?: string;
  defaultValue?: string;
}

const FormCheckbox = ({ name, label, defaultValue }: FormCheckboxProps) => {
  const defaultChecked = defaultValue === "on" ? true : false;

  return (
    <div className="flex items-center gap-3">
      <Checkbox id={name} name={name} defaultChecked={defaultChecked} />
      <Label htmlFor={name} className="cursor-pointer text-sm font-normal first-letter:uppercase">
        {label || name}
      </Label>
    </div>
  );
}
export default FormCheckbox;
