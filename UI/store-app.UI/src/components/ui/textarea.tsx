import * as React from "react"

import { cn } from "@/lib/utils"

const Textarea = React.forwardRef<
  HTMLTextAreaElement,
  React.ComponentProps<"textarea">
>(({ className, ...props }, ref) => {
  return (
    <textarea
      className={cn(
        "flex min-h-[120px] w-full rounded-lg border border-input bg-card px-4 py-3 text-base ring-offset-background transition-[border-color,box-shadow] duration-200 placeholder:text-muted-foreground/70 hover:border-foreground/40 focus-visible:outline-none focus-visible:border-foreground focus-visible:ring-4 focus-visible:ring-foreground/10 disabled:cursor-not-allowed disabled:opacity-50 md:text-sm",
        className
      )}
      ref={ref}
      {...props}
    />
  )
})
Textarea.displayName = "Textarea"

export { Textarea }
