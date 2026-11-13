import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { buyerGet } from '../buyerApi'
import { apiClient } from '../../shared/api/apiClient'
import './advancedSearch.css'

interface CategoryItem {
  id: number
  name: string
  slug: string
}

const defaultCategories = [
  'Electronics',
  'Motors',
  'Fashion',
  'Collectibles and art',
  'Sports',
  'Health and beauty',
  'Industrial equipment',
  'Home and garden',
]

export function AdvancedSearchPage() {
  const navigate = useNavigate()
  const [activeTab, setActiveTab] = useState<'items' | 'seller' | 'item-number'>('items')

  // Categories from backend
  const [categories, setCategories] = useState<CategoryItem[]>([])

  useEffect(() => {
    const c = new AbortController()
    buyerGet<CategoryItem[]>('/api/categories', c.signal)
      .then(setCategories)
      .catch(() => {
        /* Fallback handled in dropdown */
      })
    return () => c.abort()
  }, [])

  // Section references for smooth scrolling & focus
  const findItemsRef = useRef<HTMLDivElement>(null)
  const sellersRef = useRef<HTMLDivElement>(null)
  const keywordsInputRef = useRef<HTMLInputElement>(null)
  const sellerInputRef = useRef<HTMLInputElement>(null)
  const itemNumberInputRef = useRef<HTMLInputElement>(null)

  // Form Fields State
  const [keywords, setKeywords] = useState('')
  const [matchType, setMatchType] = useState<'all' | 'any' | 'exact'>('all')
  const [excludeWords, setExcludeWords] = useState('')
  const [category, setCategory] = useState('')
  const [searchDesc, setSearchDesc] = useState(false)
  const [completedListings, setCompletedListings] = useState(false)
  const [soldListings, setSoldListings] = useState(false)

  // Buying Formats
  const [formatAuction, setFormatAuction] = useState(false)
  const [formatBuyItNow, setFormatBuyItNow] = useState(false)
  const [formatAcceptsOffers, setFormatAcceptsOffers] = useState(false)

  // Price Range
  const [minPrice, setMinPrice] = useState('')
  const [maxPrice, setMaxPrice] = useState('')

  // Conditions
  const [conditionNew, setConditionNew] = useState(false)
  const [conditionUsed, setConditionUsed] = useState(false)
  const [conditionRefurbished, setConditionRefurbished] = useState(false)
  const [conditionForParts, setConditionForParts] = useState(false)

  // Listing Time & Bids
  const [endingWithin, setEndingWithin] = useState('')
  const [minBids, setMinBids] = useState('')
  const [maxBids, setMaxBids] = useState('')

  // Shipping & Sellers
  const [freeShipping, setFreeShipping] = useState(false)
  const [localPickup, setLocalPickup] = useState(false)
  const [filterSellers, setFilterSellers] = useState(false)
  const [sellerMode, setSellerMode] = useState<'include' | 'exclude'>('include')
  const [sellerUsername, setSellerUsername] = useState('')

  // Sort & Pagination
  const [sort, setSort] = useState('default')
  const [pageSize, setPageSize] = useState('25')

  // Direct Item ID Form State
  const [directItemId, setDirectItemId] = useState('')
  const [directItemError, setDirectItemError] = useState('')
  const [isCheckingItem, setIsCheckingItem] = useState(false)

  // Validation errors
  const [formError, setFormError] = useState('')
  const [isSearching, setIsSearching] = useState(false)

  // BR-03: UI Dependencies
  // When "Sold listings" is checked -> automatically check "Completed listings"
  function handleSoldChange(checked: boolean) {
    setSoldListings(checked)
    if (checked) {
      setCompletedListings(true)
      setEndingWithin('') // Ending within is disabled for completed/sold listings
    }
  }

  // When "Completed listings" is unchecked -> uncheck "Sold listings"
  function handleCompletedChange(checked: boolean) {
    setCompletedListings(checked)
    if (!checked) {
      setSoldListings(false)
    } else {
      setEndingWithin('')
    }
  }

  // BR-04: Range validation (derived during render)
  const priceRangeError = (() => {
    if (minPrice !== '' && maxPrice !== '') {
      const min = Number(minPrice)
      const max = Number(maxPrice)
      if (Number.isFinite(min) && Number.isFinite(max) && min > max) {
        return 'Min price cannot be greater than Max price.'
      }
    }
    return ''
  })()

  const bidsRangeError = (() => {
    if (minBids !== '' && maxBids !== '') {
      const min = Number(minBids)
      const max = Number(maxBids)
      if (Number.isFinite(min) && Number.isFinite(max) && min > max) {
        return 'Min bids cannot be greater than Max bids.'
      }
    }
    return ''
  })()

  function handleSwapPrices() {
    const temp = minPrice
    setMinPrice(maxPrice)
    setMaxPrice(temp)
  }

  function handleSwapBids() {
    const temp = minBids
    setMinBids(maxBids)
    setMaxBids(temp)
  }

  // Tab Navigation Handling
  function handleTabClick(tab: 'items' | 'seller' | 'item-number') {
    setActiveTab(tab)
    if (tab === 'items') {
      findItemsRef.current?.scrollIntoView({ behavior: 'smooth' })
      setTimeout(() => keywordsInputRef.current?.focus(), 300)
    } else if (tab === 'seller') {
      setFilterSellers(true)
      sellersRef.current?.scrollIntoView({ behavior: 'smooth' })
      setTimeout(() => sellerInputRef.current?.focus(), 300)
    } else if (tab === 'item-number') {
      setTimeout(() => itemNumberInputRef.current?.focus(), 150)
    }
  }

  // Reset / Clear Options (BR-05)
  function handleReset() {
    setKeywords('')
    setMatchType('all')
    setExcludeWords('')
    setCategory('')
    setSearchDesc(false)
    setCompletedListings(false)
    setSoldListings(false)
    setFormatAuction(false)
    setFormatBuyItNow(false)
    setFormatAcceptsOffers(false)
    setMinPrice('')
    setMaxPrice('')
    setConditionNew(false)
    setConditionUsed(false)
    setConditionRefurbished(false)
    setConditionForParts(false)
    setEndingWithin('')
    setMinBids('')
    setMaxBids('')
    setFreeShipping(false)
    setLocalPickup(false)
    setFilterSellers(false)
    setSellerMode('include')
    setSellerUsername('')
    setSort('default')
    setPageSize('25')
    setFormError('')
    setDirectItemId('')
    setDirectItemError('')
  }

  // Direct Item ID Form Submission (BR-02 dedicated mode)
  async function handleDirectItemLookup(e: FormEvent) {
    e.preventDefault()
    const cleanId = directItemId.trim()
    if (!cleanId || !/^\d+$/.test(cleanId)) {
      setDirectItemError('Please enter a valid numeric Item ID.')
      return
    }

    setIsCheckingItem(true)
    setDirectItemError('')
    try {
      const response = await apiClient.get(`/api/products/${cleanId}`)
      if (response.data?.success && response.data?.data) {
        navigate(`/products/${cleanId}`)
        return
      }
      setDirectItemError(`Item #${cleanId} was not found in the catalog.`)
    } catch {
      setDirectItemError(`Item #${cleanId} was not found in the catalog.`)
    } finally {
      setIsCheckingItem(false)
    }
  }

  // Main Advanced Search Form Submission
  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setFormError('')

    const cleanKeywords = keywords.trim()
    const cleanCategory = category.trim()
    const cleanSeller = filterSellers ? sellerUsername.trim() : ''

    // BR-01: Required Input Validation
    // User must enter at least 1 of: Keyword, Category, or Seller Username
    if (!cleanKeywords && !cleanCategory && !cleanSeller) {
      setFormError('Please enter keywords, choose a specific category, or specify a seller username before searching.')
      findItemsRef.current?.scrollIntoView({ behavior: 'smooth' })
      keywordsInputRef.current?.focus()
      return
    }

    // Range Validation Check
    if (priceRangeError || bidsRangeError) {
      return
    }

    // BR-02: Direct Item ID Lookup
    // If the keyword is a pure numeric string, check if it matches an existing Product/Item ID in DB
    if (/^\d+$/.test(cleanKeywords)) {
      setIsSearching(true)
      try {
        const response = await apiClient.get(`/api/products/${cleanKeywords}`)
        if (response.data?.success && response.data?.data) {
          // Found matching item! Directly navigate to Product Detail
          navigate(`/products/${cleanKeywords}`)
          return
        }
      } catch {
        // Not found as direct ID, proceed with standard catalog search
      } finally {
        setIsSearching(false)
      }
    }

    // BR-05: Query & Routing
    const params = new URLSearchParams()

    if (cleanKeywords) params.set('q', cleanKeywords)
    if (matchType !== 'all') params.set('match', matchType)
    if (excludeWords.trim()) params.set('exclude', excludeWords.trim())
    if (cleanCategory) params.set('category', cleanCategory)
    if (searchDesc) params.set('desc', 'true')
    if (completedListings) params.set('completed', 'true')
    if (soldListings) params.set('sold', 'true')

    // Buying Formats
    if (formatAuction && !formatBuyItNow) {
      params.set('type', 'AUCTION')
    } else if (formatBuyItNow && !formatAuction) {
      params.set('type', 'FIXED_PRICE')
    }

    // Price Range
    if (minPrice.trim()) params.set('min', minPrice.trim())
    if (maxPrice.trim()) params.set('max', maxPrice.trim())

    // Conditions
    const conditions: string[] = []
    if (conditionNew) conditions.push('New')
    if (conditionUsed) conditions.push('Used')
    if (conditionRefurbished) conditions.push('Refurbished')
    if (conditionForParts) conditions.push('For parts')
    if (conditions.length > 0) params.set('condition', conditions.join(','))

    // Listing Time & Bids
    if (endingWithin && !completedListings && !soldListings) {
      params.set('endingWithin', endingWithin)
    }
    if (minBids.trim()) params.set('minBids', minBids.trim())
    if (maxBids.trim()) params.set('maxBids', maxBids.trim())

    // Shipping & Sellers
    if (freeShipping) params.set('freeShipping', 'true')
    if (localPickup) params.set('localPickup', 'true')
    if (cleanSeller) {
      params.set('seller', cleanSeller)
      params.set('sellerMode', sellerMode)
    }

    // Sort & Pagination
    if (sort !== 'default') params.set('sort', sort)
    if (pageSize !== '25') params.set('pageSize', pageSize)

    // Navigate to Search & Category Results Screen
    navigate(`/catalog?${params.toString()}`)
  }

  return (
    <div className="eb-advanced-search">
      {/* Page Header */}
      <div className="eb-adv-header">
        <div>
          <h1>Advanced Search</h1>
          <p>Find items with detailed filters, prices, conditions, and seller options.</p>
        </div>
        <div className="eb-adv-actions-top">
          {activeTab !== 'item-number' && (
            <>
              <button
                type="button"
                className="eb-adv-btn-secondary"
                onClick={handleReset}
              >
                Clear options
              </button>
              <button
                type="button"
                className="eb-adv-btn-primary"
                onClick={e => void handleSubmit(e)}
                disabled={isSearching}
              >
                {isSearching ? 'Searching…' : 'Search'}
              </button>
            </>
          )}
        </div>
      </div>

      <div className="eb-adv-layout">
        {/* Left Anchor Navigation Tabs */}
        <aside className="eb-adv-nav" aria-label="Advanced search navigation">
          <h2>Search options</h2>
          <ul>
            <li>
              <button
                type="button"
                className={`eb-adv-nav-item ${activeTab === 'items' ? 'is-active' : ''}`}
                onClick={() => handleTabClick('items')}
              >
                Find items
              </button>
            </li>
            <li>
              <button
                type="button"
                className={`eb-adv-nav-item ${activeTab === 'seller' ? 'is-active' : ''}`}
                onClick={() => handleTabClick('seller')}
              >
                By seller
              </button>
            </li>
            <li>
              <button
                type="button"
                className={`eb-adv-nav-item ${activeTab === 'item-number' ? 'is-active' : ''}`}
                onClick={() => handleTabClick('item-number')}
              >
                By item number
              </button>
            </li>
          </ul>
        </aside>

        {/* Main Content Area */}
        <main>
          {/* TAB 3: By Item Number Dedicated Form */}
          {activeTab === 'item-number' ? (
            <div className="eb-adv-item-number-box">
              <h2>Find item by Item Number</h2>
              <p>Enter the specific eBay Item / Product ID to view the product detail directly.</p>
              <form onSubmit={e => void handleDirectItemLookup(e)}>
                <div className="eb-adv-row">
                  <label htmlFor="direct-item-id" className="eb-adv-label">
                    Item number
                  </label>
                  <input
                    id="direct-item-id"
                    ref={itemNumberInputRef}
                    type="text"
                    inputMode="numeric"
                    pattern="[0-9]*"
                    className="eb-adv-input"
                    placeholder="e.g. 1, 2, 10"
                    value={directItemId}
                    onChange={e => setDirectItemId(e.target.value.replace(/\D/g, ''))}
                    required
                  />
                  {directItemError && (
                    <span className="eb-adv-inline-error">{directItemError}</span>
                  )}
                </div>
                <div style={{ marginTop: 24, display: 'flex', gap: 12 }}>
                  <button
                    type="submit"
                    className="eb-adv-btn-primary"
                    disabled={isCheckingItem || !directItemId.trim()}
                  >
                    {isCheckingItem ? 'Checking…' : 'Lookup Item'}
                  </button>
                  <button
                    type="button"
                    className="eb-adv-btn-secondary"
                    onClick={() => {
                      setDirectItemId('')
                      setDirectItemError('')
                      setActiveTab('items')
                    }}
                  >
                    Back to Find items
                  </button>
                </div>
              </form>
            </div>
          ) : (
            /* TAB 1 & 2: Single-Page Unified Form */
            <form onSubmit={e => void handleSubmit(e)} className="eb-adv-main" noValidate>
              {/* Validation Alert (BR-01) */}
              {formError && (
                <div className="eb-adv-alert" role="alert">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <circle cx="12" cy="12" r="10" />
                    <line x1="12" y1="8" x2="12" y2="12" />
                    <line x1="12" y1="16" x2="12.01" y2="16" />
                  </svg>
                  <span>{formError}</span>
                </div>
              )}

              {/* SECTION 1: Keywords & Scope */}
              <div id="find-items" ref={findItemsRef} className="eb-adv-section">
                <h2 className="eb-adv-section-title">Keywords & Scope</h2>
                <p className="eb-adv-section-desc">Search for words, phrases, or specific categories.</p>

                <div className="eb-adv-row">
                  <label htmlFor="adv-keywords" className="eb-adv-label">
                    Enter keywords or item number
                  </label>
                  <input
                    id="adv-keywords"
                    ref={keywordsInputRef}
                    type="search"
                    className="eb-adv-input"
                    placeholder="e.g. laptop dell, iPhone, 1"
                    value={keywords}
                    onChange={e => setKeywords(e.target.value)}
                  />
                </div>

                <div className="eb-adv-row">
                  <label htmlFor="adv-match-type" className="eb-adv-label">
                    Keyword match type
                  </label>
                  <select
                    id="adv-match-type"
                    className="eb-adv-select"
                    value={matchType}
                    onChange={e => setMatchType(e.target.value as 'all' | 'any' | 'exact')}
                  >
                    <option value="all">All words, any order</option>
                    <option value="any">Any words, any order</option>
                    <option value="exact">Exact words, exact order</option>
                  </select>
                </div>

                <div className="eb-adv-row">
                  <label htmlFor="adv-exclude" className="eb-adv-label">
                    Exclude words from search
                  </label>
                  <input
                    id="adv-exclude"
                    type="text"
                    className="eb-adv-input"
                    placeholder="e.g. broken, case, cable"
                    value={excludeWords}
                    onChange={e => setExcludeWords(e.target.value)}
                  />
                </div>

                <div className="eb-adv-row">
                  <label htmlFor="adv-category" className="eb-adv-label">
                    In this category
                  </label>
                  <select
                    id="adv-category"
                    className="eb-adv-select"
                    value={category}
                    onChange={e => setCategory(e.target.value)}
                  >
                    <option value="">All Categories</option>
                    {categories.length > 0
                      ? categories.map(cat => (
                          <option key={cat.id} value={cat.name}>
                            {cat.name}
                          </option>
                        ))
                      : defaultCategories.map(name => (
                          <option key={name} value={name}>
                            {name}
                          </option>
                        ))}
                  </select>
                </div>

                <div className="eb-adv-row">
                  <span className="eb-adv-label">Search including</span>
                  <div className="eb-adv-checkbox-group">
                    <label className="eb-adv-checkbox-label">
                      <input
                        type="checkbox"
                        checked={searchDesc}
                        onChange={e => setSearchDesc(e.target.checked)}
                      />
                      Title and description
                    </label>
                    <label className="eb-adv-checkbox-label">
                      <input
                        type="checkbox"
                        checked={completedListings}
                        onChange={e => handleCompletedChange(e.target.checked)}
                      />
                      Completed listings
                    </label>
                    <label className="eb-adv-checkbox-label">
                      <input
                        type="checkbox"
                        checked={soldListings}
                        onChange={e => handleSoldChange(e.target.checked)}
                      />
                      Sold listings
                    </label>
                  </div>
                </div>
              </div>

              {/* SECTION 2: Buying Formats */}
              <div className="eb-adv-section">
                <h2 className="eb-adv-section-title">Buying Formats</h2>
                <div className="eb-adv-checkbox-group">
                  <label className="eb-adv-checkbox-label">
                    <input
                      type="checkbox"
                      checked={formatAuction}
                      onChange={e => setFormatAuction(e.target.checked)}
                    />
                    Auction
                  </label>
                  <label className="eb-adv-checkbox-label">
                    <input
                      type="checkbox"
                      checked={formatBuyItNow}
                      onChange={e => setFormatBuyItNow(e.target.checked)}
                    />
                    Buy It Now
                  </label>
                  <label className="eb-adv-checkbox-label">
                    <input
                      type="checkbox"
                      checked={formatAcceptsOffers}
                      onChange={e => setFormatAcceptsOffers(e.target.checked)}
                    />
                    Accepts Offers
                  </label>
                </div>
              </div>

              {/* SECTION 3: Price Range */}
              <div className="eb-adv-section">
                <h2 className="eb-adv-section-title">Price Range (VND)</h2>
                <div className="eb-adv-range-inputs">
                  <div className="eb-adv-range-field">
                    <span>From (Min price)</span>
                    <input
                      type="number"
                      min="0"
                      step="1000"
                      className="eb-adv-input"
                      placeholder="0 ₫"
                      value={minPrice}
                      onChange={e => setMinPrice(e.target.value)}
                    />
                  </div>
                  <span className="eb-adv-range-divider">to</span>
                  <div className="eb-adv-range-field">
                    <span>To (Max price)</span>
                    <input
                      type="number"
                      min="0"
                      step="1000"
                      className="eb-adv-input"
                      placeholder="Unlimited"
                      value={maxPrice}
                      onChange={e => setMaxPrice(e.target.value)}
                    />
                  </div>
                  {priceRangeError && (
                    <button
                      type="button"
                      className="eb-adv-swap-btn"
                      onClick={handleSwapPrices}
                      title="Swap min and max prices"
                    >
                      ⇄ Swap
                    </button>
                  )}
                </div>
                {priceRangeError && (
                  <span className="eb-adv-inline-error">{priceRangeError}</span>
                )}
              </div>

              {/* SECTION 4: Condition */}
              <div className="eb-adv-section">
                <h2 className="eb-adv-section-title">Condition</h2>
                <div className="eb-adv-checkbox-group">
                  <label className="eb-adv-checkbox-label">
                    <input
                      type="checkbox"
                      checked={conditionNew}
                      onChange={e => setConditionNew(e.target.checked)}
                    />
                    New
                  </label>
                  <label className="eb-adv-checkbox-label">
                    <input
                      type="checkbox"
                      checked={conditionUsed}
                      onChange={e => setConditionUsed(e.target.checked)}
                    />
                    Used
                  </label>
                  <label className="eb-adv-checkbox-label">
                    <input
                      type="checkbox"
                      checked={conditionRefurbished}
                      onChange={e => setConditionRefurbished(e.target.checked)}
                    />
                    Refurbished
                  </label>
                  <label className="eb-adv-checkbox-label">
                    <input
                      type="checkbox"
                      checked={conditionForParts}
                      onChange={e => setConditionForParts(e.target.checked)}
                    />
                    For parts or not working
                  </label>
                </div>
              </div>

              {/* SECTION 5: Listing Time & Bids */}
              <div className="eb-adv-section">
                <h2 className="eb-adv-section-title">Listing Time & Bids</h2>

                <div className="eb-adv-row">
                  <label htmlFor="adv-ending-within" className="eb-adv-label">
                    Ending within {completedListings || soldListings ? '(Disabled for completed/sold items)' : ''}
                  </label>
                  <select
                    id="adv-ending-within"
                    className="eb-adv-select"
                    value={endingWithin}
                    disabled={completedListings || soldListings}
                    onChange={e => setEndingWithin(e.target.value)}
                  >
                    <option value="">Any time</option>
                    <option value="1h">Ending within 1 hour</option>
                    <option value="2h">Ending within 2 hours</option>
                    <option value="12h">Ending within 12 hours</option>
                    <option value="24h">Ending within 24 hours</option>
                  </select>
                </div>

                <div className="eb-adv-row" style={{ marginTop: 18 }}>
                  <span className="eb-adv-label">Number of bids</span>
                  <div className="eb-adv-range-inputs">
                    <div className="eb-adv-range-field">
                      <span>Min bids</span>
                      <input
                        type="number"
                        min="0"
                        className="eb-adv-input"
                        placeholder="0"
                        value={minBids}
                        onChange={e => setMinBids(e.target.value)}
                      />
                    </div>
                    <span className="eb-adv-range-divider">to</span>
                    <div className="eb-adv-range-field">
                      <span>Max bids</span>
                      <input
                        type="number"
                        min="0"
                        className="eb-adv-input"
                        placeholder="Any"
                        value={maxBids}
                        onChange={e => setMaxBids(e.target.value)}
                      />
                    </div>
                    {bidsRangeError && (
                      <button
                        type="button"
                        className="eb-adv-swap-btn"
                        onClick={handleSwapBids}
                        title="Swap min and max bids"
                      >
                        ⇄ Swap
                      </button>
                    )}
                  </div>
                  {bidsRangeError && (
                    <span className="eb-adv-inline-error">{bidsRangeError}</span>
                  )}
                </div>
              </div>

              {/* SECTION 6: Shipping & Sellers (Anchor: #sellers-section) */}
              <div id="sellers-section" ref={sellersRef} className="eb-adv-section">
                <h2 className="eb-adv-section-title">Shipping & Sellers</h2>

                <div className="eb-adv-row">
                  <span className="eb-adv-label">Shipping options</span>
                  <div className="eb-adv-checkbox-group">
                    <label className="eb-adv-checkbox-label">
                      <input
                        type="checkbox"
                        checked={freeShipping}
                        onChange={e => setFreeShipping(e.target.checked)}
                      />
                      Free shipping
                    </label>
                    <label className="eb-adv-checkbox-label">
                      <input
                        type="checkbox"
                        checked={localPickup}
                        onChange={e => setLocalPickup(e.target.checked)}
                      />
                      Local pickup
                    </label>
                  </div>
                </div>

                <div className="eb-adv-row" style={{ marginTop: 22 }}>
                  <label className="eb-adv-checkbox-label" style={{ fontWeight: 600, fontSize: 15 }}>
                    <input
                      type="checkbox"
                      checked={filterSellers}
                      onChange={e => setFilterSellers(e.target.checked)}
                    />
                    Only show items from / Exclude items from sellers
                  </label>

                  {filterSellers && (
                    <div style={{ marginTop: 14, paddingLeft: 26 }}>
                      <div className="eb-adv-checkbox-group" style={{ marginBottom: 12 }}>
                        <label className="eb-adv-radio-label">
                          <input
                            type="radio"
                            name="sellerMode"
                            value="include"
                            checked={sellerMode === 'include'}
                            onChange={() => setSellerMode('include')}
                          />
                          Include (Only show from these sellers)
                        </label>
                        <label className="eb-adv-radio-label">
                          <input
                            type="radio"
                            name="sellerMode"
                            value="exclude"
                            checked={sellerMode === 'exclude'}
                            onChange={() => setSellerMode('exclude')}
                          />
                          Exclude (Exclude these sellers)
                        </label>
                      </div>
                      <label htmlFor="adv-seller-username" className="eb-adv-label">
                        Seller username
                      </label>
                      <input
                        id="adv-seller-username"
                        ref={sellerInputRef}
                        type="text"
                        className="eb-adv-input"
                        placeholder="e.g. tech_store, bestdeals"
                        value={sellerUsername}
                        onChange={e => setSellerUsername(e.target.value)}
                      />
                    </div>
                  )}
                </div>
              </div>

              {/* SECTION 7: Sort & Pagination */}
              <div className="eb-adv-section">
                <h2 className="eb-adv-section-title">Sort & View Options</h2>

                <div className="eb-adv-row">
                  <label htmlFor="adv-sort" className="eb-adv-label">
                    Sort by
                  </label>
                  <select
                    id="adv-sort"
                    className="eb-adv-select"
                    value={sort}
                    onChange={e => setSort(e.target.value)}
                  >
                    <option value="default">Best Match</option>
                    <option value="ending_soon">Time: ending soonest</option>
                    <option value="newest">Time: newly listed</option>
                    <option value="price_asc">Price + Shipping: lowest first</option>
                    <option value="price_desc">Price + Shipping: highest first</option>
                  </select>
                </div>

                <div className="eb-adv-row">
                  <label htmlFor="adv-page-size" className="eb-adv-label">
                    Results per page
                  </label>
                  <select
                    id="adv-page-size"
                    className="eb-adv-select"
                    value={pageSize}
                    onChange={e => setPageSize(e.target.value)}
                  >
                    <option value="25">25 items</option>
                    <option value="50">50 items</option>
                    <option value="100">100 items</option>
                  </select>
                </div>
              </div>

              {/* Bottom Action Bar */}
              <div className="eb-adv-actions-bottom">
                <button
                  type="submit"
                  className="eb-adv-btn-primary"
                  disabled={isSearching}
                >
                  {isSearching ? 'Searching…' : 'Search'}
                </button>
                <button
                  type="button"
                  className="eb-adv-btn-secondary"
                  onClick={handleReset}
                >
                  Clear options
                </button>
                <Link to="/catalog" style={{ marginLeft: 'auto', fontSize: 14, color: '#0064d2' }}>
                  Return to standard search
                </Link>
              </div>
            </form>
          )}
        </main>
      </div>
    </div>
  )
}
