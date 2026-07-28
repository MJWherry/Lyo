type PageHeroProps = {
  kicker?: string;
  title: string;
  description: string;
};

export function PageHero({ kicker, title, description }: PageHeroProps) {
  return (
    <div className="page-hero shell">
      {kicker ? <div className="kicker">{kicker}</div> : null}
      <h1>{title}</h1>
      <p>{description}</p>
    </div>
  );
}
