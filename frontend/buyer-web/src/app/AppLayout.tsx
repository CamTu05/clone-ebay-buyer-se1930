import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../features/authentication/useAuth'
import { useWatchlist } from '../features/watchlist/watchlistStore'
import { Icon } from '../shared/components/Icon'

const categories = ['Electronics', 'Motors', 'Fashion', 'Collectibles and art', 'Sports', 'Health and beauty', 'Industrial equipment', 'Home and garden']
const keywords = ['laptop', 'car', 'shoes', 'collectible', 'sport', 'beauty', 'tools', 'home']
const footerGroups = [
  { title: 'Buy', links: [['Shop all items', '/catalog'], ['Registration', '/register'], ['Bidding & buying help', '/help'], ['Watchlist', '/watchlist'], ['Shipping addresses', '/addresses']] },
  { title: 'My eBay', links: [['Purchase history', '/orders'], ['Bids & won items', '/bids'], ['Personal information', '/profile'], ['Shopping cart', '/cart'], ['Saved items', '/watchlist']] },
  { title: 'Explore', links: [['Electronics', '/catalog?q=laptop'], ['Fashion', '/catalog?q=shoes'], ['Home and garden', '/catalog?q=home'], ['Collectibles', '/catalog?q=collectible']] },
  { title: 'Help & Contact', links: [['Buying help', '/help'], ['Your account', '/profile'], ['Delivery addresses', '/addresses'], ['Returns information', '/help']] },
]

function SearchBox() {
  const location = useLocation()
  const params = new URLSearchParams(location.search)
  const [query, setQuery] = useState(params.get('q') ?? '')
  const [category, setCategory] = useState(params.get('category') ?? '')
  const navigate = useNavigate()
  function search(event: FormEvent) {
    event.preventDefault()
    const next = new URLSearchParams()
    if (query.trim()) next.set('q', query.trim())
    if (category) next.set('category', category)
    navigate(`/catalog${next.size ? `?${next}` : ''}`)
  }
  return <form className="global-search" role="search" onSubmit={search}>
    <div className="search-field"><Icon name="search" size={20}/><label className="sr-only" htmlFor="global-query">Search for anything</label><input id="global-query" type="search" placeholder="Search for anything" value={query} onChange={event => setQuery(event.target.value)}/><Link className="image-search" to="/help" aria-label="Search help"><Icon name="camera" size={20}/></Link><label className="sr-only" htmlFor="search-category">Search category</label><select id="search-category" value={category} onChange={event => setCategory(event.target.value)}><option value="">All Categories</option>{categories.map(name => <option key={name}>{name}</option>)}</select></div>
    <button className="search-submit" type="submit">Search</button>
    <div style={{ display: 'flex', flexDirection: 'column', gap: '3px', alignItems: 'flex-start', flexShrink: 0 }}>
      <Link className="advanced-link" to="/catalog">Advanced</Link>
      <Link className="advanced-link" to="/advanced-search" style={{ color: '#0064d2', fontWeight: 600 }}>Advanced 2</Link>
    </div>
  </form>
}

