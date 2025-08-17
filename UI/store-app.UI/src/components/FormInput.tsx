import { Label } from "./ui/label";
import { Input } from "./ui/input";
import type * as React from 'react';

interface FormInputProps extends Omit<React.ComponentProps<'input'>, 'id' | 'name' | 'type' | 'defaultValue'> {
  name: string;
  type: string;
  label?: string;
  defaultValue?: string | number;
}

const FormInput = ({ label, name, type, defaultValue, ...rest }: FormInputProps) => {
  return (
    <div className="mb-2">
      <Label htmlFor={name} className="capitalize">
        {label || name}
      </Label>
  <Input id={name} name={name} type={type} defaultValue={defaultValue} {...rest} />
    </div>
  );
}
export default FormInput;
