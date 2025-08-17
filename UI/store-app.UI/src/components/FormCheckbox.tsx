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
    <div className="mb-2 flex items-center gap-2">
      <Checkbox id={name} name={name} defaultChecked={defaultChecked} />
      <Label htmlFor={name} className="capitalize cursor-pointer">
        {label || name}
      </Label>
    </div>
  );
}
export default FormCheckbox;
