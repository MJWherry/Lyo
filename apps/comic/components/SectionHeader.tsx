export function SectionHeader({ title, href, action }: { title: string; href?: string; action?: string }) {
  return (
    <div className="section-header">
      <h2>{title}</h2>
      {href ? (
        <a href={href} className="section-header__action">
          {action ?? "View all"}
        </a>
      ) : null}
    </div>
  );
}
