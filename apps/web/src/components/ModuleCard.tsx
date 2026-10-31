import { CheckCircle2, CircleDashed, type LucideIcon } from "lucide-react";
import type { ModuleStatus } from "@/lib/api";

type ModuleCardProps = {
  module: ModuleStatus;
  icon: LucideIcon;
};

export function ModuleCard({ module, icon: Icon }: ModuleCardProps) {
  const isSeeded = module.status === "seeded";

  return (
    <article className="module-card">
      <div className="module-card__header">
        <span className="icon-box" aria-hidden="true">
          <Icon size={18} />
        </span>
        <span className={isSeeded ? "status status--seeded" : "status"}>
          {isSeeded ? <CheckCircle2 size={14} /> : <CircleDashed size={14} />}
          {module.status}
        </span>
      </div>
      <h3>{module.name}</h3>
      <p>{module.purpose}</p>
    </article>
  );
}
