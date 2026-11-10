import { execFileSync } from 'node:child_process';
// Run against an isolated, freshly seeded SQL Server database; never production.
const base = process.env.BUYER_TEST_URL;
if (!base || !process.env.BUYER_TEST_PASSWORD) throw Error('Set BUYER_TEST_URL and BUYER_TEST_PASSWORD for an isolated test server.');
const database = process.env.BUYER_TEST_DATABASE;
if (!/^CloneEbayBuyer_Test_[A-Za-z0-9_]+$/.test(database ?? '')) throw Error('BUYER_TEST_DATABASE must name a dedicated CloneEbayBuyer_Test_ database.');
function sql(query) { return execFileSync('sqlcmd',['-S',process.env.BUYER_TEST_SQL_SERVER ?? '.\\SQLEXPRESS',...(process.env.BUYER_TEST_SQL_USER?['-U',process.env.BUYER_TEST_SQL_USER]:['-E']),'-C','-I','-b','-d',database,'-h','-1','-W','-Q','SET NOCOUNT ON; '+query],{encoding:'utf8'}).trim(); }
let assertions = 0;
function check(value, message) { if (!value) throw Error(message); assertions++; }
async function api(path, token, method='GET', body, expected=200) {
 const r = await fetch(base+path,{method,headers:{'Content-Type':'application/json',...(token?{Authorization:'Bearer '+token}:{})},body:body===undefined?undefined:JSON.stringify(body)});
 const raw=await r.text(); let j;try{j=JSON.parse(raw)}catch{j=raw}
 check(r.status===expected, `${method} ${path}: expected ${expected}, got ${r.status}: ${JSON.stringify(j)}`);return j?.data;
}
async function login(n) { return (await api('/api/auth/login',null,'POST',{email:`seed.buyer${String(n).padStart(2,'0')}@example.test`,password:process.env.BUYER_TEST_PASSWORD})).accessToken; }
const a=await login(1), b=await login(2);
await api('/health/ready');
const catalog=await api('/api/products?pageSize=100');check(catalog.totalItems===48,'seed catalog count');
const searched=await api('/api/products?q=laptop');check(searched.totalItems===8,'search covers catalog');
await api('/api/orders',null,'GET',undefined,401);
for(const path of ['/api/me','/api/addresses','/api/cart','/api/orders','/api/notifications'])await api(path,a);
const fixed=catalog.items.find(x=>x.listingType==='FIXED_PRICE');
await api(`/api/watchlist/${fixed.listingId}`,a,'PUT');await api(`/api/watchlist/${fixed.listingId}`,a,'PUT');
check((await api('/api/watchlist',a)).filter(x=>x.listingId===fixed.listingId).length===1,'idempotent watchlist');
await api(`/api/watchlist/${fixed.listingId}`,a,'DELETE');
for(const item of (await api('/api/cart',a)).items)await api('/api/cart/items/'+item.cartItemId,a,'DELETE',undefined,200);
await api('/api/cart/items',a,'POST',{listingId:fixed.listingId,quantity:1});
const address=(await api('/api/addresses',a))[0];const req={addressId:address.id,method:'PAYPAL'};
const stockBefore=Number(sql('SELECT quantity FROM Listing WHERE id='+fixed.listingId));
const quote=await api('/api/checkout/preview',a,'POST',req);
const order=await api('/api/checkout',a,'POST',req);check(order.totalAmount===quote.totalAmount,'preview equals checkout');
await api(`/api/checkout/${order.checkoutGroupId}/mock-payment`,b,'POST',{success:true},404);
await api(`/api/checkout/${order.checkoutGroupId}/mock-payment`,a,'POST',{success:false});
await api(`/api/checkout/${order.checkoutGroupId}/mock-payment`,a,'POST',{success:true});
check(Number(sql('SELECT quantity FROM Listing WHERE id='+fixed.listingId))===stockBefore-1,'checkout decrements stock once');
const repeated=await api(`/api/checkout/${order.checkoutGroupId}/mock-payment`,a,'POST',{success:true});check(repeated.alreadyProcessed===true,'payment idempotency');
await api(`/api/checkout/${order.checkoutGroupId}/cancel`,a,'POST',{},409);
await api(`/api/orders/${order.orderIds[0]}/cancellation-requests`,a,'POST',{reason:'Test cancellation'});
await api(`/api/orders/${order.orderIds[0]}/cancellation-requests`,a,'POST',{reason:'Duplicate'},409);
await api('/api/cart/items',a,'POST',{listingId:fixed.listingId,quantity:1});
const unpaid=await api('/api/checkout',a,'POST',req);
await api(`/api/checkout/${unpaid.checkoutGroupId}/cancel`,a,'POST');await api(`/api/checkout/${unpaid.checkoutGroupId}/cancel`,a,'POST');
await api(`/api/checkout/${unpaid.checkoutGroupId}/mock-payment`,a,'POST',{success:true},409);
check(Number(sql('SELECT quantity FROM Listing WHERE id='+fixed.listingId))===stockBefore-1,'cancellation restores stock exactly once');
check(Number(sql("SELECT COUNT(*) FROM Payment WHERE status='PAID' AND checkoutGroupId="+order.checkoutGroupId))===1,'one paid DB record');
const auctionProduct=catalog.items.find(x=>x.listingType==='AUCTION');const detail=await api(`/api/products/${auctionProduct.productId}`);const auctionId=detail.listings[0].auctionId;check(auctionId>0,'auction linked');
const auction=await api(`/api/auctions/${auctionId}`);check(!('maximumBid' in auction),'maximum bid private');
await api(`/api/auctions/${auctionId}/bids`,a,'POST',{maximumBid:Math.max(auction.currentPrice,Number(sql('SELECT COALESCE(MAX(maximumBid),0) FROM Bid WHERE auctionId='+auctionId)))+auction.bidIncrement*100});
await api(`/api/auctions/${auctionId}/bids`,b,'POST',{maximumBid:1},409);
// Race two buyers for the final unit; exactly one transaction may commit.
const c=await login(11), d=await login(12);const last=catalog.items.filter(x=>x.listingType==='FIXED_PRICE').at(-1);
sql('UPDATE Listing SET quantity=1 WHERE id='+last.listingId);
for(const t of [c,d]) { for(const i of (await api('/api/cart',t)).items) await api('/api/cart/items/'+i.cartItemId,t,'DELETE'); await api('/api/cart/items',t,'POST',{listingId:last.listingId,quantity:1}); }
const ca=(await api('/api/addresses',c))[0], da=(await api('/api/addresses',d))[0];
const race=await Promise.all([[c,ca],[d,da]].map(async ([t,ad])=>{const r=await fetch(base+'/api/checkout',{method:'POST',headers:{'Content-Type':'application/json',Authorization:'Bearer '+t},body:JSON.stringify({addressId:ad.id,method:'COD'})});return r.status}));
check(race.filter(x=>x===200).length===1 && race.filter(x=>x===409).length===1,'last unit protected from concurrent checkout: '+JSON.stringify(race));
check(Number(sql('SELECT quantity FROM Listing WHERE id='+last.listingId))===0,'stock never negative');
const delivered=await api('/api/orders',b);const item=delivered.find(x=>x.shipping?.status==='DELIVERED')?.items[0];
if(item){
 await api('/api/order-items/'+item.id+'/returns',a,'POST',{quantity:1,reason:'Not my item'},404);
 const returned=Number(sql("SELECT COALESCE(SUM(ri.quantity),0) FROM ReturnRequestItem ri JOIN ReturnRequest r ON r.id=ri.returnRequestId WHERE r.status NOT IN ('REJECTED','CANCELLED') AND ri.orderItemId="+item.id));
 if(returned===0)await api('/api/order-items/'+item.id+'/returns',b,'POST',{quantity:1,reason:'Test return'});
 await api('/api/order-items/'+item.id+'/returns',b,'POST',{quantity:999,reason:'Too many'},409);
}
console.log(`PASS: ${assertions} assertions against isolated buyer API.`);

