import React from "react";
import { Badge } from "@/components/ui/badge";

interface SaleBadgeProps {
  percent: number;
  className?: string;
}

const SaleBadge: React.FC<SaleBadgeProps> = ({ percent, className }) => {
  if (!Number.isFinite(percent) || percent <= 0) return null;
  return (
    <div className={"absolute top-2 right-2 " + (className ?? "") }>
      <Badge variant="destructive" className="rounded-md shadow">
        -{Math.round(percent)}%
      </Badge>
    </div>
  );
};

export default SaleBadge;
