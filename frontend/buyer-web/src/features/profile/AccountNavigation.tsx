import { Link, NavLink } from 'react-router-dom'
import type { ReactNode } from 'react'
import '../account.css'

export function AccountNavigation({ children }: { children: ReactNode }) {
  return <div className="buyer-account">
    <nav className="account-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span>›</span><span>My eBay</span></nav>
    <h1 className="account-heading">My eBay</h1>
    <nav className="account-tabs" aria-label="My eBay sections">
      <Link to="/orders">Activity</Link><NavLink to="/profile" className="selected">Account</NavLink>
    </nav>
    <div className="account-layout">
      <aside className="account-sidebar" aria-label="Account settings">
        <h2>Personal info</h2>
        <NavLink to="/profile">Personal information</NavLink>
        <NavLink to="/addresses">Addresses</NavLink>
        <h2>Buying</h2>
        <NavLink to="/orders">Purchase history</NavLink>
        <NavLink to="/cart">Shopping cart</NavLink>
        <Link to="/catalog">Browse items</Link>
      </aside>
      <div className="account-content">{children}</div>
    </div>
  </div>
}
