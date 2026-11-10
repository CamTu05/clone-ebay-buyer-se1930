import fs from 'node:fs';
import {execFileSync} from 'node:child_process';
const base=process.env.BUYER_TEST_URL,db=process.env.BUYER_TEST_DATABASE;
if(!base||!/^CloneEbayBuyer_Test_[A-Za-z0-9_]+$/.test(db??''))throw Error('Dedicated test URL/database required');
const sql=q=>execFileSync('sqlcmd',['-S',process.env.BUYER_TEST_SQL_SERVER??'.\\SQLEXPRESS',...(process.env.BUYER_TEST_SQL_USER?['-U',process.env.BUYER_TEST_SQL_USER]:['-E']),'-C','-I','-b','-d',db,'-h','-1','-W','-Q','SET NOCOUNT ON; '+q],{encoding:'utf8'}).trim();let checks=0;const check=(v,m)=>{if(!v)throw Error(m);checks++};
const r=await fetch(base+'/api/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email:'seed.buyer01@example.test',password:process.env.BUYER_TEST_PASSWORD})});check(r.status===200,'login');const token=(await r.json()).data.accessToken;
const req={headers:{Authorization:'Bearer '+token,'Content-Type':'application/json'}};
// Fulfilment fixture for a new buyer-owned order: seller delivery itself is outside Buyer scope.
sql("UPDATE Listing SET quantity=10 WHERE id=4");let response=await fetch(base+'/api/addresses',req);const address=(await response.json()).data[0].id;response=await fetch(base+'/api/checkout',{...req,method:'POST',body:JSON.stringify({addressId:address,method:'COD',listingId:4,quantity:1,idempotencyKey:crypto.randomUUID()})});check(response.status===200,'checkout');const order=(await response.json()).data.orderIds[0];
sql(`INSERT ShippingInfo(orderId,status,deliveredAt) VALUES(${order},'DELIVERED',SYSUTCDATETIME());`);
const item=Number(sql('SELECT id FROM OrderItem WHERE orderId='+order));
response=await fetch(base+'/api/order-items/'+item+'/reviews',{...req,method:'POST',body:JSON.stringify({rating:5,comment:'SQL smoke verified delivery'})});check(response.status===200,'review');response=await fetch(base+'/api/order-items/'+item+'/reviews',{...req,method:'POST',body:JSON.stringify({rating:4})});check(response.status===409,'review once');
response=await fetch(base+'/api/orders/'+order+'/seller-feedback',{...req,method:'POST',body:JSON.stringify({type:'POSITIVE',comment:'Delivered'})});check(response.status===200,'seller feedback');response=await fetch(base+'/api/orders/'+order+'/seller-feedback',{...req,method:'POST',body:JSON.stringify({type:'POSITIVE'})});check(response.status===409,'feedback once');
response=await fetch(base+'/api/products/4/reviews');check((await response.json()).data.some(x=>x.comment==='SQL smoke verified delivery'),'reviews readable');
console.log('PASS '+checks+' delivered review / feedback assertions');
