import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { getProducts } from '../features/catalog/catalogApi'
import { ProductImage } from '../features/catalog/ProductImage'
import type { ProductSummary } from '../features/catalog/types'
import { useWatchlist } from '../features/watchlist/watchlistStore'
import { Icon } from '../shared/components/Icon'

const img = (file: string) => `/images/ebay/${file}.webp`
const technology = [
  ['Laptops', 'laptop', 's-l500'], ['Computer parts', 'computer', 's-l500(1)'], ['Smartphones', 'phone', 's-l500(2)'],
  ['Enterprise networking', 'computer', 's-l500(3)'], ['Tablets and eBooks', 'tablet', 's-l500(4)'], ['Storage and blank media', 'storage', 's-l500(5)'], ['Lenses and filters', 'camera', 's-l500(6)'],
]
const trending = [
  ['Tech', 'laptop', 's-l500(22)'], ['Motors', 'car', 's-l500(23)'], ['Luxury', 'watch', 's-l500(24)'], ['Collectibles and art', 'collectible', 's-l500(25)'],
  ['Home and garden', 'home', 's-l500(26)'], ['Trading cards', 'card', 's-l500(27)'], ['Health and beauty', 'beauty', 's-l500(28)'],
]
const slides = [
  { title: 'Get more with an eBay account', text: 'Enjoy exclusive benefits-deals, app-only coupons, and more.', cta: 'Sign in', path: '/login', image: 's-l1600', kind: 'photo' },
  { title: 'Get help exactly when you need it', text: 'From shopping to shipping, find answers to your questions.', cta: 'Start here', path: '/help', image: 's-l1600(1)', kind: 'photo help-slide' },
  { title: 'Find your next favorite thing', text: 'Discover great finds for every side of you.', cta: 'Explore now', path: '/catalog', image: 's-l960', kind: 'green-slide' },
  { title: 'A little something for every style', text: 'Find the brands and pieces that make it yours.', cta: 'Shop now', path: '/catalog?q=shoes', image: 's-l960(1)', kind: 'pink-slide' },
]
const money = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })

function CategoryStrip({ title, items }: { title: string; items: string[][] }) {
  return <section className="home-section"><h2>{title}</h2><div className="category-tiles">{items.map(([label, keyword, asset]) => <Link key={label} to={`/catalog?q=${keyword}`}><div className="category-picture"><img src={img(asset)} alt="" loading="lazy"/></div><span>{label}</span></Link>)}</div></section>
}

function ProductDiscovery() {
  const [attempt, setAttempt] = useState(0)
  const [result, setResult] = useState<{ attempt: number; items: ProductSummary[]; error: boolean } | null>(null)
  const row = useRef<HTMLDivElement>(null)
  const { has, toggle } = useWatchlist()
  const current = result?.attempt === attempt ? result : null
  useEffect(() => {
    const controller = new AbortController()
    getProducts(1, 10, controller.signal).then(data => {
      if (!controller.signal.aborted) setResult({ attempt, items: data.items, error: false })
    }).catch(() => { if (!controller.signal.aborted) setResult({ attempt, items: [], error: true }) })
    return () => controller.abort()
  }, [attempt])
  return <section className="home-section discovery-section"><div className="section-heading"><div><h2>Discover your next great find</h2><p className="muted">Explore the latest items in our catalog</p></div><Link to="/catalog">See all <Icon name="arrow" size={22}/></Link></div>
    {!current ? <div className="product-skeletons" role="status" aria-label="Loading items">{[1,2,3,4,5].map(value => <div key={value}/>)}</div>
      : current.error ? <div className="home-catalog-message"><Icon name="bag" size={28}/><div><h3>More great finds are on their way</h3><p>We couldn't load the catalog right now. Please try again.</p></div><button className="pill outline" onClick={() => setAttempt(value => value + 1)}>Try again</button></div>
      : current.items.length === 0 ? <div className="home-catalog-message"><Icon name="bag" size={28}/><div><h3>New finds are on their way</h3><p>Check back soon for new listings.</p></div></div>
      : <div className="product-carousel"><div className="discovery-products" ref={row}>{current.items.map(product => <article key={product.productId}><div className="discovery-image"><Link to={`/products/${product.productId}`}><ProductImage key={product.imageUrl} url={product.imageUrl} name={product.name}/></Link><button className={`watch-heart ${has(product.productId) ? 'is-saved' : ''}`} aria-label={`${has(product.productId) ? 'Unwatch' : 'Watch'} ${product.name}`} aria-pressed={has(product.productId)} onClick={() => toggle(product)}><Icon name="heart" size={21}/></button></div><Link className="discovery-name" to={`/products/${product.productId}`}>{product.name}</Link><p className="discovery-price">{money.format(product.price)}</p>{product.listingType === 'AUCTION' && <span className="muted">Current bid</span>}</article>)}</div>{current.items.length > 4 && <button className="carousel-next" aria-label="Show more items" onClick={() => row.current?.scrollBy({ left: 580, behavior: 'smooth' })}><Icon name="arrow"/></button>}</div>}
  </section>
}

