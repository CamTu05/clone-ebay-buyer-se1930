import {execFileSync} from 'node:child_process';
const base=process.env.BUYER_TEST_URL,db=process.env.BUYER_TEST_DATABASE,password=process.env.BUYER_TEST_PASSWORD;
if(!base||!password||!/^CloneEbayBuyer_Test_[A-Za-z0-9_]+$/.test(db??''))throw Error('Dedicated test URL/database/password required');
const sql=q=>execFileSync('sqlcmd',['-S',process.env.BUYER_TEST_SQL_SERVER??'.\\SQLEXPRESS',...(process.env.BUYER_TEST_SQL_USER?['-U',process.env.BUYER_TEST_SQL_USER]:['-E']),'-C','-I','-b','-d',db,'-h','-1','-W','-Q','SET NOCOUNT ON; '+q],{encoding:'utf8'}).trim();
let assertions=0;function check(v,m){if(!v)throw Error(m);assertions++}
async function api(path,{token,method='GET',body,status=200,guestToken}={}){const r=await fetch(base+path,{method,headers:{'Content-Type':'application/json',...(token?{Authorization:'Bearer '+token}:{}),...(guestToken?{'X-Guest-Token':guestToken}:{})},body:body===undefined?undefined:JSON.stringify(body)});const raw=await r.text();let j;try{j=JSON.parse(raw)}catch{j=raw}check(r.status===status,`${method} ${path}: ${r.status} ${JSON.stringify(j)}`);return j?.data}
const post=(path,body,token,status=200)=>api(path,{method:'POST',body,token,status});
const suffix=crypto.randomUUID().slice(0,8),email='test.'+suffix+'@example.test',pw='TestBuyer@12345';
await post('/api/auth/register',{email,username:'Buyer '+suffix,password:pw});
await post('/api/auth/login',{email,password:pw},undefined,403);
let mail=sql("SELECT TOP(1) body FROM EmailOutbox WHERE recipient='"+email+"' ORDER BY id DESC");let verify=mail.match(/token=([A-F0-9]{64})/)[1];
await post('/api/auth/verify-email',{token:verify});await post('/api/auth/verify-email',{token:verify},undefined,400);
let login=await post('/api/auth/login',{email,password:pw});let token=login.accessToken;
await post('/api/auth/forgot-password',{email});mail=sql("SELECT TOP(1) body FROM EmailOutbox WHERE recipient='"+email+"' ORDER BY id DESC");const reset=mail.match(/token=([A-F0-9]{64})/)[1];
await post('/api/auth/reset-password',{token:reset,password:pw+'2'});await api('/api/cart',{token,status:401});
token=(await post('/api/auth/login',{email,password:pw+'2'})).accessToken;
await post('/api/auth/change-password',{currentPassword:'wrong',password:pw+'3'},token,400);await post('/api/auth/change-password',{currentPassword:pw+'2',password:pw+'3'},token);await api('/api/cart',{token,status:401});
token=(await post('/api/auth/login',{email,password:pw+'3'})).accessToken;
const address=await post('/api/addresses',{fullName:'Test Buyer',phone:'0901234567',street:'123 Test street',city:'Hanoi',countryCode:'VN'},token,201);
sql('UPDATE Listing SET quantity=30 WHERE id IN (1,2,3,4)');
const merge={key:crypto.randomUUID(),items:[{listingId:1,quantity:2}]};await post('/api/cart/merge',merge,token);await post('/api/cart/merge',merge,token);
let cart=await api('/api/cart',{token});check(cart.items[0].quantity===2,'merge exactly once');
await post('/api/cart/items/'+cart.items[0].cartItemId+'/save-for-later',{},token);check((await api('/api/cart',{token})).items.length===0,'saved excluded');let saved=await api('/api/cart/saved',{token});check(saved.length===1,'saved list');await post('/api/cart/items/'+saved[0].id+'/move-to-cart',{},token);
const request={addressId:address.id,method:'PAYPAL',listingId:2,quantity:2,idempotencyKey:crypto.randomUUID()};let quote=await post('/api/checkout/preview',request,token);check(quote.taxAmount>0 && quote.shippingOptions.length>0,'tax and shipping options');
const order=await post('/api/checkout',request,token),again=await post('/api/checkout',request,token);check(order.checkoutGroupId===again.checkoutGroupId,'checkout replay');check(Number(sql('SELECT quantity FROM Listing WHERE id=2'))===28,'stock deducted once');
await post('/api/checkout',{...request,quantity:1},token,409);await post('/api/checkout/'+order.checkoutGroupId+'/mock-payment',{success:false},token);await post('/api/checkout/'+order.checkoutGroupId+'/mock-payment',{success:true},token);await post('/api/checkout/'+order.checkoutGroupId+'/mock-payment',{success:true},token);
const guest={addressId:0,method:'PAYPAL',listingId:3,quantity:1,idempotencyKey:crypto.randomUUID(),guest:{email:'guest.'+suffix+'@example.test',fullName:'Guest Buyer',phone:'0901234567',street:'123 Test street',city:'Hanoi',countryCode:'VN'}};
await post('/api/guest/checkout/preview',guest);const go=await post('/api/guest/checkout',guest),gr=await post('/api/guest/checkout',guest);check(go.checkoutGroupId===gr.checkoutGroupId && go.guestToken===gr.guestToken,'guest replay');
await api('/api/guest/checkout/'+go.checkoutGroupId,{status:401});await api('/api/guest/checkout/'+go.checkoutGroupId,{guestToken:go.guestToken});
await api('/api/guest/checkout/'+go.checkoutGroupId+'/mock-payment',{method:'POST',body:{success:false},guestToken:go.guestToken});await api('/api/guest/checkout/'+go.checkoutGroupId+'/mock-payment',{method:'POST',body:{success:true},guestToken:go.guestToken});
let me=await api('/api/me',{token});await api('/api/me',{method:'PATCH',body:{username:'Renamed_'+suffix},token});await api('/api/me',{method:'PATCH',body:{username:'Again_'+suffix},token,status:409});
await post('/api/auth/logout',{},token);await api('/api/cart',{token,status:401});
console.log(`PASS ${assertions} account/cart/checkout/guest assertions`);

