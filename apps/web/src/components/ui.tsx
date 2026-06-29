import type { AnchorHTMLAttributes, ButtonHTMLAttributes, ReactNode, SelectHTMLAttributes } from "react";

type Tone = "neutral" | "success" | "warning" | "danger" | "info";
type Size = "sm" | "md";

type PageHeaderProps = {
  actions?: ReactNode;
  children?: ReactNode;
  description?: string;
  eyebrow: string;
  title: string;
};

export function PageHeader({ actions, children, description, eyebrow, title }: PageHeaderProps) {
  return (
    <header className="page-header">
      <div className="page-header__main">
        <p className="eyebrow">{eyebrow}</p>
        <h1>{title}</h1>
        {description ? <p className="page-header__description">{description}</p> : null}
        {children}
      </div>
      {actions ? <div className="page-header__actions">{actions}</div> : null}
    </header>
  );
}

type WorkspaceMetric = {
  label: string;
  value: string | number;
  tone?: Tone;
  hint?: string;
};

export function WorkspaceMetricStrip({ items }: { items: WorkspaceMetric[] }) {
  return (
    <section className="workspace-metric-strip" aria-label="Workspace priority summary">
      {items.map((item) => (
        <MetricTile className="workspace-metric" hint={item.hint} key={item.label} label={item.label} tone={item.tone} value={item.value} />
      ))}
    </section>
  );
}

export function MetricTile({
  className,
  hint,
  label,
  tone = "neutral",
  value
}: {
  className?: string;
  hint?: string;
  label: string;
  tone?: Tone;
  value: ReactNode;
}) {
  const classes = ["ui-metric-tile", `ui-metric-tile--${tone}`, className, className ? `${className}--${tone}` : undefined].filter(Boolean).join(" ");

  return (
    <div className={classes}>
      <span>{label}</span>
      <strong>{value}</strong>
      {hint ? <small>{hint}</small> : null}
    </div>
  );
}

export function ComplianceBadge({ label, tone = "neutral" }: { label: string; tone?: Tone }) {
  return <span className={`compliance-badge compliance-badge--${tone}`}>{label}</span>;
}

export function DataHandlingBadge({ mode }: { mode: string }) {
  const normalizedMode = mode.toLowerCase();
  const tone: Tone = normalizedMode.includes("cui") && !normalizedMode.includes("no") ? "danger" : normalizedMode.includes("demo") ? "info" : "warning";

  return <ComplianceBadge label={mode} tone={tone} />;
}

export function SourceMeta({
  confidence,
  lastReviewedAt,
  source
}: {
  confidence: string;
  lastReviewedAt: string;
  source: string;
}) {
  return (
    <dl className="source-meta">
      <div>
        <dt>Source</dt>
        <dd>{source}</dd>
      </div>
      <div>
        <dt>Confidence</dt>
        <dd>{confidence}</dd>
      </div>
      <div>
        <dt>Reviewed</dt>
        <dd>{lastReviewedAt}</dd>
      </div>
    </dl>
  );
}

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  icon?: ReactNode;
  size?: Size;
  variant?: "primary" | "secondary" | "danger" | "ghost";
};

export function Button({ children, className, icon, size = "md", type = "button", variant = "secondary", ...props }: ButtonProps) {
  const classes = ["ui-button", `ui-button--${variant}`, `ui-button--${size}`, className].filter(Boolean).join(" ");

  return (
    <button className={classes} type={type} {...props}>
      {icon ? <span className="ui-button__icon">{icon}</span> : null}
      <span>{children}</span>
    </button>
  );
}

export function StatusPill({ label, tone = "neutral" }: { label: string; tone?: Tone }) {
  return <span className={`ui-status-pill ui-status-pill--${tone}`}>{label}</span>;
}

export function RiskBadge({ level }: { level: string }) {
  const normalized = level.toLowerCase();
  const tone: Tone = normalized.includes("critical") || normalized.includes("high") ? "danger" : normalized.includes("medium") ? "warning" : "success";

  return <span className={`ui-risk-badge ui-risk-badge--${tone}`}>{level}</span>;
}