export function HomePage() {
  const [index, setIndex] = useState(0)
  const [playing, setPlaying] = useState(false)
  const slide = slides[index]
  useEffect(() => {
    if (!playing) return
    const timer = window.setInterval(() => setIndex(value => (value + 1) % slides.length), 6000)
    return () => clearInterval(timer)
  }, [playing])
  function move(direction: number) { setIndex(value => (value + direction + slides.length) % slides.length) }
  return <>
    <section className={`home-hero ${slide.kind}`} aria-label="Featured collections" aria-roledescription="carousel">
      <img className="hero-background" src={img(slide.image)} alt="" fetchPriority="high"/>
      <div className="hero-copy" aria-live={playing ? 'off' : 'polite'}><h1>{slide.title}</h1><p>{slide.text}</p><Link className="pill light" to={slide.path}>{slide.cta}</Link></div>
      <div className="hero-dots">{slides.map((item, i) => <button key={item.title} className={index === i ? 'active' : ''} aria-label={`Show slide ${i + 1}: ${item.title}`} aria-pressed={index === i} onClick={() => setIndex(i)}/>)}</div>
      <div className="hero-controls"><button aria-label="Previous slide" onClick={() => move(-1)}><Icon name="chevron" size={20} style={{ transform: 'rotate(90deg)' }}/></button><button aria-label="Next slide" onClick={() => move(1)}><Icon name="chevron" size={20} style={{ transform: 'rotate(-90deg)' }}/></button><button aria-label={playing ? 'Pause slideshow' : 'Play slideshow'} onClick={() => setPlaying(value => !value)}>{playing ? <span aria-hidden="true">Ⅱ</span> : <span aria-hidden="true">▶</span>}</button></div>
    </section>
    <CategoryStrip title="The future in your hands" items={technology}/>
    <section className="shopping-banner"><div><h2>Shopping made easy</h2><p>Find your favorites, manage your cart and choose where they go.</p></div><Link className="pill dark" to="/catalog">Start now</Link></section>
    <section className="perks-banner"><img src={img('s-l1600(2)')} alt="" loading="lazy"/><div><h2>Don't miss out on insider perks</h2><p>What are you waiting for? Make the most of your buyer account.</p><Link className="pill light" to="/login">Sign in</Link></div></section>
    <ProductDiscovery/>
    <CategoryStrip title="Trending on eBay" items={trending}/>
    <section className="bottom-banner"><div><h2>Find it. Love it. Save it.</h2><p>Keep your favorites close with your Watchlist.</p><Link className="pill dark" to="/watchlist">See your saved items <Icon name="arrow" size={18}/></Link></div><img src={img('s-l960(2)')} alt="" loading="lazy"/></section>
  </>
}
