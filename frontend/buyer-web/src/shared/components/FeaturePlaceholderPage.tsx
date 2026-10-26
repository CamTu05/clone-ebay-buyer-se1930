interface FeaturePlaceholderPageProps {
  title: string
}

export function FeaturePlaceholderPage({ title }: FeaturePlaceholderPageProps) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white p-8">
      <p className="text-sm font-semibold uppercase tracking-wide text-blue-700">Buyer feature</p>
      <h1 className="mt-2 text-3xl font-bold">{title}</h1>
      <p className="mt-3 text-slate-600">Awaiting official requirements and API contracts.</p>
    </section>
  )
}
