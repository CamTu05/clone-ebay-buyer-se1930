import { Link } from 'react-router-dom'
import { Icon } from '../../shared/components/Icon'
import { ProductImage } from '../catalog/ProductImage'
import { useWatchlist } from './watchlistStore'

const money = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })

export function WatchlistPage() {
  const { items, toggle, persistent } = useWatchlist()
  return <section className="saved-page">
    <p className="breadcrumb"><Link to="/">Home</Link><span>›</span>My eBay</p>
    <h1>My eBay</h1>
    <nav className="activity-tabs" aria-label="My eBay"><Link className="active" to="/watchlist">Activity</Link><Link to="/profile">Account</Link></nav>
    <div className="activity-layout">
      <aside className="activity-nav"><Link to="/orders">Purchase history</Link><Link className="active" to="/watchlist">Watchlist <span>{items.length}</span></Link><Link to="/cart">Shopping cart</Link><Link to="/catalog?type=AUCTION">Bids & offers</Link></aside>
      <div><div className="section-heading"><h2>Watchlist <span className="muted">({items.length})</span></h2><Link to="/catalog">Find more items <span aria-hidden="true">→</span></Link></div>
        <p className="muted saved-note">{persistent ? 'Saved on this device' : 'Saved for this session'} · Prices and availability may change. Open an item for its latest details.</p>
        {items.length === 0 ? <div className="empty-state"><Icon name="heart" size={52}/><h2>Your next great find starts here</h2><p>Keep an eye on the things you love. Select the heart on any item to save it.</p><Link className="pill primary" to="/catalog">Explore items</Link></div>
          : <div className="saved-grid">{items.map(item => <article className="saved-card" key={item.productId}><Link className="saved-image" to={`/products/${item.productId}`}><ProductImage key={item.imageUrl} url={item.imageUrl} name={item.name}/></Link><Link className="saved-title" to={`/products/${item.productId}`}>{item.name}</Link><p className="saved-price">{money.format(item.price)}</p><p className="muted">{item.listingType === 'AUCTION' ? 'Saved current bid' : 'Saved price'}</p><button className="text-link" onClick={() => toggle(item)}>Remove from Watchlist</button></article>)}</div>}
      </div>
    </div>
  </section>
}