export function DataRow({
  items
}: {
  items: Array<{ label: string; value: ReactNode; tone?: Tone }>;
}) {
  return (
    <dl className="ui-data-row">
      {items.map((item) => (
        <div className={item.tone ? `ui-data-row__item ui-data-row__item--${item.tone}` : "ui-data-row__item"} key={item.label}>
          <dt>{item.label}</dt>
          <dd>{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}

export function TaskCard({
  actions,
  badges,
  children,
  className,
  meta,
  summary,
  title
}: {
  actions?: ReactNode;
  badges?: ReactNode;
  children?: ReactNode;
  className?: string;
  meta?: Array<{ label: string; value: ReactNode; tone?: Tone }>;
  summary?: ReactNode;
  title: ReactNode;
}) {
  const classes = ["ui-task-card", className].filter(Boolean).join(" ");

  return (
    <article className={classes}>
      <div className="ui-task-card__header">
        <div>
          {badges ? <div className="ui-task-card__badges">{badges}</div> : null}
          <h3>{title}</h3>
        </div>
        {actions ? <div className="ui-task-card__actions">{actions}</div> : null}
      </div>
      {summary ? <p>{summary}</p> : null}
      {meta ? <DataRow items={meta} /> : null}
      {children}
    </article>
  );
}

export function WorkflowColumn({
  ariaLabel,
  children,
  description,
  title
}: {
  ariaLabel?: string;
  children: ReactNode;
  description?: string;
  title: string;
}) {
  return (
    <section aria-label={ariaLabel ?? title} className="ui-workflow-column">
      <div className="ui-workflow-column__header">
        <h3>{title}</h3>
        {description ? <p>{description}</p> : null}
      </div>
      <div className="ui-workflow-column__body">{children}</div>
    </section>
  );
}

export function NavItem({
  active,
  children,
  className,
  description,
  icon,
  label,
  ...props
}: AnchorHTMLAttributes<HTMLAnchorElement> & {
  active?: boolean;
  description?: string;
  icon?: ReactNode;
  label: string;
}) {
  const classes = ["ui-nav-item", className].filter(Boolean).join(" ");

  return (
    <a aria-current={active ? "page" : undefined} className={classes} {...props}>
      {icon}
      <span>
        <strong>{label}</strong>
        {description ? <small>{description}</small> : null}
        {children}
      </span>
    </a>
  );
}

export function Panel({
  actions,
  children,
  description,
  eyebrow,
  title
}: {
  actions?: ReactNode;
  children: ReactNode;
  description?: string;
  eyebrow?: string;
  title?: string;
}) {
  return (
    <section className="ui-panel">
      {title || eyebrow || description || actions ? (
        <div className="ui-panel__header">
          <div>
            {eyebrow ? <p className="eyebrow">{eyebrow}</p> : null}
            {title ? <h2>{title}</h2> : null}
            {description ? <p>{description}</p> : null}
          </div>
          {actions ? <div className="ui-panel__actions">{actions}</div> : null}
        </div>
      ) : null}
      <div className="ui-panel__body">{children}</div>
    </section>
  );
}

export function Alert({
  children,
  title,
  tone = "info"
}: {
  children?: ReactNode;
  title: string;
  tone?: Tone;
}) {
  return (
    <div className={`ui-alert ui-alert--${tone}`} role={tone === "danger" ? "alert" : "status"}>
      <strong>{title}</strong>
      {children ? <div>{children}</div> : null}
    </div>
  );
}

export function EmptyState({
  action,
  body,
  title
}: {
  action?: ReactNode;
  body: string;
  title: string;
}) {
  return (
    <div className="ui-empty-state">
      <h3>{title}</h3>
      <p>{body}</p>
      {action ? <div className="ui-empty-state__action">{action}</div> : null}
    </div>
  );
}

export function LoadingState({ label = "Loading workspace data" }: { label?: string }) {
  return (
    <div className="ui-loading-state" role="status" aria-live="polite">
      <span aria-hidden="true" />
      <strong>{label}</strong>
    </div>
  );
}

export function Field({
  children,
  error,
  hint,
  label
}: {
  children: ReactNode;
  error?: string;
  hint?: string;
  label: string;
}) {
  return (
    <label className="ui-field">
      <span>{label}</span>
      {children}
      {hint ? <small>{hint}</small> : null}
      {error ? <strong role="alert">{error}</strong> : null}
    </label>
  );
}

export function SelectField({
  label,
  options,
  ...props
}: SelectHTMLAttributes<HTMLSelectElement> & {
  label: string;
  options: Array<{ label: string; value: string }>;
}) {
  return (
    <Field label={label}>
      <select {...props}>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </Field>
  );
}

export function Toolbar({ children }: { children: ReactNode }) {
  return <div className="ui-toolbar">{children}</div>;
}

export function DataTable({
  caption,
  children
}: {
  caption?: string;
  children: ReactNode;
}) {
  return (
    <div className="ui-table-wrap">
      <table className="ui-table">
        {caption ? <caption>{caption}</caption> : null}
        {children}
      </table>
    </div>
  );
}