export function AppLayout() {
  const { isAuthenticated, logout, user } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const { items } = useWatchlist()
  const [menu, setMenu] = useState<string | null>(null)
  const headerRef = useRef<HTMLElement>(null)
  const authPaths = ['/login', '/register', '/verify-email', '/forgot-password', '/reset-password', '/resend-verification', '/change-password']
  const simple = authPaths.includes(location.pathname)

  useEffect(() => {
    window.scrollTo({ top: 0 })
    const headings: Record<string, string> = { '/': 'Electronics, Fashion, Collectibles & More', '/catalog': 'Shop and find your next great item', '/advanced-search': 'Advanced Search', '/login': 'Sign in', '/register': 'Create an account', '/verify-email': 'Verify your email', '/forgot-password': 'Reset your password', '/reset-password': 'Choose a new password', '/change-password': 'Change password', '/resend-verification': 'Resend verification', '/cart': 'Shopping cart', '/watchlist': 'Watchlist', '/profile': 'My eBay', '/addresses': 'Shipping addresses', '/checkout': 'Checkout', '/orders': 'Purchase history' }
    document.title = `${headings[location.pathname] ?? 'Buyer'} | eBay Clone`
  }, [location.pathname, location.search])

  useEffect(() => {
    if (!menu) return
    const closeOutside = (event: PointerEvent) => { if (!headerRef.current?.contains(event.target as Node)) setMenu(null) }
    const escape = (event: KeyboardEvent) => { if (event.key === 'Escape') setMenu(null) }
    document.addEventListener('pointerdown', closeOutside)
    document.addEventListener('keydown', escape)
    return () => { document.removeEventListener('pointerdown', closeOutside); document.removeEventListener('keydown', escape) }
  }, [menu])

  const activeMenu = menu?.startsWith(`${location.key}:`) ? menu.slice(location.key.length + 1) : null
  function toggle(name: string) { setMenu(current => current === `${location.key}:${name}` ? null : `${location.key}:${name}`) }
  function signOut() { logout(); setMenu(null); navigate('/') }

  return <div className="buyer-app">
    <a className="skip-link" href="#main-content">Skip to main content</a>
    <header className={`site-header ${simple ? 'simple-header' : ''}`} ref={headerRef}>
      {simple ? <div className="site-width simple-header-inner"><Link to="/" aria-label="eBay Clone home"><img className="ebay-logo" src="/images/ebay/logo.svg" alt="eBay"/></Link><Link to="/help">Help & Contact</Link></div> : <>
        <div className="utility-bar"><div className="site-width utility-inner">
          <div className="utility-left"><span>Hi! {isAuthenticated ? <><Link to="/profile">{user?.username}</Link> <button className="inline-link" onClick={signOut}>Sign out</button></> : <><Link to="/login">Sign in</Link> or <Link to="/register">register</Link></>}</span><Link to="/deals">Deals</Link><Link to="/deals">Brand Outlet</Link><Link to="/help">Help & Contact</Link></div>
          <div className="utility-right"><Link className="ship-link" to="/addresses"><Icon name="truck" size={18}/> Ship to Vietnam</Link><Link className="sell-link" to="/help">Sell <Icon name="chevron" size={12}/></Link>
            <div className="menu-anchor"><button className="utility-menu" aria-expanded={activeMenu === 'watchlist'} onClick={() => toggle('watchlist')}>Watchlist <Icon name="chevron" size={12}/></button>{activeMenu === 'watchlist' && <div className="header-popover watch-popover"><h2>Your Watchlist</h2><p>{items.length ? `${items.length} saved ${items.length === 1 ? 'item' : 'items'} on this device.` : 'Keep track of the items you love.'}</p><Link className="pill primary" to="/watchlist">View Watchlist</Link></div>}</div>
            <div className="menu-anchor"><button className="utility-menu" aria-expanded={activeMenu === 'account'} onClick={() => toggle('account')}>My eBay <Icon name="chevron" size={12}/></button>{activeMenu === 'account' && <nav className="header-popover account-popover" aria-label="My eBay menu"><Link to="/orders">Summary</Link><Link to="/orders">Purchase history</Link><Link to="/watchlist">Watchlist</Link><Link to="/catalog?type=AUCTION">Bids & offers</Link><Link to="/profile">Account settings</Link><Link to="/addresses">Shipping addresses</Link></nav>}</div>
            <Link className="icon-button" to="/notifications" aria-label="Notifications"><Icon name="bell"/></Link><Link className="icon-button" to="/cart" aria-label="Shopping cart"><Icon name="cart" size={24}/></Link>
          </div>
        </div></div>
        <div className="site-width search-row"><Link to="/" aria-label="eBay Clone home"><img className="ebay-logo" src="/images/ebay/logo.svg" alt="eBay"/></Link><div className="menu-anchor category-anchor"><button className="category-trigger" aria-expanded={activeMenu === 'categories'} onClick={() => toggle('categories')}>Shop by<br/>category <Icon name="chevron" size={14}/></button>{activeMenu === 'categories' && <div className="header-popover category-menu"><h2>Shop by category</h2><div>{categories.map((name, i) => <Link key={name} to={`/catalog?q=${keywords[i]}`}>{name}<Icon name="arrow" size={16}/></Link>)}</div><Link className="text-link" to="/catalog">See all items</Link></div>}</div><SearchBox key={location.search}/></div>
      </>}
    </header>
    {!simple && location.pathname === '/' && <nav className="category-nav" aria-label="Popular categories"><div className="site-width"><Link to="/watchlist">Saved</Link>{categories.map((name, i) => <Link to={`/catalog?q=${keywords[i]}`} key={name}>{name}</Link>)}<Link to="/deals">Deals</Link><Link to="/help">Sell</Link></div></nav>}
    <main id="main-content" className={`site-width main-content ${location.pathname === '/' ? 'home-main' : ''} ${simple ? 'login-main' : ''}`}><Outlet/></main>
    <footer className={`site-footer ${simple ? 'simple-footer' : ''}`}>
      {!simple && <div className="site-width footer-columns">{footerGroups.map(group => <div key={group.title}><h2>{group.title}</h2>{group.links.map(([label, to]) => <Link key={label} to={to}>{label}</Link>)}</div>)}<div><h2>About this project</h2><Link to="/help">Buyer experience</Link><a href="https://www.ebay.com/" target="_blank" rel="noreferrer">Visit the original eBay</a><h2 className="footer-sites">eBay Sites</h2><a className="site-country" href="https://www.ebay.com/" target="_blank" rel="noreferrer">🇺🇸 United States <Icon name="chevron" size={13}/></a></div></div>}
      <div className="site-width footer-legal"><p>eBay-inspired buyer project · PRN222. Independent educational implementation.</p><Link to="/help">Help & Contact</Link><a href="https://www.ebay.com/help/policies/member-behaviour-policies/user-agreement?id=4259" target="_blank" rel="noreferrer">eBay User Agreement</a><a href="https://www.ebay.com/help/policies/member-behaviour-policies/user-privacy-notice-privacy-policy?id=4260" target="_blank" rel="noreferrer">eBay Privacy</a></div>
    </footer>
    {!simple && <Link className="floating-help" to="/help" aria-label="Help">?</Link>}
  </div>
}
