import { Label } from "./ui/label";
import { Input } from "./ui/input";
import type * as React from 'react';

interface FormInputProps extends Omit<React.ComponentProps<'input'>, 'id' | 'name' | 'type' | 'defaultValue'> {
  name: string;
  type: string;
  label?: string;
  defaultValue?: string | number;
}

/** The small uppercase caption every form field of the shop carries. */
export const fieldLabelClass = 'text-[0.7rem] font-medium uppercase tracking-[0.14em] text-muted-foreground';

const FormInput = ({ label, name, type, defaultValue, ...rest }: FormInputProps) => {
  return (
    <div className="grid gap-2">
      <Label htmlFor={name} className={fieldLabelClass}>
        {label || name}
      </Label>
      <Input id={name} name={name} type={type} defaultValue={defaultValue} {...rest} />
    </div>
  );
}
export default FormInput;
